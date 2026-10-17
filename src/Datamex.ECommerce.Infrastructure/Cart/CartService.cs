using Datamex.ECommerce.Application.Abstractions;
using Datamex.ECommerce.Application.Cart;
using Datamex.ECommerce.Application.Inventory;
using Datamex.ECommerce.Application.Pricing;
using Datamex.ECommerce.Domain.Entities;
using Datamex.ECommerce.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Datamex.ECommerce.Infrastructure.Cart;

public sealed class CartService : ICartService
{
    private const int EstadoAbierto = 0;

    private readonly EcomDbContext _db;
    private readonly ITenantContext _tenant;
    private readonly IPricingService _pricing;
    private readonly IInventoryService _inventory;

    public CartService(
        EcomDbContext db,
        ITenantContext tenant,
        IPricingService pricing,
        IInventoryService inventory)
    {
        _db = db;
        _tenant = tenant;
        _pricing = pricing;
        _inventory = inventory;
    }

    public async Task<CartDto> GetOrCreateAsync(string sessionId, string canal = "B2C", CancellationToken ct = default)
    {
        var cart = await FindOpenCartAsync(sessionId, ct) ?? await CreateCartAsync(sessionId, canal, ct);
        return await MapAsync(cart, canal, ct);
    }

    public async Task<CartDto> AddItemAsync(string sessionId, AddCartItemRequest request, CancellationToken ct = default)
    {
        if (request.Cantidad <= 0)
            throw new ArgumentException("La cantidad debe ser mayor a 0.");

        var canal = await _pricing.ResolveEffectiveCanalAsync(request.Canal, ct);
        var idEmpresa = RequireTenant();
        var cart = await FindOpenCartAsync(sessionId, ct) ?? await CreateCartAsync(sessionId, canal, ct);

        var articulo = await _db.Articulos.AsNoTracking()
            .FirstOrDefaultAsync(a => a.ID == request.IDArticulo && a.IDEmpresa == idEmpresa && a.Activo && a.ActivoEcommerce, ct)
            ?? throw new InvalidOperationException("Artículo no encontrado o no activo en ecom.");

        var embalaje = await ResolveEmbalajeAsync(idEmpresa, request.IDArticulo, request.IDArticuloEmbalaje, ct);

        // Validar cantidad según unidad (metro, m², etc.) — medida exacta si permite fracción
        var qty = Math.Round(request.Cantidad, 3, MidpointRounding.AwayFromZero);
        ValidateCantidadVenta(embalaje, qty);

        var prices = await _pricing.GetEmbalajePricesAsync(request.IDArticulo, canal, ct);
        var price = prices.FirstOrDefault(p => p.IDArticuloEmbalaje == embalaje.ID)
                    ?? prices.OrderBy(p => p.PrecioContado).FirstOrDefault();

        var unitPrice = price?.PrecioContado ?? embalaje.PrecioDemo ?? 0;
        if (unitPrice <= 0)
            throw new InvalidOperationException("El artículo no tiene precio configurado.");

        if (price is not null && qty < price.CantidadMinimaMayorista)
            throw new InvalidOperationException($"Cantidad mínima mayorista: {price.CantidadMinimaMayorista:0.##}");

        var existing = await _db.DetallesCarritoCompra
            .FirstOrDefaultAsync(d =>
                d.IDCarritoCompra == cart.ID
                && d.IDArticulo == request.IDArticulo
                && d.CantidadxEmbalaje == (decimal)embalaje.CantidadxEmbalaje
                && d.Activo, ct);

        var newQty = Math.Round((existing?.Cantidad ?? 0) + qty, 3, MidpointRounding.AwayFromZero);
        var check = await _inventory.CheckAvailabilityAsync(
            request.IDArticulo, newQty, DepositForStockCheck(cart), ct);
        if (!check.Ok)
            throw new InvalidOperationException(check.Mensaje ?? "Stock insuficiente.");

        if (existing is null)
        {
            existing = new DetalleCarritoCompra
            {
                IDEmpresa = idEmpresa,
                IDCarritoCompra = cart.ID,
                IDArticulo = request.IDArticulo,
                Activo = true,
                UltModificacion = DateTime.UtcNow,
                CantidadxEmbalaje = (decimal)embalaje.CantidadxEmbalaje,
                IDRegimenImpositivo = 1,
                PorcentajeImpuesto = 10,
                PrecioBrutoConImpuesto = unitPrice,
                PrecioBrutoSinImpuesto = Math.Round(unitPrice / 1.1m, 2),
                PrecioNetoConImpuesto = unitPrice,
                PrecioNetoSinImpuesto = Math.Round(unitPrice / 1.1m, 2),
            };
            ApplyCantidadExacta(existing, embalaje, qty, unitPrice);
            _db.DetallesCarritoCompra.Add(existing);
        }
        else
        {
            ApplyCantidadExacta(existing, embalaje, newQty, unitPrice);
            existing.PrecioBrutoConImpuesto = unitPrice;
            existing.PrecioNetoConImpuesto = unitPrice;
            existing.PrecioBrutoSinImpuesto = Math.Round(unitPrice / 1.1m, 2);
            existing.PrecioNetoSinImpuesto = Math.Round(unitPrice / 1.1m, 2);
            existing.UltModificacion = DateTime.UtcNow;
        }

        if (price is not null)
            cart.IDListaPrecio = price.IDListaPrecio;

        await _db.SaveChangesAsync(ct);
        await RecalcTotalsAsync(cart, ct);
        await _db.SaveChangesAsync(ct);
        return await MapAsync(cart, canal, ct);
    }

    public async Task<CartDto> UpdateItemAsync(string sessionId, long detalleId, decimal cantidad, string canal = "B2C", CancellationToken ct = default)
    {
        if (cantidad <= 0)
            return await RemoveItemAsync(sessionId, detalleId, canal, ct);

        cantidad = Math.Round(cantidad, 3, MidpointRounding.AwayFromZero);

        var cart = await FindOpenCartAsync(sessionId, ct)
                   ?? throw new InvalidOperationException("Carrito no encontrado.");

        var line = await _db.DetallesCarritoCompra
            .FirstOrDefaultAsync(d => d.ID == detalleId && d.IDCarritoCompra == cart.ID && d.Activo, ct)
            ?? throw new InvalidOperationException("Ítem no encontrado.");

        var check = await _inventory.CheckAvailabilityAsync(
            line.IDArticulo, cantidad, DepositForStockCheck(cart), ct);
        if (!check.Ok)
            throw new InvalidOperationException(check.Mensaje ?? "Stock insuficiente.");

        var emb = await ResolveEmbalajeAsync(cart.IDEmpresa, line.IDArticulo, null, ct);
        // Prefer matching line's cantidadxembalaje
        var embExact = await _db.ArticuloEmbalajes.AsNoTracking()
            .FirstOrDefaultAsync(e =>
                e.IDEmpresa == cart.IDEmpresa
                && e.IDArticulo == line.IDArticulo
                && e.Activo
                && (decimal)e.CantidadxEmbalaje == line.CantidadxEmbalaje, ct) ?? emb;

        ValidateCantidadVenta(embExact, cantidad);
        ApplyCantidadExacta(line, embExact, cantidad, line.PrecioNetoConImpuesto);
        line.UltModificacion = DateTime.UtcNow;

        await _db.SaveChangesAsync(ct);
        await RecalcTotalsAsync(cart, ct);
        await _db.SaveChangesAsync(ct);
        return await MapAsync(cart, canal, ct);
    }

    public async Task<CartDto> RemoveItemAsync(string sessionId, long detalleId, string canal = "B2C", CancellationToken ct = default)
    {
        var cart = await FindOpenCartAsync(sessionId, ct)
                   ?? throw new InvalidOperationException("Carrito no encontrado.");

        var line = await _db.DetallesCarritoCompra
            .FirstOrDefaultAsync(d => d.ID == detalleId && d.IDCarritoCompra == cart.ID, ct);

        if (line is not null)
        {
            _db.DetallesCarritoCompra.Remove(line);
            await _db.SaveChangesAsync(ct);
            await RecalcTotalsAsync(cart, ct);
            await _db.SaveChangesAsync(ct);
        }

        return await MapAsync(cart, canal, ct);
    }

    public async Task ClearAsync(string sessionId, CancellationToken ct = default)
    {
        var cart = await FindOpenCartAsync(sessionId, ct);
        if (cart is null)
            return;

        var lines = await _db.DetallesCarritoCompra.Where(d => d.IDCarritoCompra == cart.ID).ToListAsync(ct);
        _db.DetallesCarritoCompra.RemoveRange(lines);
        cart.MontoTotal = 0;
        cart.MontoIVA = 0;
        cart.MontoGravado = 0;
        cart.UltModificacion = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);
    }

    public async Task<CartDto> SetPickupAsync(string sessionId, long idAgencia, CancellationToken ct = default)
    {
        var idEmpresa = RequireTenant();
        var cart = await FindOpenCartAsync(sessionId, ct)
                   ?? throw new InvalidOperationException("No hay carrito abierto.");

        var agencia = await _db.Agencias.AsNoTracking()
            .FirstOrDefaultAsync(a => a.ID == idAgencia && a.IDEmpresa == idEmpresa && a.Activo, ct)
            ?? throw new InvalidOperationException("Sucursal / agencia no válida.");

        if (agencia.IDDeposito is not > 0)
            throw new InvalidOperationException(
                $"La sucursal '{agencia.DescripcionAgencia}' no tiene depósito de ecommerce configurado.");

        cart.IDAgenciaFacturacion = agencia.ID;
        cart.IDDeposito = agencia.IDDeposito.Value;
        cart.TipoEntrega = 1; // pickup
        cart.UltModificacion = DateTime.UtcNow;

        // Revalidar stock SOLO en el depósito de esa sucursal (sin fallback a otras).
        var lines = await _db.DetallesCarritoCompra
            .Where(d => d.IDCarritoCompra == cart.ID && d.Activo)
            .ToListAsync(ct);
        foreach (var line in lines)
        {
            var check = await _inventory.CheckAvailabilityAsync(line.IDArticulo, line.Cantidad, cart.IDDeposito, ct);
            if (!check.Ok)
                throw new InvalidOperationException(
                    $"Sin stock en {agencia.DescripcionAgencia} para el artículo {line.IDArticulo} (disp. {check.CantidadLibre:0}). Probá otra sucursal.");
        }

        await _db.SaveChangesAsync(ct);
        return await MapAsync(cart, "B2C", ct);
    }

    public async Task<CartDto> ReorderAsync(string sessionId, long nroPedido, string canal = "B2C", CancellationToken ct = default)
    {
        var idEmpresa = RequireTenant();
        var source = await _db.CarritosCompra.AsNoTracking()
            .FirstOrDefaultAsync(c =>
                c.IDEmpresa == idEmpresa
                && c.NroPedidoCarritoCompra == nroPedido
                && c.Estado != EstadoAbierto
                && c.Activo, ct)
            ?? throw new InvalidOperationException("Pedido no encontrado para volver a pedir.");

        var sourceLines = await _db.DetallesCarritoCompra.AsNoTracking()
            .Where(d => d.IDCarritoCompra == source.ID && d.Activo)
            .ToListAsync(ct);
        if (sourceLines.Count == 0)
            throw new InvalidOperationException("El pedido no tiene ítems.");

        var cart = await FindOpenCartAsync(sessionId, ct) ?? await CreateCartAsync(sessionId, canal, ct);

        var deliveryArtId = await _db.EmpresaConfiguracionesEcommerce.AsNoTracking()
            .Where(c => c.ID == idEmpresa)
            .Select(c => c.IDArticuloDelivery)
            .FirstOrDefaultAsync(ct);

        var added = 0;
        foreach (var line in sourceLines)
        {
            if (deliveryArtId.HasValue && line.IDArticulo == deliveryArtId.Value)
                continue;

            var artOk = await _db.Articulos.AsNoTracking()
                .AnyAsync(a => a.ID == line.IDArticulo && a.IDEmpresa == idEmpresa && a.Activo && a.ActivoEcommerce, ct);
            if (!artOk)
                continue;

            long? embId = null;
            if (line.CantidadxEmbalaje > 0)
            {
                embId = await _db.ArticuloEmbalajes.AsNoTracking()
                    .Where(e => e.IDEmpresa == idEmpresa
                                && e.IDArticulo == line.IDArticulo
                                && e.Activo
                                && (decimal)e.CantidadxEmbalaje == line.CantidadxEmbalaje)
                    .Select(e => (long?)e.ID)
                    .FirstOrDefaultAsync(ct);
            }

            try
            {
                await AddItemAsync(sessionId, new AddCartItemRequest(line.IDArticulo, line.Cantidad, embId, canal), ct);
                added++;
            }
            catch (InvalidOperationException)
            {
                // Ítem sin stock / MOQ: seguir con el resto
            }
            cart = await FindOpenCartAsync(sessionId, ct) ?? cart;
        }

        if (added == 0)
            throw new InvalidOperationException("No se pudieron agregar ítems del pedido (stock o artículos inactivos).");

        return await MapAsync(cart, canal, ct);
    }

    private async Task<CarritoCompra?> FindOpenCartAsync(string sessionId, CancellationToken ct)
    {
        var idEmpresa = RequireTenant();
        return await _db.CarritosCompra
            .Include(c => c.DetalleCarritoCompras)
            .FirstOrDefaultAsync(c =>
                c.IDEmpresa == idEmpresa
                && c.IDSesion == sessionId
                && c.Estado == EstadoAbierto
                && !c.Migrado
                && c.Activo, ct);
    }

    /// <summary>
    /// Retiro: validar solo el depósito de la sucursal.
    /// Delivery / aún sin sucursal: stock agregado ecommerce (misma regla que la card).
    /// </summary>
    private static long? DepositForStockCheck(CarritoCompra cart)
        => cart.TipoEntrega == 1 && cart.IDDeposito > 0 ? cart.IDDeposito : null;

    private async Task<CarritoCompra> CreateCartAsync(string sessionId, string canal, CancellationToken ct)
    {
        var idEmpresa = RequireTenant();
        var now = DateTime.UtcNow;
        var config = await _db.EmpresaConfiguracionesEcommerce.AsNoTracking()
            .FirstAsync(c => c.ID == idEmpresa, ct);

        // Atómico: evita dos carritos con el mismo N° bajo carrera concurrente.
        var nroPedido = await PedidoNumeroAllocator.NextAsync(_db, idEmpresa, ct);
        var deposito = await _inventory.GetEcommerceDepositoAsync(ct);
        var lista = await _pricing.ResolveListaAsync(canal, null, ct);
        // Default: sucursal ecommerce (Casa Matriz), no hardcode ID=1.
        var agenciaDefault = await _db.Agencias.AsNoTracking()
            .Where(a => a.IDEmpresa == idEmpresa && a.Activo)
            .OrderByDescending(a => a.EsSucursalEcommerce)
            .ThenByDescending(a => a.IDDeposito == config.IDDeposito)
            .ThenBy(a => a.CodAgencia)
            .Select(a => new { a.ID, a.IDDeposito })
            .FirstOrDefaultAsync(ct);

        var cart = new CarritoCompra
        {
            IDEmpresa = idEmpresa,
            Activo = true,
            UltModificacion = now,
            NroPedidoCarritoCompra = nroPedido,
            IDSesion = sessionId,
            IDListaPrecio = lista?.Id,
            IDCondicionVenta = 1,
            IDVendedor = 1,
            IDDeposito = deposito?.Id ?? agenciaDefault?.IDDeposito ?? config.IDDeposito,
            // 0 = el write-back resuelve cotización ERP del día (empresa+moneda+fecha).
            IDCotizacionComercial = 0,
            IDAgenciaFacturacion = agenciaDefault?.ID ?? 0,
            FechaPedido = now,
            MontoCambio = 1,
            FechaPrevistaEntrega = now.AddDays(2),
            RazonSocial = "Consumidor final",
            NumeroDocumento = "0",
            Estado = EstadoAbierto,
            Migrado = false,
            TipoEntrega = 0,
            TipoPago = 0
        };

        _db.CarritosCompra.Add(cart);
        await _db.SaveChangesAsync(ct);
        return cart;
    }

    private async Task<ArticuloEmbalaje> ResolveEmbalajeAsync(
        long idEmpresa, long articuloId, long? embalajeId, CancellationToken ct)
    {
        if (embalajeId.HasValue)
        {
            return await _db.ArticuloEmbalajes.AsNoTracking()
                       .FirstOrDefaultAsync(e =>
                           e.ID == embalajeId.Value
                           && e.IDEmpresa == idEmpresa
                           && e.IDArticulo == articuloId
                           && e.Activo, ct)
                   ?? throw new InvalidOperationException("Embalaje no válido.");
        }

        return await _db.ArticuloEmbalajes.AsNoTracking()
                   .Where(e => e.IDEmpresa == idEmpresa && e.IDArticulo == articuloId && e.Activo)
                   .OrderBy(e => e.CantidadxEmbalaje)
                   .FirstOrDefaultAsync(ct)
               ?? throw new InvalidOperationException("El artículo no tiene embalajes.");
    }

    private static void ValidateCantidadVenta(ArticuloEmbalaje embalaje, decimal cantidad)
    {
        if (cantidad <= 0)
            throw new ArgumentException("La cantidad debe ser mayor a 0.");

        // Normalizar ruido de float (máx. 3 decimales)
        cantidad = Math.Round(cantidad, 3, MidpointRounding.AwayFromZero);

        var min = embalaje.CantidadMinimaVenta > 0 ? embalaje.CantidadMinimaVenta : 1m;
        if (cantidad < min)
            throw new InvalidOperationException(
                $"Cantidad mínima: {min:0.###} {(embalaje.CodigoUnidadMedida is "M" or "M2" ? embalaje.CodigoUnidadMedida.ToLowerInvariant() : "")}".Trim());

        if (!embalaje.PermiteFraccion && cantidad != Math.Truncate(cantidad))
            throw new InvalidOperationException("Este producto no admite cantidades fraccionarias.");

        // Con fracción (metro/kg/litro): se factura la medida exacta pedida.
        // El incremento solo guía los botones +/- del storefront; no fuerza redondeo.
        if (embalaje.PermiteFraccion)
            return;

        var step = embalaje.IncrementoVenta > 0 ? embalaje.IncrementoVenta : 1m;
        if (step > 0)
        {
            var steps = (cantidad - min) / step;
            if (Math.Abs(steps - Math.Round(steps, 4)) > 0.0001m)
                throw new InvalidOperationException($"La cantidad debe ir de a {step:0.##} a partir de {min:0.##}.");
        }
    }

    /// <summary>
    /// Guarda la cantidad exacta a facturar y deriva embalada/fraccionada (paridad PedidoMovil:
    /// qty = embalada + fraccionada / cantidadxembalaje).
    /// </summary>
    private static void ApplyCantidadExacta(
        DetalleCarritoCompra line, ArticuloEmbalaje embalaje, decimal cantidad, decimal unitPrice)
    {
        cantidad = Math.Round(cantidad, 3, MidpointRounding.AwayFromZero);
        line.Cantidad = cantidad;
        line.MontoTotal = Math.Round(unitPrice * cantidad, 2, MidpointRounding.AwayFromZero);
        line.MontoIVA = Math.Round(line.MontoTotal * 0.1m / 1.1m, 2, MidpointRounding.AwayFromZero);

        var cxe = (decimal)embalaje.CantidadxEmbalaje;
        if (cxe <= 0) cxe = 1m;

        if (embalaje.PermiteFraccion)
        {
            var whole = Math.Floor(cantidad);
            var fracPart = cantidad - whole;
            var fraccionada = (int)Math.Round(fracPart * cxe, MidpointRounding.AwayFromZero);
            var embalada = (int)whole;
            if (cxe >= 1m && fraccionada >= (int)cxe)
            {
                embalada += fraccionada / (int)cxe;
                fraccionada %= (int)cxe;
            }
            line.CantidadEmbalada = embalada;
            line.CantidadFraccionada = fraccionada;
        }
        else
        {
            line.CantidadEmbalada = (int)Math.Round(cantidad, MidpointRounding.AwayFromZero);
            line.CantidadFraccionada = 0;
        }
    }

    private async Task RecalcTotalsAsync(CarritoCompra cart, CancellationToken ct)
    {
        var lines = await _db.DetallesCarritoCompra
            .Where(d => d.IDCarritoCompra == cart.ID && d.Activo)
            .ToListAsync(ct);

        cart.MontoTotal = lines.Sum(l => l.MontoTotal);
        cart.MontoIVA = lines.Sum(l => l.MontoIVA);
        cart.MontoGravado = cart.MontoTotal - cart.MontoIVA;
        cart.UltModificacion = DateTime.UtcNow;
    }

    private async Task<CartDto> MapAsync(CarritoCompra cart, string canal, CancellationToken ct)
    {
        var idEmpresa = RequireTenant();
        var config = await _db.EmpresaConfiguracionesEcommerce.AsNoTracking()
            .FirstOrDefaultAsync(c => c.ID == idEmpresa, ct);

        var lines = await _db.DetallesCarritoCompra.AsNoTracking()
            .Where(d => d.IDCarritoCompra == cart.ID && d.Activo)
            .ToListAsync(ct);

        var artIds = lines.Select(l => l.IDArticulo).Distinct().ToList();
        var arts = await _db.Articulos.AsNoTracking()
            .Where(a => artIds.Contains(a.ID))
            .Select(a => new { a.ID, a.SKU, a.DescripcionArticulo })
            .ToListAsync(ct);
        var artMap = arts.ToDictionary(a => a.ID);

        var imgs = await _db.ArticuloImagenes.AsNoTracking()
            .Where(i => artIds.Contains(i.IDArticulo) && i.Activo)
            .OrderByDescending(i => i.EsPrincipal)
            .ThenBy(i => i.Orden)
            .Select(i => new { i.IDArticulo, i.Url })
            .ToListAsync(ct);
        var imgMap = imgs.GroupBy(i => i.IDArticulo).ToDictionary(g => g.Key, g => g.First().Url);

        var emb = await _db.ArticuloEmbalajes.AsNoTracking()
            .Where(e => artIds.Contains(e.IDArticulo) && e.Activo)
            .Select(e => new { e.ID, e.IDArticulo, e.CantidadxEmbalaje, e.DescripcionEmbalaje })
            .ToListAsync(ct);

        var stockMap = await _inventory.GetStockBatchAsync(artIds, cart.IDDeposito, ct);

        var dtoLines = lines.Select(l =>
        {
            artMap.TryGetValue(l.IDArticulo, out var a);
            imgMap.TryGetValue(l.IDArticulo, out var img);
            var embMatch = emb.FirstOrDefault(e =>
                e.IDArticulo == l.IDArticulo
                && (decimal)e.CantidadxEmbalaje == l.CantidadxEmbalaje);
            stockMap.TryGetValue(l.IDArticulo, out var stock);
            var libre = stock?.CantidadLibre ?? 0;
            // Línea: OK si el espejo ERP cubre la cantidad (Integrated también).
            var ok = libre >= l.Cantidad;

            return new CartLineDto(
                l.ID,
                l.IDArticulo,
                a?.SKU,
                a?.DescripcionArticulo ?? $"Artículo {l.IDArticulo}",
                img,
                embMatch?.ID,
                embMatch?.DescripcionEmbalaje,
                l.Cantidad,
                l.PrecioNetoConImpuesto,
                l.MontoTotal,
                libre,
                ok);
        }).ToList();

        var listaCanal = canal;
        if (cart.IDListaPrecio.HasValue)
        {
            var lista = await _db.ListasPrecio.AsNoTracking()
                .Where(x => x.ID == cart.IDListaPrecio)
                .Select(x => x.CanalEcommerce)
                .FirstOrDefaultAsync(ct);
            if (!string.IsNullOrWhiteSpace(lista))
                listaCanal = lista;
        }

        return new CartDto(
            cart.ID,
            cart.IDSesion,
            listaCanal,
            cart.IDDeposito,
            cart.Estado,
            cart.MontoTotal,
            cart.MontoIVA,
            dtoLines.Count,
            config?.ControlaStockEnCarrito ?? false,
            dtoLines,
            cart.MontoDescuento,
            cart.CodigoCuponAplicado,
            cart.IDAgenciaFacturacion);
    }

    private long RequireTenant()
    {
        if (!_tenant.IsResolved || !_tenant.IDEmpresa.HasValue)
            throw new InvalidOperationException("Tenant no resuelto.");
        return _tenant.IDEmpresa.Value;
    }
}
