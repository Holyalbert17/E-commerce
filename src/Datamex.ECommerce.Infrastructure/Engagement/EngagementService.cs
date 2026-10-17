using Datamex.ECommerce.Application.Abstractions;
using Datamex.ECommerce.Application.Engagement;
using Datamex.ECommerce.Application.Integration;
using Datamex.ECommerce.Application.Inventory;
using Datamex.ECommerce.Application.Media;
using Datamex.ECommerce.Application.Pricing;
using Datamex.ECommerce.Application.Sync;
using Datamex.ECommerce.Domain.Entities;
using Datamex.ECommerce.Domain.Enums;
using Datamex.ECommerce.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Datamex.ECommerce.Infrastructure.Engagement;

public sealed class EngagementService : IEngagementService
{
    private const int EstadoAbierto = 0;

    private readonly EcomDbContext _db;
    private readonly ITenantContext _tenant;
    private readonly IMediaStorage _media;
    private readonly IChatRealtimeNotifier _realtime;
    private readonly ITrackingRealtimeNotifier _trackingRealtime;
    private readonly IPricingService _pricing;
    private readonly IInventoryService _inventory;
    private readonly IErpPedidoLifecycleWriter _erpLife;

    public EngagementService(
        EcomDbContext db,
        ITenantContext tenant,
        IMediaStorage media,
        IChatRealtimeNotifier realtime,
        ITrackingRealtimeNotifier trackingRealtime,
        IPricingService pricing,
        IInventoryService inventory,
        IErpPedidoLifecycleWriter erpLife)
    {
        _db = db;
        _tenant = tenant;
        _media = media;
        _realtime = realtime;
        _trackingRealtime = trackingRealtime;
        _pricing = pricing;
        _inventory = inventory;
        _erpLife = erpLife;
    }

    public async Task TrackAsync(TrackInteraccionRequest request, long? idUsuario, string? email, CancellationToken ct = default)
    {
        var idEmpresa = RequireTenant();
        var tipo = (request.TipoInteraccion ?? "vista").Trim().ToLowerInvariant();
        if (tipo is not ("vista" or "click" or "add_cart" or "compra" or "favorito" or "seguir"))
            tipo = "vista";

        _db.InteraccionesArticuloEcommerce.Add(new InteraccionArticuloEcommerce
        {
            IDEmpresa = idEmpresa,
            IDArticulo = request.IdArticulo,
            IDUsuarioEcommerce = idUsuario,
            Email = email,
            IDSesion = request.IdSesion,
            TipoInteraccion = tipo,
            CanalEcommerce = NormalizeCanal(request.Canal),
            Momento = DateTime.UtcNow,
            Activo = true,
            UltModificacion = DateTime.UtcNow,
            UserName = email ?? idUsuario?.ToString()
        });
        await _db.SaveChangesAsync(ct);
    }

    public async Task<IReadOnlyList<RecomendacionDto>> GetRecomendacionesAsync(
        long? idArticulo, string canal = "B2C", int take = 8, CancellationToken ct = default)
    {
        var idEmpresa = RequireTenant();
        canal = NormalizeCanal(canal);
        take = Math.Clamp(take, 1, 20);

        // Co-occurrence from cart lines + popular interactions
        List<long> ranked;
        if (idArticulo.HasValue)
        {
            var cartIds = await _db.DetallesCarritoCompra.AsNoTracking()
                .Where(d => d.IDEmpresa == idEmpresa && d.IDArticulo == idArticulo.Value && d.Activo)
                .Select(d => d.IDCarritoCompra)
                .Distinct()
                .Take(200)
                .ToListAsync(ct);

            ranked = await _db.DetallesCarritoCompra.AsNoTracking()
                .Where(d => cartIds.Contains(d.IDCarritoCompra) && d.IDArticulo != idArticulo.Value && d.Activo)
                .GroupBy(d => d.IDArticulo)
                .Select(g => new { Id = g.Key, Cnt = g.Count() })
                .OrderByDescending(x => x.Cnt)
                .Take(take)
                .Select(x => x.Id)
                .ToListAsync(ct);
        }
        else
        {
            ranked = await _db.InteraccionesArticuloEcommerce.AsNoTracking()
                .Where(i => i.IDEmpresa == idEmpresa && i.Activo)
                .GroupBy(i => i.IDArticulo)
                .Select(g => new { Id = g.Key, Cnt = g.Count() })
                .OrderByDescending(x => x.Cnt)
                .Take(take)
                .Select(x => x.Id)
                .ToListAsync(ct);
        }

        if (ranked.Count < take)
        {
            var extra = await _db.Articulos.AsNoTracking()
                .Where(a => a.IDEmpresa == idEmpresa && a.Activo && a.ActivoEcommerce
                            && a.EstadoEcommerce == "ACTIVO"
                            && !ranked.Contains(a.ID)
                            && (!idArticulo.HasValue || a.ID != idArticulo.Value))
                .OrderByDescending(a => a.ID)
                .Take(take - ranked.Count)
                .Select(a => a.ID)
                .ToListAsync(ct);
            ranked.AddRange(extra);
        }

        return await MapRecomendacionesAsync(ranked, idArticulo.HasValue ? "Tambi?n compraron" : "Populares", ct);
    }

    public async Task<IReadOnlyList<CuponDto>> AdminListCuponesAsync(CancellationToken ct = default)
    {
        var idEmpresa = RequireTenant();
        var rows = await _db.CuponesEcommerce.AsNoTracking()
            .Where(c => c.IDEmpresa == idEmpresa)
            .OrderByDescending(c => c.ID)
            .ToListAsync(ct);
        return rows.Select(MapCupon).ToList();
    }

    public async Task<CuponDto> AdminCrearCuponAsync(CrearCuponRequest request, CancellationToken ct = default)
    {
        var idEmpresa = RequireTenant();
        var code = (request.Codigo ?? "").Trim().ToUpperInvariant();
        if (code.Length < 3)
            throw new ArgumentException("C?digo inv?lido.");
        if (await _db.CuponesEcommerce.AnyAsync(c => c.IDEmpresa == idEmpresa && c.Codigo == code, ct))
            throw new InvalidOperationException("Ya existe un cup?n con ese c?digo.");

        var entity = new CuponEcommerce
        {
            IDEmpresa = idEmpresa,
            Codigo = code,
            Descripcion = Truncate(request.Descripcion, 200) ?? code,
            TipoDescuento = request.TipoDescuento is "monto" ? "monto" : "porcentaje",
            ValorDescuento = request.ValorDescuento,
            MontoMinimo = request.MontoMinimo,
            MontoMaximoDescuento = request.MontoMaximoDescuento,
            CanalEcommerce = string.IsNullOrWhiteSpace(request.CanalEcommerce) ? "ALL" : request.CanalEcommerce.ToUpperInvariant(),
            FechaInicio = request.FechaInicio ?? DateTime.UtcNow.Date,
            FechaFin = request.FechaFin ?? DateTime.UtcNow.Date.AddMonths(3),
            UsosMaximos = request.UsosMaximos,
            UsosPorUsuario = Math.Max(1, request.UsosPorUsuario),
            Activo = true,
            UltModificacion = DateTime.UtcNow,
            UserName = "admin"
        };
        _db.CuponesEcommerce.Add(entity);
        await _db.SaveChangesAsync(ct);
        return MapCupon(entity);
    }

    public async Task AdminToggleCuponAsync(long id, bool activo, CancellationToken ct = default)
    {
        var idEmpresa = RequireTenant();
        var c = await _db.CuponesEcommerce.FirstOrDefaultAsync(x => x.ID == id && x.IDEmpresa == idEmpresa, ct)
                ?? throw new InvalidOperationException("Cupón no encontrado.");
        c.Activo = activo;
        c.UltModificacion = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);
    }

    public async Task<AplicarCuponResult> AplicarCuponAsync(
        string sessionId, string codigo, string canal, long? idUsuario, CancellationToken ct = default)
    {
        var idEmpresa = RequireTenant();
        var code = (codigo ?? "").Trim().ToUpperInvariant();
        var cupon = await _db.CuponesEcommerce
            .FirstOrDefaultAsync(c => c.IDEmpresa == idEmpresa && c.Codigo == code && c.Activo, ct);
        if (cupon is null)
            return new AplicarCuponResult(false, "Cupón no válido.", 0, null);

        var now = DateTime.UtcNow;
        if (now < cupon.FechaInicio || now > cupon.FechaFin.AddDays(1))
            return new AplicarCuponResult(false, "Cupón fuera de vigencia.", 0, null);

        canal = NormalizeCanal(canal);
        if (cupon.CanalEcommerce is not ("ALL" or "")
            && !string.Equals(cupon.CanalEcommerce, canal, StringComparison.OrdinalIgnoreCase))
            return new AplicarCuponResult(false, $"Cupón solo para canal {cupon.CanalEcommerce}.", 0, null);

        if (cupon.UsosMaximos.HasValue && cupon.UsosActuales >= cupon.UsosMaximos.Value)
            return new AplicarCuponResult(false, "Cupón agotado.", 0, null);

        if (idUsuario.HasValue)
        {
            var usosUser = await _db.UsosCuponEcommerce.CountAsync(
                u => u.IDCuponEcommerce == cupon.ID && u.IDUsuarioEcommerce == idUsuario && u.Activo, ct);
            if (usosUser >= cupon.UsosPorUsuario)
                return new AplicarCuponResult(false, "Ya usaste este cupón el máximo permitido.", 0, null);
        }

        var cart = await _db.CarritosCompra
            .Include(c => c.DetalleCarritoCompras)
            .FirstOrDefaultAsync(c => c.IDEmpresa == idEmpresa
                                      && c.IDSesion == sessionId
                                      && c.Estado == EstadoAbierto
                                      && c.Activo, ct)
            ?? throw new InvalidOperationException("Carrito no encontrado.");

        var subtotal = cart.DetalleCarritoCompras.Where(d => d.Activo).Sum(d => d.MontoTotal);
        if (subtotal < cupon.MontoMinimo)
            return new AplicarCuponResult(false, $"Monto mínimo {cupon.MontoMinimo:N0}.", 0, null);

        var discount = cupon.TipoDescuento == "monto"
            ? cupon.ValorDescuento
            : Math.Round(subtotal * (cupon.ValorDescuento / 100m), 0);
        if (cupon.MontoMaximoDescuento.HasValue)
            discount = Math.Min(discount, cupon.MontoMaximoDescuento.Value);
        discount = Math.Min(discount, subtotal);

        cart.CodigoCuponAplicado = cupon.Codigo;
        cart.MontoDescuento = discount;
        cart.MontoTotal = Math.Max(0, subtotal - discount);
        cart.UltModificacion = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);

        return new AplicarCuponResult(true, $"Cupón {cupon.Codigo} aplicado.", discount, cupon.Codigo);
    }

    public async Task QuitarCuponAsync(string sessionId, CancellationToken ct = default)
    {
        var idEmpresa = RequireTenant();
        var cart = await _db.CarritosCompra
            .Include(c => c.DetalleCarritoCompras)
            .FirstOrDefaultAsync(c => c.IDEmpresa == idEmpresa
                                      && c.IDSesion == sessionId
                                      && c.Estado == EstadoAbierto
                                      && c.Activo, ct);
        if (cart is null) return;
        cart.CodigoCuponAplicado = null;
        cart.MontoDescuento = 0;
        cart.MontoTotal = cart.DetalleCarritoCompras.Where(d => d.Activo).Sum(d => d.MontoTotal);
        cart.UltModificacion = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);
    }

    public async Task SeguirProductoAsync(long idUsuario, long idArticulo, CancellationToken ct = default)
    {
        var idEmpresa = RequireTenant();
        var user = await RequireUserAsync(idEmpresa, idUsuario, ct);
        var exists = await _db.ProductosSeguidosEcommerce
            .AnyAsync(p => p.IDEmpresa == idEmpresa && p.IDUsuarioEcommerce == idUsuario
                           && p.IDArticulo == idArticulo && p.Activo, ct);
        if (exists) return;
        _db.ProductosSeguidosEcommerce.Add(new ProductoSeguidoEcommerce
        {
            IDEmpresa = idEmpresa,
            IDUsuarioEcommerce = idUsuario,
            IDArticulo = idArticulo,
            Email = user.Email ?? user.Usuario,
            CanalEcommerce = NormalizeCanal(user.CanalPreferido),
            FechaSeguimiento = DateTime.UtcNow,
            Activo = true,
            UltModificacion = DateTime.UtcNow,
            UserName = user.Email
        });
        await _db.SaveChangesAsync(ct);
    }

    public async Task DejarDeSeguirAsync(long idUsuario, long idArticulo, CancellationToken ct = default)
    {
        var idEmpresa = RequireTenant();
        var rows = await _db.ProductosSeguidosEcommerce
            .Where(p => p.IDEmpresa == idEmpresa && p.IDUsuarioEcommerce == idUsuario
                        && p.IDArticulo == idArticulo && p.Activo)
            .ToListAsync(ct);
        foreach (var r in rows)
        {
            r.Activo = false;
            r.UltModificacion = DateTime.UtcNow;
        }
        await _db.SaveChangesAsync(ct);
    }

    public async Task<IReadOnlyList<SeguirProductoDto>> ListSeguidosAsync(long idUsuario, CancellationToken ct = default)
    {
        var idEmpresa = RequireTenant();
        var rows = await _db.ProductosSeguidosEcommerce.AsNoTracking()
            .Where(p => p.IDEmpresa == idEmpresa && p.IDUsuarioEcommerce == idUsuario && p.Activo)
            .OrderByDescending(p => p.FechaSeguimiento)
            .ToListAsync(ct);
        var artIds = rows.Select(r => r.IDArticulo).ToList();
        if (artIds.Count == 0)
            return Array.Empty<SeguirProductoDto>();

        var arts = await _db.Articulos.AsNoTracking()
            .Where(a => artIds.Contains(a.ID))
            .ToDictionaryAsync(a => a.ID, a => a, ct);
        var imgs = (await _db.ArticuloImagenes.AsNoTracking()
            .Where(i => artIds.Contains(i.IDArticulo) && i.Activo)
            .OrderByDescending(i => i.EsPrincipal).ThenBy(i => i.Orden)
            .Select(i => new { i.IDArticulo, i.Url }).ToListAsync(ct))
            .GroupBy(x => x.IDArticulo).ToDictionary(g => g.Key, g => g.First().Url);

        var canal = await _pricing.ResolveEffectiveCanalAsync(null, ct);
        var priceMap = await _pricing.GetProductPriceSummariesAsync(artIds, canal, ct);
        var stockMap = await _inventory.GetStockBatchAsync(artIds, null, ct);

        return rows.Select(r =>
        {
            arts.TryGetValue(r.IDArticulo, out var a);
            priceMap.TryGetValue(r.IDArticulo, out var price);
            stockMap.TryGetValue(r.IDArticulo, out var stock);
            var libre = stock?.CantidadLibre ?? 0m;
            var ok = a?.EstadoEcommerce != "agotado" && libre > 0;

            return new SeguirProductoDto(
                r.IDArticulo,
                a?.DescripcionArticulo ?? $"#{r.IDArticulo}",
                imgs.GetValueOrDefault(r.IDArticulo),
                r.FechaSeguimiento,
                a?.SKU,
                price?.PrecioDesde,
                ok,
                libre);
        }).ToList();
    }

    public async Task<bool> EstaSeguidoAsync(long idUsuario, long idArticulo, CancellationToken ct = default)
    {
        var idEmpresa = RequireTenant();
        return await _db.ProductosSeguidosEcommerce.AsNoTracking()
            .AnyAsync(p => p.IDEmpresa == idEmpresa && p.IDUsuarioEcommerce == idUsuario
                           && p.IDArticulo == idArticulo && p.Activo, ct);
    }

    public async Task<IReadOnlyList<SeguimientoDto>> GetTrackingAsync(long idPedidoMovil, CancellationToken ct = default)
    {
        var idEmpresa = RequireTenant();
        return await _db.SeguimientosPedidoEcommerce.AsNoTracking()
            .Where(s => s.IDEmpresa == idEmpresa && s.IDPedidoMovil == idPedidoMovil && s.Activo)
            .OrderBy(s => s.FechaEstado)
            .Select(s => new SeguimientoDto(s.ID, s.IDPedidoMovil, s.Estado, s.Mensaje, s.FechaEstado, s.CodigoTracking))
            .ToListAsync(ct);
    }

    public async Task AdminAddTrackingAsync(
        long idPedidoMovil, string estado, string? mensaje, string? codigoTracking, CancellationToken ct = default)
    {
        var idEmpresa = RequireTenant();
        var pedido = await _db.PedidosMovil
            .FirstOrDefaultAsync(p => p.ID == idPedidoMovil && p.IDEmpresa == idEmpresa, ct)
            ?? throw new InvalidOperationException("Pedido no encontrado.");

        var next = Datamex.ECommerce.Infrastructure.Orders.FulfillmentEstado.Normalize(estado);
        var last = await _db.SeguimientosPedidoEcommerce.AsNoTracking()
            .Where(s => s.IDEmpresa == idEmpresa && s.IDPedidoMovil == idPedidoMovil && s.Activo)
            .OrderByDescending(s => s.FechaEstado)
            .ThenByDescending(s => s.ID)
            .Select(s => s.Estado)
            .FirstOrDefaultAsync(ct);

        if (!Datamex.ECommerce.Infrastructure.Orders.FulfillmentEstado.CanTransition(last, next))
            throw new InvalidOperationException(
                $"Transición de fulfillment no permitida: '{last ?? "(inicio)"}' → '{next}'.");

        _db.SeguimientosPedidoEcommerce.Add(new SeguimientoPedidoEcommerce
        {
            IDEmpresa = idEmpresa,
            IDPedidoMovil = idPedidoMovil,
            IDCarritoCompra = null,
            Estado = next,
            Mensaje = Truncate(mensaje, 500),
            CodigoTracking = Truncate(codigoTracking, 100),
            FechaEstado = DateTime.UtcNow,
            Activo = true,
            UltModificacion = DateTime.UtcNow,
            UserName = "admin"
        });
        Datamex.ECommerce.Infrastructure.Orders.FulfillmentEstado.ApplyPedidoSideEffect(pedido, next);
        await _db.SaveChangesAsync(ct);

        var dto = new SeguimientoDto(
            // ID may be 0 until save — reload last
            await _db.SeguimientosPedidoEcommerce.AsNoTracking()
                .Where(s => s.IDEmpresa == idEmpresa && s.IDPedidoMovil == idPedidoMovil)
                .OrderByDescending(s => s.ID).Select(s => s.ID).FirstAsync(ct),
            idPedidoMovil, next, Truncate(mensaje, 500), DateTime.UtcNow, Truncate(codigoTracking, 100));
        try { await _trackingRealtime.NotifyTrackingAsync(idEmpresa, idPedidoMovil, dto, ct); }
        catch { /* no bloquear tracking */ }
    }

    public async Task<string> EnsureTrackingShareTokenAsync(
        long idUsuario, long idPedidoMovil, CancellationToken ct = default)
    {
        var idEmpresa = RequireTenant();
        var cuentaIds = await _db.UsuariosEcommerceCuenta.AsNoTracking()
            .Where(c => c.IDEmpresa == idEmpresa && c.IDUsuarioEcommerce == idUsuario && c.Activo)
            .Select(c => c.IDCuenta)
            .ToListAsync(ct);

        var owns = cuentaIds.Count > 0 && await _db.CarritosCompra.AsNoTracking()
            .AnyAsync(c => c.IDEmpresa == idEmpresa
                           && c.IDPedidoMovil == idPedidoMovil
                           && c.IDCuenta != null
                           && cuentaIds.Contains(c.IDCuenta.Value), ct);
        if (!owns)
            throw new InvalidOperationException("Pedido no encontrado o no pertenece a tu cuenta.");

        var pedido = await _db.PedidosMovil
            .FirstOrDefaultAsync(p => p.ID == idPedidoMovil && p.IDEmpresa == idEmpresa, ct)
            ?? throw new InvalidOperationException("Pedido no encontrado.");

        if (!string.IsNullOrWhiteSpace(pedido.TokenTrackingPublico))
            return pedido.TokenTrackingPublico;

        pedido.TokenTrackingPublico = Convert.ToHexString(System.Security.Cryptography.RandomNumberGenerator.GetBytes(16)).ToLowerInvariant();
        pedido.UltModificacion = DateTime.UtcNow;
        pedido.UserName = "share-token";
        await _db.SaveChangesAsync(ct);
        return pedido.TokenTrackingPublico;
    }

    public async Task<TrackingPublicoDto?> GetTrackingByShareTokenAsync(string token, CancellationToken ct = default)
    {
        var idEmpresa = RequireTenant();
        token = (token ?? "").Trim().ToLowerInvariant();
        if (token.Length < 16) return null;

        var pedido = await _db.PedidosMovil.AsNoTracking()
            .FirstOrDefaultAsync(p => p.IDEmpresa == idEmpresa && p.TokenTrackingPublico == token, ct);
        if (pedido is null) return null;

        var eventos = await _db.SeguimientosPedidoEcommerce.AsNoTracking()
            .Where(s => s.IDEmpresa == idEmpresa && s.IDPedidoMovil == pedido.ID && s.Activo)
            .OrderByDescending(s => s.FechaEstado)
            .ThenByDescending(s => s.ID)
            .Select(s => new SeguimientoDto(s.ID, s.IDPedidoMovil, s.Estado, s.Mensaje, s.FechaEstado, s.CodigoTracking))
            .ToListAsync(ct);

        var last = eventos.FirstOrDefault();
        return new TrackingPublicoDto(
            pedido.NroPedido,
            last?.Estado ?? "preparando",
            last?.Mensaje,
            last?.FechaEstado,
            eventos);
    }

    public async Task<DevolucionDto> CrearDevolucionAsync(long idUsuario, CrearDevolucionRequest request, CancellationToken ct = default)
        => await CrearDevolucionInternalAsync(idUsuario, request, null, null, null, ct);

    public async Task<DevolucionDto> CrearDevolucionConAdjuntoAsync(
        long idUsuario,
        CrearDevolucionRequest request,
        Stream file,
        string contentType,
        string fileName,
        CancellationToken ct = default)
        => await CrearDevolucionInternalAsync(idUsuario, request, file, contentType, fileName, ct);

    private async Task<DevolucionDto> CrearDevolucionInternalAsync(
        long idUsuario,
        CrearDevolucionRequest request,
        Stream? file,
        string? contentType,
        string? fileName,
        CancellationToken ct)
    {
        var idEmpresa = RequireTenant();
        var user = await RequireUserAsync(idEmpresa, idUsuario, ct);
        var cuentaIds = user.UsuarioEcommerceCuentas.Where(c => c.Activo).Select(c => c.IDCuenta).ToList();
        var owns = await _db.CarritosCompra.AsNoTracking()
            .AnyAsync(c => c.IDEmpresa == idEmpresa
                           && c.IDPedidoMovil == request.IdPedidoMovil
                           && c.IDCuenta != null
                           && cuentaIds.Contains(c.IDCuenta.Value), ct);
        if (!owns)
            throw new InvalidOperationException("No podés solicitar devolución de un pedido ajeno.");

        var pedido = await _db.PedidosMovil.AsNoTracking()
            .FirstOrDefaultAsync(p => p.ID == request.IdPedidoMovil && p.IDEmpresa == idEmpresa, ct)
            ?? throw new InvalidOperationException("Pedido no encontrado.");
        if (pedido.Estado == EstadoPedidoMovil.Anulado)
            throw new InvalidOperationException("Ese pedido está cancelado; no admite devolución.");

        var yaAbierta = await _db.DevolucionesEcommerce.AsNoTracking()
            .AnyAsync(d => d.IDEmpresa == idEmpresa
                           && d.IDPedidoMovil == request.IdPedidoMovil
                           && d.Activo
                           && (d.Estado == "solicitada" || d.Estado == "aprobada"), ct);
        if (yaAbierta)
            throw new InvalidOperationException("Ya hay una devolución abierta para ese pedido.");

        string? artNombre = null;
        if (request.IdArticulo is > 0)
        {
            artNombre = await _db.Articulos.AsNoTracking()
                .Where(a => a.ID == request.IdArticulo && a.IDEmpresa == idEmpresa)
                .Select(a => a.DescripcionArticulo)
                .FirstOrDefaultAsync(ct);
        }

        string? urlAdjunto = null;
        string? mime = null;
        string? nombreArchivo = null;
        if (file is not null)
        {
            var stored = await _media.SaveBrandingAssetAsync(
                idEmpresa, "devoluciones", file, contentType ?? "application/octet-stream",
                fileName ?? "foto.jpg", ct);
            urlAdjunto = stored.PublicUrl;
            mime = stored.ContentType;
            nombreArchivo = Truncate(stored.OriginalFileName, 260);
        }

        var entity = new DevolucionEcommerce
        {
            IDEmpresa = idEmpresa,
            IDUsuarioEcommerce = idUsuario,
            IDCuenta = user.UsuarioEcommerceCuentas.OrderByDescending(c => c.EsPrincipal).FirstOrDefault()?.IDCuenta,
            Email = user.Email ?? user.Usuario,
            IDPedidoMovil = request.IdPedidoMovil,
            IDArticulo = request.IdArticulo,
            Motivo = Truncate(request.Motivo, 200) ?? "Sin motivo",
            Detalle = Truncate(request.Detalle, 1000),
            Estado = "solicitada",
            FechaSolicitud = DateTime.UtcNow,
            UrlAdjunto = urlAdjunto,
            MimeType = mime,
            NombreArchivo = nombreArchivo,
            Activo = true,
            UltModificacion = DateTime.UtcNow,
            UserName = user.Email
        };
        _db.DevolucionesEcommerce.Add(entity);
        await _db.SaveChangesAsync(ct);

        var cfg = await _db.EmpresaConfiguracionesEcommerce.AsNoTracking()
            .FirstOrDefaultAsync(c => c.ID == idEmpresa, ct);
        var autoErp = TenantIntegrationMode.Resolve(cfg, IntegrationMode.Standalone) == IntegrationMode.Integrated
                      && (cfg?.ProcesaPedidosERP ?? false);
        if (autoErp)
        {
            await TryCerrarDevolucionConErpAsync(entity, ct, requireErpSuccess: true);
            await _db.SaveChangesAsync(ct);
        }

        return MapDevolucion(entity, pedido.NroPedido, artNombre);
    }

    public async Task<IReadOnlyList<DevolucionDto>> MisDevolucionesAsync(long idUsuario, CancellationToken ct = default)
    {
        var idEmpresa = RequireTenant();
        return await (
            from d in _db.DevolucionesEcommerce.AsNoTracking()
            join p in _db.PedidosMovil.AsNoTracking() on d.IDPedidoMovil equals p.ID into pj
            from p in pj.DefaultIfEmpty()
            join a in _db.Articulos.AsNoTracking() on d.IDArticulo equals a.ID into aj
            from a in aj.DefaultIfEmpty()
            where d.IDEmpresa == idEmpresa && d.IDUsuarioEcommerce == idUsuario && d.Activo
            orderby d.FechaSolicitud descending
            select new DevolucionDto(
                d.ID, d.IDPedidoMovil, d.IDArticulo, d.Motivo, d.Detalle, d.Estado, d.FechaSolicitud,
                p != null ? p.NroPedido : null, d.Email, a != null ? a.DescripcionArticulo : null, d.UrlAdjunto,
                d.IdNotaCreditoErp, null, d.IdNotaCreditoErp != null, d.ErpMensaje)
        ).ToListAsync(ct);
    }

    public async Task<IReadOnlyList<DevolucionDto>> AdminDevolucionesAsync(CancellationToken ct = default)
    {
        var idEmpresa = RequireTenant();
        return await (
            from d in _db.DevolucionesEcommerce.AsNoTracking()
            join p in _db.PedidosMovil.AsNoTracking() on d.IDPedidoMovil equals p.ID into pj
            from p in pj.DefaultIfEmpty()
            join a in _db.Articulos.AsNoTracking() on d.IDArticulo equals a.ID into aj
            from a in aj.DefaultIfEmpty()
            where d.IDEmpresa == idEmpresa && d.Activo
            orderby d.FechaSolicitud descending
            select new DevolucionDto(
                d.ID, d.IDPedidoMovil, d.IDArticulo, d.Motivo, d.Detalle, d.Estado, d.FechaSolicitud,
                p != null ? p.NroPedido : null, d.Email, a != null ? a.DescripcionArticulo : null, d.UrlAdjunto,
                d.IdNotaCreditoErp, null, d.IdNotaCreditoErp != null, d.ErpMensaje)
        ).ToListAsync(ct);
    }

    public async Task AdminUpdateDevolucionAsync(long id, string estado, CancellationToken ct = default)
    {
        var idEmpresa = RequireTenant();
        var d = await _db.DevolucionesEcommerce.FirstOrDefaultAsync(x => x.ID == id && x.IDEmpresa == idEmpresa, ct)
                ?? throw new InvalidOperationException("Devolución no encontrada.");
        var next = (estado ?? "").Trim().ToLowerInvariant();
        if (next is not ("solicitada" or "aprobada" or "rechazada" or "completada"))
            throw new ArgumentException("Estado inválido. Usá: solicitada, aprobada, rechazada, completada.");
        if (d.Estado is "completada" or "rechazada")
            throw new InvalidOperationException($"La devolución ya está {d.Estado}; no se puede cambiar.");
        if (next == "completada" && d.Estado != "aprobada" && d.Estado != "solicitada")
            throw new InvalidOperationException("Solo se puede completar una devolución abierta.");

        // Aprobar / completar → write-back ERP (NC o anulación pre-factura).
        if (next is "aprobada" or "completada")
        {
            var ok = await TryCerrarDevolucionConErpAsync(d, ct);
            if (!ok)
                throw new InvalidOperationException(d.ErpMensaje ?? "No se pudo procesar la devolución en el ERP.");
            next = "completada";
        }

        d.Estado = next;
        d.UltModificacion = DateTime.UtcNow;
        d.UserName = "admin";
        await _db.SaveChangesAsync(ct);
    }

    /// <summary>
    /// Mismo camino que el ERP: MotivoNDNC tipo DEVOLUCION + NC sobre la factura,
    /// o anulación si aún no hay factura fiscal.
    /// </summary>
    private async Task<bool> TryCerrarDevolucionConErpAsync(
        DevolucionEcommerce d, CancellationToken ct, bool requireErpSuccess = false)
    {
        var idEmpresa = d.IDEmpresa;
        var erp = await _erpLife.DevolverPedidoAsync(
            idEmpresa, d.IDPedidoMovil, d.Motivo + (string.IsNullOrWhiteSpace(d.Detalle) ? "" : $" — {d.Detalle}"), ct);
        d.ErpMensaje = Truncate(erp.Mensaje, 500);
        d.IdNotaCreditoErp = erp.IdNotaCreditoErp;
        d.ErpProcesado = erp.Ok || erp.Skipped;
        if (!erp.Ok && !erp.Skipped)
            return false;
        if (requireErpSuccess && !erp.Ok)
            return false;

        var pedido = await _db.PedidosMovil.FirstOrDefaultAsync(p => p.ID == d.IDPedidoMovil && p.IDEmpresa == idEmpresa, ct);
        if (pedido is not null && pedido.Estado != EstadoPedidoMovil.Anulado)
        {
            pedido.Estado = EstadoPedidoMovil.Anulado;
            pedido.UltModificacion = DateTime.UtcNow;
            pedido.UserName = "ecom-devolucion";
        }

        var cart = await _db.CarritosCompra
            .Where(c => c.IDEmpresa == idEmpresa && c.IDPedidoMovil == d.IDPedidoMovil)
            .OrderByDescending(c => c.ID)
            .FirstOrDefaultAsync(ct);
        if (cart is not null && cart.Estado != EstadoCarrito.Anulado)
        {
            cart.Estado = EstadoCarrito.Anulado;
            cart.UltModificacion = DateTime.UtcNow;
        }

        d.Estado = "completada";
        d.UltModificacion = DateTime.UtcNow;
        return true;
    }

    private static DevolucionDto MapDevolucion(DevolucionEcommerce d, long? nroPedido, string? artNombre) =>
        new(d.ID, d.IDPedidoMovil, d.IDArticulo, d.Motivo, d.Detalle, d.Estado, d.FechaSolicitud,
            nroPedido, d.Email, artNombre, d.UrlAdjunto,
            d.IdNotaCreditoErp, null, d.IdNotaCreditoErp is > 0, d.ErpMensaje);

    public async Task<ChatConversacionDto> GetOrCreateMiConversacionAsync(
        long idUsuario, string asunto = "consulta", long? idPedidoMovil = null, CancellationToken ct = default)
    {
        var conv = await EnsureConversacionAsync(idUsuario, asunto, idPedidoMovil, null, forzarNueva: false, ct);
        return MapConv(conv);
    }

    public async Task<IReadOnlyList<ChatConversacionDto>> MisConversacionesAsync(long idUsuario, CancellationToken ct = default)
    {
        var idEmpresa = RequireTenant();
        return await _db.ChatsConversacionEcommerce.AsNoTracking()
            .Where(c => c.IDEmpresa == idEmpresa && c.IDUsuarioEcommerce == idUsuario && c.Activo)
            .OrderByDescending(c => c.FechaUltimoMensaje)
            .Select(c => new ChatConversacionDto(
                c.ID, c.IDUsuarioEcommerce, c.Email, c.NombreCliente, c.Asunto, c.Estado,
                c.IDPedidoMovil, c.IDAdminAsignado, c.NombreAdminAsignado,
                c.FechaInicio, c.FechaUltimoMensaje, c.PreviewUltimoMensaje,
                c.NoLeidosCliente, c.NoLeidosSoporte))
            .ToListAsync(ct);
    }

    public async Task<ChatMensajeDto> EnviarChatAsync(long idUsuario, EnviarChatRequest request, CancellationToken ct = default)
    {
        var msg = Truncate(request.Mensaje, 2000) ?? "";
        if (msg.Length < 1)
            throw new ArgumentException("Mensaje vacio.");
        var conv = await EnsureConversacionAsync(
            idUsuario, request.Asunto, request.IdPedidoMovil, request.IdConversacion, request.ForzarNueva, ct);
        return await AppendMensajeAsync(conv, "cliente", "texto", msg, null, null, null, null, null, ct);
    }

    public async Task<ChatMensajeDto> EnviarChatConAdjuntoAsync(
        long idUsuario, Stream file, string contentType, string fileName,
        string? mensaje, string asunto, long? idConversacion, long? idPedidoMovil,
        bool forzarNueva = false, CancellationToken ct = default)
    {
        var conv = await EnsureConversacionAsync(idUsuario, asunto, idPedidoMovil, idConversacion, forzarNueva, ct);
        var stored = await _media.SaveChatAttachmentAsync(RequireTenant(), conv.ID, file, contentType, fileName, ct);
        var caption = Truncate(mensaje, 2000) ?? "";
        return await AppendMensajeAsync(
            conv, "cliente", "imagen",
            string.IsNullOrWhiteSpace(caption) ? "[Foto]" : caption,
            stored.PublicUrl, stored.ContentType, stored.OriginalFileName, null, null, ct);
    }

    public async Task<IReadOnlyList<ChatMensajeDto>> MisChatsAsync(
        long idUsuario, long? idPedidoMovil, long? idConversacion = null, CancellationToken ct = default)
    {
        var idEmpresa = RequireTenant();
        var q = _db.ChatsSoporteMensajeEcommerce.AsNoTracking()
            .Where(c => c.IDEmpresa == idEmpresa && c.IDUsuarioEcommerce == idUsuario && c.Activo);
        if (idConversacion.HasValue)
            q = q.Where(c => c.IDConversacion == idConversacion);
        else if (idPedidoMovil.HasValue)
            q = q.Where(c => c.IDPedidoMovil == idPedidoMovil);
        var rows = await q.OrderBy(c => c.FechaMensaje).Take(500).ToListAsync(ct);
        return rows.Select(MapChat).ToList();
    }

    public async Task MarcarLeidosClienteAsync(long idUsuario, long idConversacion, CancellationToken ct = default)
    {
        var idEmpresa = RequireTenant();
        var conv = await _db.ChatsConversacionEcommerce
            .FirstOrDefaultAsync(c => c.ID == idConversacion && c.IDEmpresa == idEmpresa && c.IDUsuarioEcommerce == idUsuario, ct)
            ?? throw new InvalidOperationException("Conversacion no encontrada.");
        var unread = await _db.ChatsSoporteMensajeEcommerce
            .Where(m => m.IDConversacion == idConversacion && m.Origen == "soporte" && !m.Leido && m.Activo)
            .ToListAsync(ct);
        foreach (var m in unread) m.Leido = true;
        conv.NoLeidosCliente = 0;
        conv.UltModificacion = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);
    }

    public async Task CerrarConversacionClienteAsync(long idUsuario, long idConversacion, CancellationToken ct = default)
    {
        var idEmpresa = RequireTenant();
        var conv = await _db.ChatsConversacionEcommerce
            .FirstOrDefaultAsync(c =>
                c.ID == idConversacion && c.IDEmpresa == idEmpresa && c.IDUsuarioEcommerce == idUsuario && c.Activo, ct)
            ?? throw new InvalidOperationException("Conversacion no encontrada.");
        conv.Estado = "cerrada";
        conv.UltModificacion = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);
    }

    public async Task<IReadOnlyList<ChatConversacionDto>> AdminListConversacionesAsync(
        string? estado = null, string? q = null, CancellationToken ct = default)
    {
        var idEmpresa = RequireTenant();
        var query = _db.ChatsConversacionEcommerce.AsNoTracking()
            .Where(c => c.IDEmpresa == idEmpresa && c.Activo);
        if (!string.IsNullOrWhiteSpace(estado))
            query = query.Where(c => c.Estado == estado.Trim().ToLowerInvariant());
        if (!string.IsNullOrWhiteSpace(q))
        {
            var term = q.Trim().ToLowerInvariant();
            query = query.Where(c =>
                (c.Email != null && c.Email.ToLower().Contains(term))
                || (c.NombreCliente != null && c.NombreCliente.ToLower().Contains(term))
                || (c.PreviewUltimoMensaje != null && c.PreviewUltimoMensaje.ToLower().Contains(term)));
        }
        return await query.OrderByDescending(c => c.FechaUltimoMensaje)
            .Take(200)
            .Select(c => new ChatConversacionDto(
                c.ID, c.IDUsuarioEcommerce, c.Email, c.NombreCliente, c.Asunto, c.Estado,
                c.IDPedidoMovil, c.IDAdminAsignado, c.NombreAdminAsignado,
                c.FechaInicio, c.FechaUltimoMensaje, c.PreviewUltimoMensaje,
                c.NoLeidosCliente, c.NoLeidosSoporte))
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<ChatMensajeDto>> AdminGetConversacionMensajesAsync(long idConversacion, CancellationToken ct = default)
    {
        var idEmpresa = RequireTenant();
        var exists = await _db.ChatsConversacionEcommerce.AsNoTracking()
            .AnyAsync(c => c.ID == idConversacion && c.IDEmpresa == idEmpresa, ct);
        if (!exists) throw new InvalidOperationException("Conversacion no encontrada.");

        var rows = await _db.ChatsSoporteMensajeEcommerce.AsNoTracking()
            .Where(m => m.IDEmpresa == idEmpresa && m.IDConversacion == idConversacion && m.Activo)
            .OrderBy(m => m.FechaMensaje)
            .Take(500)
            .ToListAsync(ct);

        var conv = await _db.ChatsConversacionEcommerce
            .FirstAsync(c => c.ID == idConversacion && c.IDEmpresa == idEmpresa, ct);
        var unread = await _db.ChatsSoporteMensajeEcommerce
            .Where(m => m.IDConversacion == idConversacion && m.Origen == "cliente" && !m.Leido && m.Activo)
            .ToListAsync(ct);
        if (unread.Count > 0)
        {
            foreach (var m in unread) m.Leido = true;
            conv.NoLeidosSoporte = 0;
            await _db.SaveChangesAsync(ct);
        }

        return rows.Select(MapChat).ToList();
    }

    public async Task<ChatMensajeDto> AdminResponderConversacionAsync(
        long idConversacion, string? mensaje, long? idAdmin, string? nombreAdmin, string? nuevoEstado, CancellationToken ct = default)
    {
        var text = Truncate(mensaje, 2000) ?? "";
        if (text.Length < 1)
            throw new ArgumentException("Mensaje vacio.");
        var conv = await RequireConversacionAsync(idConversacion, ct);
        var agent = await ResolveNombreAdminAsync(idAdmin, nombreAdmin, ct);
        if (idAdmin.HasValue)
        {
            conv.IDAdminAsignado = idAdmin;
            conv.NombreAdminAsignado = agent;
        }
        if (!string.IsNullOrWhiteSpace(nuevoEstado))
            conv.Estado = NormalizeEstadoConv(nuevoEstado);
        else if (conv.Estado == "abierta")
            conv.Estado = "en_curso";
        return await AppendMensajeAsync(conv, "soporte", "texto", text, null, null, null, idAdmin, agent, ct);
    }

    public async Task<ChatMensajeDto> AdminResponderConAdjuntoAsync(
        long idConversacion, Stream file, string contentType, string fileName,
        string? mensaje, long? idAdmin, string? nombreAdmin, CancellationToken ct = default)
    {
        var conv = await RequireConversacionAsync(idConversacion, ct);
        var agent = await ResolveNombreAdminAsync(idAdmin, nombreAdmin, ct);
        if (idAdmin.HasValue)
        {
            conv.IDAdminAsignado = idAdmin;
            conv.NombreAdminAsignado = agent;
        }
        if (conv.Estado == "abierta")
            conv.Estado = "en_curso";
        var stored = await _media.SaveChatAttachmentAsync(RequireTenant(), conv.ID, file, contentType, fileName, ct);
        var caption = Truncate(mensaje, 2000) ?? "";
        return await AppendMensajeAsync(
            conv, "soporte", "imagen",
            string.IsNullOrWhiteSpace(caption) ? "[Foto]" : caption,
            stored.PublicUrl, stored.ContentType, stored.OriginalFileName, idAdmin, agent, ct);
    }

    public async Task AdminAsignarConversacionAsync(long idConversacion, long? idAdmin, string? nombreAdmin, CancellationToken ct = default)
    {
        var conv = await RequireConversacionAsync(idConversacion, ct);
        var agent = await ResolveNombreAdminAsync(idAdmin, nombreAdmin, ct);
        conv.IDAdminAsignado = idAdmin;
        conv.NombreAdminAsignado = agent;
        if (idAdmin.HasValue && conv.Estado == "abierta")
            conv.Estado = "en_curso";
        conv.UltModificacion = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);
    }

    public async Task AdminCambiarEstadoConversacionAsync(long idConversacion, string estado, CancellationToken ct = default)
    {
        var conv = await RequireConversacionAsync(idConversacion, ct);
        conv.Estado = NormalizeEstadoConv(estado);
        conv.UltModificacion = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);
    }

    public async Task<IReadOnlyList<ChatMensajeDto>> AdminChatsAsync(CancellationToken ct = default)
    {
        var idEmpresa = RequireTenant();
        var rows = await _db.ChatsSoporteMensajeEcommerce.AsNoTracking()
            .Where(c => c.IDEmpresa == idEmpresa && c.Activo)
            .OrderByDescending(c => c.FechaMensaje)
            .Take(200)
            .ToListAsync(ct);
        return rows.Select(MapChat).ToList();
    }

    public async Task AdminResponderChatAsync(long idMensajeHilo, string mensaje, CancellationToken ct = default)
    {
        var idEmpresa = RequireTenant();
        var origen = await _db.ChatsSoporteMensajeEcommerce.AsNoTracking()
            .FirstOrDefaultAsync(c => c.ID == idMensajeHilo && c.IDEmpresa == idEmpresa, ct)
            ?? throw new InvalidOperationException("Mensaje no encontrado.");
        long convId;
        if (origen.IDConversacion.HasValue)
            convId = origen.IDConversacion.Value;
        else
        {
            var conv = await EnsureConversacionAsync(
                origen.IDUsuarioEcommerce ?? 0, "consulta", origen.IDPedidoMovil, null, forzarNueva: false, ct);
            convId = conv.ID;
        }
        await AdminResponderConversacionAsync(convId, mensaje, null, "soporte", null, ct);
    }

    private async Task<ChatConversacionEcommerce> EnsureConversacionAsync(
        long idUsuario, string? asunto, long? idPedidoMovil, long? idConversacion, bool forzarNueva, CancellationToken ct)
    {
        var idEmpresa = RequireTenant();
        if (idUsuario <= 0)
            throw new InvalidOperationException("Usuario invalido.");
        var user = await RequireUserAsync(idEmpresa, idUsuario, ct);

        if (idConversacion.HasValue && !forzarNueva)
        {
            return await _db.ChatsConversacionEcommerce
                .FirstOrDefaultAsync(c =>
                    c.ID == idConversacion && c.IDEmpresa == idEmpresa && c.IDUsuarioEcommerce == idUsuario && c.Activo, ct)
                ?? throw new InvalidOperationException("Conversacion no encontrada.");
        }

        var asu = NormalizeAsunto(asunto);
        if (!forzarNueva)
        {
            var open = await _db.ChatsConversacionEcommerce
                .Where(c => c.IDEmpresa == idEmpresa
                            && c.IDUsuarioEcommerce == idUsuario
                            && c.Activo
                            && (c.Estado == "abierta" || c.Estado == "en_curso")
                            && c.Asunto == asu)
                .OrderByDescending(c => c.FechaUltimoMensaje)
                .FirstOrDefaultAsync(ct);
            if (open != null)
            {
                if (idPedidoMovil.HasValue) open.IDPedidoMovil = idPedidoMovil;
                return open;
            }
        }

        var nombre = $"{user.Nombre} {user.Apellido}".Trim();
        var conv = new ChatConversacionEcommerce
        {
            IDEmpresa = idEmpresa,
            IDUsuarioEcommerce = idUsuario,
            IDPedidoMovil = idPedidoMovil,
            Email = user.Email ?? user.Usuario,
            NombreCliente = string.IsNullOrWhiteSpace(nombre) ? user.Usuario : nombre,
            Asunto = asu,
            Estado = "abierta",
            FechaInicio = DateTime.UtcNow,
            FechaUltimoMensaje = DateTime.UtcNow,
            PreviewUltimoMensaje = null,
            NoLeidosCliente = 0,
            NoLeidosSoporte = 0,
            Activo = true,
            UltModificacion = DateTime.UtcNow,
            UserName = user.Email
        };
        _db.ChatsConversacionEcommerce.Add(conv);
        await _db.SaveChangesAsync(ct);
        return conv;
    }

    private async Task<ChatConversacionEcommerce> RequireConversacionAsync(long idConversacion, CancellationToken ct)
    {
        var idEmpresa = RequireTenant();
        return await _db.ChatsConversacionEcommerce
            .FirstOrDefaultAsync(c => c.ID == idConversacion && c.IDEmpresa == idEmpresa && c.Activo, ct)
            ?? throw new InvalidOperationException("Conversacion no encontrada.");
    }

    private async Task<ChatMensajeDto> AppendMensajeAsync(
        ChatConversacionEcommerce conv,
        string origen,
        string tipo,
        string mensaje,
        string? urlAdjunto,
        string? mime,
        string? nombreArchivo,
        long? idAdmin,
        string? nombreAdmin,
        CancellationToken ct)
    {
        var now = DateTime.UtcNow;
        var entity = new ChatSoporteMensajeEcommerce
        {
            IDEmpresa = conv.IDEmpresa,
            IDConversacion = conv.ID,
            IDUsuarioEcommerce = conv.IDUsuarioEcommerce,
            IDPedidoMovil = conv.IDPedidoMovil,
            IDAdminUsuario = idAdmin,
            Email = conv.Email,
            Origen = origen,
            TipoMensaje = tipo,
            Mensaje = mensaje,
            UrlAdjunto = urlAdjunto,
            MimeType = mime,
            NombreArchivo = Truncate(nombreArchivo, 260),
            FechaMensaje = now,
            Leido = false,
            Activo = true,
            UltModificacion = now,
            UserName = origen == "soporte" ? (nombreAdmin ?? "soporte") : conv.Email
        };
        _db.ChatsSoporteMensajeEcommerce.Add(entity);

        conv.FechaUltimoMensaje = now;
        conv.PreviewUltimoMensaje = Truncate(mensaje, 240);
        conv.UltModificacion = now;
        if (origen == "cliente")
            conv.NoLeidosSoporte += 1;
        else
            conv.NoLeidosCliente += 1;

        await _db.SaveChangesAsync(ct);
        var dto = MapChat(entity);
        try
        {
            await _realtime.NotifyMessageAsync(conv.IDEmpresa, conv.ID, dto, ct);
        }
        catch
        {
            // El chat REST no debe fallar si SignalR no está disponible.
        }
        return dto;
    }

    private static string NormalizeAsunto(string? asunto)
    {
        var a = (asunto ?? "consulta").Trim().ToLowerInvariant();
        return a is "consulta" or "reclamo" or "pedido" or "general" ? a : "consulta";
    }

    private static string NormalizeEstadoConv(string? estado)
    {
        var e = (estado ?? "abierta").Trim().ToLowerInvariant();
        return e is "abierta" or "en_curso" or "resuelta" or "cerrada" ? e : "abierta";
    }

    private static ChatConversacionDto MapConv(ChatConversacionEcommerce c) => new(
        c.ID, c.IDUsuarioEcommerce, c.Email, c.NombreCliente, c.Asunto, c.Estado,
        c.IDPedidoMovil, c.IDAdminAsignado, c.NombreAdminAsignado,
        c.FechaInicio, c.FechaUltimoMensaje, c.PreviewUltimoMensaje,
        c.NoLeidosCliente, c.NoLeidosSoporte);

    private async Task<IReadOnlyList<RecomendacionDto>> MapRecomendacionesAsync(List<long> ids, string motivo, CancellationToken ct)
    {
        if (ids.Count == 0) return Array.Empty<RecomendacionDto>();
        var arts = await _db.Articulos.AsNoTracking()
            .Where(a => ids.Contains(a.ID) && a.Activo && a.ActivoEcommerce)
            .ToListAsync(ct);
        var imgs = (await _db.ArticuloImagenes.AsNoTracking()
            .Where(i => ids.Contains(i.IDArticulo) && i.Activo)
            .OrderByDescending(i => i.EsPrincipal).ThenBy(i => i.Orden)
            .Select(i => new { i.IDArticulo, i.Url }).ToListAsync(ct))
            .GroupBy(x => x.IDArticulo).ToDictionary(g => g.Key, g => g.First().Url);
        var ratings = await _db.ResenasArticuloEcommerce.AsNoTracking()
            .Where(r => ids.Contains(r.IDArticulo) && r.Activo && r.Aprobado)
            .GroupBy(r => r.IDArticulo)
            .Select(g => new { Id = g.Key, Avg = g.Average(x => x.Puntuacion), Cnt = g.Count() })
            .ToDictionaryAsync(x => x.Id, ct);

        var order = ids.Select((id, i) => (id, i)).ToDictionary(x => x.id, x => x.i);
        return arts.OrderBy(a => order.GetValueOrDefault(a.ID, 999))
            .Select(a =>
            {
                ratings.TryGetValue(a.ID, out var r);
                imgs.TryGetValue(a.ID, out var img);
                return new RecomendacionDto(
                    a.ID, a.SKU, a.DescripcionArticulo, img, null,
                    r is null ? null : Math.Round(r.Avg, 2), r?.Cnt ?? 0, motivo);
            }).ToList();
    }

    private static CuponDto MapCupon(CuponEcommerce c) => new(
        c.ID, c.Codigo, c.Descripcion, c.TipoDescuento, c.ValorDescuento, c.MontoMinimo,
        c.MontoMaximoDescuento, c.CanalEcommerce, c.FechaInicio, c.FechaFin,
        c.UsosMaximos, c.UsosActuales, c.Activo);

    private static DevolucionDto MapDev(DevolucionEcommerce d, long? nroPedido = null, string? articuloNombre = null) =>
        new(d.ID, d.IDPedidoMovil, d.IDArticulo, d.Motivo, d.Detalle, d.Estado, d.FechaSolicitud, nroPedido, d.Email, articuloNombre, d.UrlAdjunto);

    private static ChatMensajeDto MapChat(ChatSoporteMensajeEcommerce c) =>
        new(
            c.ID,
            c.IDConversacion,
            c.IDPedidoMovil,
            c.Email,
            c.Origen,
            c.TipoMensaje,
            c.Mensaje,
            c.FechaMensaje,
            c.Leido,
            string.IsNullOrWhiteSpace(c.UrlAdjunto)
                ? null
                : new ChatAdjuntoDto(c.UrlAdjunto, c.MimeType, c.NombreArchivo),
            string.Equals(c.Origen, "soporte", StringComparison.OrdinalIgnoreCase)
                ? (string.IsNullOrWhiteSpace(c.UserName) ? "Soporte" : c.UserName)
                : null);

    /// <summary>Resuelve nombre visible del agente (Callbell-like) desde AdminUsuario.</summary>
    private async Task<string> ResolveNombreAdminAsync(long? idAdmin, string? fallback, CancellationToken ct)
    {
        if (idAdmin.HasValue)
        {
            var u = await _db.AdminUsuarios.AsNoTracking()
                .Where(x => x.ID == idAdmin.Value)
                .Select(x => new { x.Nombre, x.Usuario })
                .FirstOrDefaultAsync(ct);
            if (u is not null)
            {
                var preferred = !string.IsNullOrWhiteSpace(u.Nombre) ? u.Nombre : u.Usuario;
                var resolved = Truncate(preferred, 200);
                if (!string.IsNullOrWhiteSpace(resolved))
                    return resolved;
            }
        }

        return Truncate(fallback, 200) ?? "Soporte";
    }

    private async Task<UsuarioEcommerce> RequireUserAsync(long idEmpresa, long idUsuario, CancellationToken ct) =>
        await _db.UsuariosEcommerce.Include(u => u.UsuarioEcommerceCuentas)
            .FirstOrDefaultAsync(u => u.ID == idUsuario && u.IDEmpresa == idEmpresa, ct)
        ?? throw new InvalidOperationException("Usuario no encontrado.");

    private static string NormalizeCanal(string? canal) =>
        string.Equals(canal, "B2B", StringComparison.OrdinalIgnoreCase) ? "B2B" : "B2C";

    private static string? Truncate(string? v, int max) =>
        string.IsNullOrWhiteSpace(v) ? null : (v.Trim().Length <= max ? v.Trim() : v.Trim()[..max]);

    private long RequireTenant()
    {
        if (!_tenant.IsResolved || !_tenant.IDEmpresa.HasValue)
            throw new InvalidOperationException("Tenant no resuelto.");
        return _tenant.IDEmpresa.Value;
    }
}
