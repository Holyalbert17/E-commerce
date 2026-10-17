using Datamex.ECommerce.Application.Abstractions;
using Datamex.ECommerce.Application.Account;
using Datamex.ECommerce.Application.Checkout;
using Datamex.ECommerce.Application.Geography;
using Datamex.ECommerce.Application.Integration;
using Datamex.ECommerce.Application.Inventory;
using Datamex.ECommerce.Application.Notifications;
using Datamex.ECommerce.Application.Options;
using Datamex.ECommerce.Application.Payments;
using Datamex.ECommerce.Application.Shipping;
using Datamex.ECommerce.Application.Sync;
using Datamex.ECommerce.Domain.Entities;
using Datamex.ECommerce.Domain.Enums;
using Datamex.ECommerce.Infrastructure.Cart;
using Datamex.ECommerce.Infrastructure.Persistence;
using Datamex.ECommerce.Infrastructure.Pricing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Datamex.ECommerce.Infrastructure.Checkout;

/// <summary>
/// Checkout alineado a CarritoCompraService.FinalizarCarrito del ERP:
/// valida mínimo + stock, cierra carrito, crea PedidoMovil (OrigenPedido=ECOMMERCE).
/// Pago online: multi_buy Bancard (stub/live) dejando carrito ABIERTO hasta confirmación.
/// Write-back ERP: al confirmar INSERT PedidoMovil en tiempo real (paridad FinalizarCarrito).
/// </summary>
public sealed class CheckoutService : ICheckoutService
{
    private readonly EcomDbContext _db;
    private readonly ITenantContext _tenant;
    private readonly IInventoryService _inventory;
    private readonly IShippingService _shipping;
    private readonly IEmailSender _email;
    private readonly IBancardClient _bancard;
    private readonly ECommerceOptions _options;
    private readonly BancardOptions _bancardOptions;
    private readonly IMayoristaService _mayorista;
    private readonly IGeographyService _geography;
    private readonly IPedidoErpWriteBackDispatcher _writeBack;
    private readonly PentahoOptions _pentaho;
    private readonly ILogger<CheckoutService> _logger;

    public CheckoutService(
        EcomDbContext db,
        ITenantContext tenant,
        IInventoryService inventory,
        IShippingService shipping,
        IEmailSender email,
        IBancardClient bancard,
        IOptions<ECommerceOptions> options,
        IOptions<BancardOptions> bancardOptions,
        IMayoristaService mayorista,
        IGeographyService geography,
        IPedidoErpWriteBackDispatcher writeBack,
        IOptions<PentahoOptions> pentaho,
        ILogger<CheckoutService> logger)
    {
        _db = db;
        _tenant = tenant;
        _inventory = inventory;
        _shipping = shipping;
        _email = email;
        _bancard = bancard;
        _options = options.Value;
        _bancardOptions = bancardOptions.Value;
        _mayorista = mayorista;
        _geography = geography;
        _writeBack = writeBack;
        _pentaho = pentaho.Value;
        _logger = logger;
    }

    public async Task<CheckoutResultDto> CheckoutAsync(
        string sessionId, CheckoutRequest request, CancellationToken ct = default)
    {
        var idEmpresa = RequireTenant();
        var config = await _db.EmpresaConfiguracionesEcommerce
            .FirstOrDefaultAsync(c => c.ID == idEmpresa, ct)
            ?? throw new InvalidOperationException("Configuración ecom no encontrada.");

        var cart = await _db.CarritosCompra
            .Include(c => c.DetalleCarritoCompras)
            .FirstOrDefaultAsync(c =>
                c.IDEmpresa == idEmpresa
                && c.IDSesion == sessionId
                && c.Estado == EstadoCarrito.Abierto
                && !c.Migrado
                && c.Activo, ct)
            ?? throw new InvalidOperationException("No hay carrito abierto.");

        // Pickup → quitar línea de delivery (AjustarTarifa inverso).
        if (!request.Delivery)
        {
            await _shipping.ClearFromCartAsync(sessionId, ct);
            await _db.Entry(cart).ReloadAsync(ct);
        }

        // Reload lines after possible shipping clear
        await _db.Entry(cart).Collection(c => c.DetalleCarritoCompras).LoadAsync(ct);

        var lines = cart.DetalleCarritoCompras.Where(d => d.Activo).ToList();
        if (lines.Count == 0)
            throw new InvalidOperationException("El carrito está vacío.");

        var deliveryArtId = config.IDArticuloDelivery;
        var productLines = deliveryArtId is null or 0
            ? lines
            : lines.Where(l => l.IDArticulo != deliveryArtId.Value).ToList();
        if (productLines.Count == 0)
            throw new InvalidOperationException("Agregá al menos un producto (además del delivery).");

        var artIds = lines.Select(l => l.IDArticulo).Distinct().ToList();
        var inactive = await _db.Articulos.AsNoTracking()
            .Where(a => artIds.Contains(a.ID)
                        && a.ID != deliveryArtId
                        && (!a.Activo || !a.ActivoEcommerce))
            .Select(a => a.DescripcionArticulo)
            .FirstOrDefaultAsync(ct);
        if (inactive is not null)
            throw new InvalidOperationException($"Artículo inactivo en ecom: {inactive}");

        // ERP: MontoTotal > MontoPedidoMinimo; usamos >= para permitir igualar el mínimo.
        if (cart.MontoTotal < config.MontoPedidoMinimo)
            throw new InvalidOperationException(
                $"Monto mínimo de pedido: {config.MontoPedidoMinimo:0.##}. Total actual: {cart.MontoTotal:0.##}");

        // Validar contra espejo ERP (Integrated) o si el tenant controla stock en carrito.
        var integrated = config.ModoIntegracion == (int)Datamex.ECommerce.Domain.Enums.IntegrationMode.Integrated;
        if (config.ControlaStockEnCarrito || integrated)
        {
            foreach (var line in lines)
            {
                if (deliveryArtId.HasValue && line.IDArticulo == deliveryArtId.Value)
                    continue;

                var check = await _inventory.CheckAvailabilityAsync(line.IDArticulo, line.Cantidad, cart.IDDeposito, ct);
                if (!check.Ok)
                    throw new InvalidOperationException(check.Mensaje ?? "Hay artículos sin stock suficiente.");
            }
        }

        if (string.IsNullOrWhiteSpace(request.RazonSocial))
            throw new ArgumentException("Razón social / nombre es obligatorio.");
        if (string.IsNullOrWhiteSpace(request.NumeroDocumento))
            throw new ArgumentException("Documento es obligatorio.");
        if (string.IsNullOrWhiteSpace(request.Email))
            throw new ArgumentException("Email es obligatorio.");

        if (request.Delivery)
        {
            if (deliveryArtId is null or 0
                || lines.All(l => l.IDArticulo != deliveryArtId.Value))
                throw new InvalidOperationException("Seleccioná una tarifa de delivery antes de confirmar.");

            var lugar = !string.IsNullOrWhiteSpace(request.LugarEntrega)
                ? request.LugarEntrega.Trim()
                : cart.LugarEntrega;
            if (string.IsNullOrWhiteSpace(lugar))
                throw new ArgumentException("Lugar de entrega es obligatorio para delivery.");
            request = request with { LugarEntrega = lugar };
        }
        else
        {
            if (cart.IDAgenciaFacturacion <= 0)
                throw new InvalidOperationException("Seleccioná una sucursal de retiro.");
            var open = await _geography.IsAgenciaOpenAsync(cart.IDAgenciaFacturacion, null, ct);
            if (!open.Abierta)
                throw new InvalidOperationException(open.Motivo ?? "La sucursal está cerrada.");
        }

        ValidateTipoComprobante(config, request);

        // Vincular cuenta ERP si el usuario está logueado (UsuarioEcommerceCuenta).
        if (_tenant.IDUsuarioEcommerce.HasValue)
        {
            var cuentaId = await _db.UsuariosEcommerceCuenta.AsNoTracking()
                .Where(c => c.IDEmpresa == idEmpresa
                            && c.IDUsuarioEcommerce == _tenant.IDUsuarioEcommerce.Value
                            && c.Activo)
                .OrderByDescending(c => c.EsPrincipal)
                .Select(c => (long?)c.IDCuenta)
                .FirstOrDefaultAsync(ct);
            if (cuentaId.HasValue)
                cart.IDCuenta = cuentaId;
        }

        var now = DateTime.UtcNow;
        cart.RazonSocial = request.RazonSocial.Trim();
        cart.NumeroDocumento = request.NumeroDocumento.Trim();
        cart.FechaPedido = now;
        cart.FechaPrevistaEntrega = now.AddDays(1);
        cart.TipoEntrega = request.Delivery ? TipoEntregaEcommerce.Delivery : TipoEntregaEcommerce.Pickup;
        cart.TipoPago = request.PagarEnLaEntrega
            ? TipoPagoEcommerce.Entrega
            : TipoPagoEcommerce.Pedido;
        cart.Observacion = BuildObservacion(request);
        cart.UltModificacion = now;

        if (request.Delivery)
        {
            cart.LugarEntrega = request.LugarEntrega?.Trim();
            cart.LatitudEntrega = request.Latitud;
            cart.LongitudEntrega = request.Longitud;
        }
        else
        {
            cart.LugarEntrega = null;
            cart.LatitudEntrega = null;
            cart.LongitudEntrega = null;
        }

        // Flujo normal: online solo si TipoPago=Pedido y Bancard configurado.
        var requierePagoOnline = cart.TipoPago == TipoPagoEcommerce.Pedido
                                 && IsBancardConfigured(config);

        if (requierePagoOnline)
        {
            cart.IDProcesoNegocio = await NextProcesoNegocioAsync(config, ct);
            await _db.SaveChangesAsync(ct);

            var returnUrl = string.IsNullOrWhiteSpace(config.DireccionURLCheckout)
                ? $"{_options.StorefrontBaseUrl.TrimEnd('/')}/pago/{cart.NroPedidoCarritoCompra}"
                : config.DireccionURLCheckout!;

            var buy = await _bancard.CreateMultiBuyAsync(
                config.ClavePublicaProcesadoraTarjeta ?? "demo-public",
                config.ClavePrivadaProcesadoraTarjeta ?? "demo-private",
                cart.IDProcesoNegocio.Value,
                cart.MontoTotal,
                "PYG",
                lines.Count,
                config.CodigoComercio ?? 0,
                config.CodigoSucursal ?? 0,
                "Pedido e-commerce",
                returnUrl,
                returnUrl,
                ct);

            if (!string.Equals(buy.Status, "success", StringComparison.OrdinalIgnoreCase)
                || string.IsNullOrWhiteSpace(buy.ProcessId))
            {
                throw new InvalidOperationException(
                    buy.Description ?? "No se pudo iniciar el pago Bancard.");
            }

            var nowLog = DateTime.UtcNow;
            _db.LogsPagoTarjeta.Add(new LogPagoTarjeta
            {
                IDEmpresa = idEmpresa,
                Activo = true,
                UltModificacion = nowLog,
                UserName = "ecom-checkout",
                IDCarritoCompra = cart.ID,
                ShopProcessId = cart.IDProcesoNegocio.Value,
                ProcessId = buy.ProcessId,
                Monto = cart.MontoTotal,
                Status = "pending",
                Fecha = nowLog
            });
            await _db.SaveChangesAsync(ct);

            return await MapResultAsync(cart, null, true,
                "Redirección a pago online (Bancard).", ct, buy.ProcessId);
        }

        // Flujo pagar en entrega / Bancard Off → cierra + PedidoMovil
        // Si el N° se duplicó (carrera al abrir carrito), reservar uno nuevo antes de cerrar.
        await PedidoNumeroAllocator.EnsureUniqueForCartAsync(_db, cart, ct);
        cart.Estado = EstadoCarrito.Cerrado;

        PedidoMovil? pedido = null;
        if (TenantIntegrationMode.CreatesLocalPedidoMovil(config, _options.IntegrationMode))
        {
            pedido = await CreatePedidoMovilAsync(cart, lines, request, ct);
            cart.IDPedidoMovil = pedido.ID;
        }

        // Standalone: paridad PedidoMovil ERP (Existencia−, PedidoVenta+). Integrated: solo sync.
        if (TenantIntegrationMode.MutatesLocalStock(config, _options.IntegrationMode))
        {
            await _inventory.ApplyPedidoStockAsync(
                cart.IDDeposito,
                lines.Select(l => new StockLineMove(l.IDArticulo, l.Cantidad)).ToList(),
                deliveryArtId,
                ct);
        }

        await RecordCouponUsageAsync(cart, ct);

        await _db.SaveChangesAsync(ct);

        try
        {
            await _mayorista.EvaluateAfterPurchaseAsync(idEmpresa, cart.IDCuenta, ct);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "No se pudo evaluar umbral mayorista tras checkout.");
        }

        await TrySendConfirmationEmailAsync(request, cart, lines, ct);

        if (pedido is not null && config.MigrarPedidosAlGenerar)
        {
            // Tiempo real: visible en ERP al confirmar (fallback Schedule si falla).
            try
            {
                var wb = await _writeBack.FlushPedidoAsync(idEmpresa, pedido.ID, ct);
                if (wb is null || wb.Errores > 0)
                {
                    _logger.LogWarning(
                        "Write-back sync incompleto pedido={Id} ok={Ok} err={Err}; queda reintento.",
                        pedido.ID, wb?.Exitosos, wb?.Errores);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Write-back sync falló pedido={Id}; se encola reintento.", pedido.ID);
                _writeBack.SchedulePedido(idEmpresa, pedido.ID);
            }
        }

        return await MapResultAsync(cart, pedido, false,
            $"Compra finalizada. Pedido N° {cart.NroPedidoCarritoCompra}.", ct);
    }

    public async Task<CheckoutResultDto?> GetOrderAsync(long nroPedido, string sessionId, CancellationToken ct = default)
    {
        var idEmpresa = RequireTenant();
        var cart = await _db.CarritosCompra.AsNoTracking()
            .Include(c => c.DetalleCarritoCompras)
            .FirstOrDefaultAsync(c =>
                c.IDEmpresa == idEmpresa
                && c.NroPedidoCarritoCompra == nroPedido
                && (c.IDSesion == sessionId || c.Estado == EstadoCarrito.Cerrado), ct);

        if (cart is null)
            return null;

        PedidoMovil? pedido = null;
        if (cart.IDPedidoMovil.HasValue)
            pedido = await _db.PedidosMovil.AsNoTracking()
                .FirstOrDefaultAsync(p => p.ID == cart.IDPedidoMovil, ct);

        return await MapResultAsync(cart, pedido, false, "Pedido encontrado.", ct);
    }

    private async Task TrySendConfirmationEmailAsync(
        CheckoutRequest request, CarritoCompra cart, List<DetalleCarritoCompra> lines, CancellationToken ct)
    {
        try
        {
            var artIds = lines.Select(l => l.IDArticulo).Distinct().ToList();
            var names = await _db.Articulos.AsNoTracking()
                .Where(a => artIds.Contains(a.ID))
                .Select(a => new { a.ID, a.DescripcionArticulo })
                .ToDictionaryAsync(a => a.ID, a => a.DescripcionArticulo, ct);

            var emailLines = lines
                .Select(l => (
                    names.GetValueOrDefault(l.IDArticulo, $"Artículo {l.IDArticulo}"),
                    l.Cantidad,
                    l.MontoTotal))
                .ToList();

            await _email.SendOrderConfirmationAsync(new OrderConfirmationEmail(
                request.Email.Trim(),
                $"{request.Nombre.Trim()} {request.Apellido.Trim()}".Trim(),
                cart.NroPedidoCarritoCompra,
                cart.MontoTotal,
                cart.LugarEntrega,
                emailLines), ct);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "No se pudo enviar email de confirmación del pedido {Nro}", cart.NroPedidoCarritoCompra);
        }
    }

    private async Task<PedidoMovil> CreatePedidoMovilAsync(
        CarritoCompra cart,
        List<DetalleCarritoCompra> lines,
        CheckoutRequest request,
        CancellationToken ct)
    {
        var now = DateTime.UtcNow;
        var idCuenta = cart.IDCuenta
            ?? throw new InvalidOperationException(
                "El carrito no tiene cuenta vinculada. Iniciá sesión con una cuenta activada.");
        var idListaPrecioCuenta = await ListaPrecioCuentaHelper.ResolveOrEnsureAssignmentIdAsync(
            _db, cart.IDEmpresa, idCuenta, cart.IDListaPrecio, "ecom-checkout", ct);

        var pedido = new PedidoMovil
        {
            IDEmpresa = cart.IDEmpresa,
            Activo = true,
            UltModificacion = now,
            UserName = "ecom-checkout",
            NroPedido = cart.NroPedidoCarritoCompra,
            NroPedidoDispositivoMovil = cart.NroPedidoCarritoCompra,
            OrigenPedido = OrigenPedidoMovil.Ecommerce,
            IDCuenta = idCuenta,
            IDListaPrecioCuenta = idListaPrecioCuenta,
            IDVendedor = cart.IDVendedor,
            IDDeposito = cart.IDDeposito,
            IDCotizacionComercial = cart.IDCotizacionComercial,
            IDCondicionVenta = cart.IDCondicionVenta,
            IDUsuario = 1,
            FechaPedido = cart.FechaPedido,
            FechaEmision = cart.FechaPedido,
            FechaPrevistaEntrega = cart.FechaPrevistaEntrega,
            MontoCambio = cart.MontoCambio <= 0 ? 1 : cart.MontoCambio,
            Plazo = cart.Plazo,
            // Paridad ERP ValoresTipoFactura: Delivery→REPARTO(0), Pickup→SALON(1)
            TipoFactura = cart.TipoEntrega == TipoEntregaEcommerce.Delivery
                ? TipoFacturaErp.Reparto
                : TipoFacturaErp.Salon,
            TipoPedido = TipoPedidoErp.Venta,
            TipoPrecio = 0,
            Estado = EstadoPedidoMovil.Cerrado,
            Migrado = false,
            Momento = now,
            Observacion = cart.Observacion,
            Latitud = cart.LatitudEntrega,
            Longitud = cart.LongitudEntrega,
            Precision = 10,
            DireccionEntrega = cart.LugarEntrega,
            Host = request.Email
        };

        foreach (var detalle in lines)
        {
            var emb = await _db.ArticuloEmbalajes.AsNoTracking()
                .Where(e => e.IDEmpresa == cart.IDEmpresa
                            && e.IDArticulo == detalle.IDArticulo
                            && e.Activo)
                .OrderByDescending(e => e.EmbalajeParaFacturacion)
                .ThenBy(e => e.ID)
                .FirstOrDefaultAsync(ct);

            var codigoBarra = emb?.CodigoBarras;
            if (string.IsNullOrWhiteSpace(codigoBarra) && emb != null)
            {
                codigoBarra = await _db.ArticuloEmbalajeCodigosBarra.AsNoTracking()
                    .Where(c => c.IDEmpresa == cart.IDEmpresa && c.IDArticuloEmbalaje == emb.ID && c.Activo)
                    .Select(c => c.CodigoBarra)
                    .FirstOrDefaultAsync(ct);
            }

            var dlpId = await ResolveDetalleListaPrecioIdAsync(
                cart.IDEmpresa, cart.IDListaPrecio, detalle.IDArticulo, emb?.ID, ct);

            pedido.DetallePedidoMoviles.Add(new DetallePedidoMovil
            {
                Activo = true,
                UltModificacion = now,
                UserName = "ecom-checkout",
                IDArticulo = detalle.IDArticulo,
                IDDetalleListaPrecio = dlpId,
                IDUsuario = 1,
                IDUsuarioTraspaso = 1,
                CodigoBarra = Truncate(codigoBarra, 20),
                MontoCambio = (double)(cart.MontoCambio <= 0 ? 1 : cart.MontoCambio),
                PorcentajeDescuento = (double)detalle.PorcentajeDescuento,
                PrecioBrutoConImpuesto = (double)detalle.PrecioBrutoConImpuesto,
                PrecioBrutoSinImpuesto = (double)detalle.PrecioBrutoSinImpuesto,
                PrecioNetoConImpuesto = (double)detalle.PrecioNetoConImpuesto,
                PrecioNetoSinImpuesto = (double)detalle.PrecioNetoSinImpuesto,
                CantidadEmbalada = detalle.CantidadEmbalada,
                CantidadFraccionada = detalle.CantidadFraccionada,
                Traspaso = now,
                Momento = now,
                PrecioLista = detalle.PrecioNetoConImpuesto
            });
        }

        _db.PedidosMovil.Add(pedido);
        await _db.SaveChangesAsync(ct);

        // Tracking inicial (paridad ERP: "Compra realizada" / preparando).
        _db.SeguimientosPedidoEcommerce.Add(new SeguimientoPedidoEcommerce
        {
            IDEmpresa = cart.IDEmpresa,
            IDPedidoMovil = pedido.ID,
            IDCarritoCompra = cart.ID,
            Estado = "compra_realizada",
            Mensaje = cart.TipoEntrega == TipoEntregaEcommerce.Delivery
                ? $"Pedido recibido · delivery · {cart.LugarEntrega ?? "sin dirección"}"
                : "Pedido recibido · retiro en sucursal",
            CodigoTracking = $"ECO-{pedido.NroPedido}",
            FechaEstado = now,
            Activo = true,
            UltModificacion = now,
            UserName = "ecom-checkout"
        });
        _db.SeguimientosPedidoEcommerce.Add(new SeguimientoPedidoEcommerce
        {
            IDEmpresa = cart.IDEmpresa,
            IDPedidoMovil = pedido.ID,
            IDCarritoCompra = cart.ID,
            Estado = "preparando",
            Mensaje = "En preparación",
            CodigoTracking = $"ECO-{pedido.NroPedido}",
            FechaEstado = now.AddSeconds(1),
            Activo = true,
            UltModificacion = now,
            UserName = "ecom-checkout"
        });
        await _db.SaveChangesAsync(ct);
        return pedido;
    }

    private static string? Truncate(string? value, int max)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        var v = value.Trim();
        return v.Length <= max ? v : v[..max];
    }

    private bool IsBancardConfigured(EmpresaConfiguracionEcommerce config)
    {
        // UAT pedidos: Bancard desactivado hasta implementar Live.
        if (_bancardOptions.IsOff) return false;
        var hasKeys = !string.IsNullOrWhiteSpace(config.ClavePublicaProcesadoraTarjeta)
                      && !string.IsNullOrWhiteSpace(config.ClavePrivadaProcesadoraTarjeta);
        if (_bancardOptions.IsLive) return hasKeys;
        // Stub: permitir flujo online sin claves reales (CreateMultiBuy usa demo-* si faltan).
        if (_bancardOptions.IsStub) return true;
        return hasKeys || config.CobraAlHacerPedido;
    }

    private async Task<long> NextProcesoNegocioAsync(
        EmpresaConfiguracionEcommerce config, CancellationToken ct)
    {
        config.UltIDProcesoNegocio += 1;
        await _db.SaveChangesAsync(ct);
        return config.UltIDProcesoNegocio;
    }

    private static void ValidateTipoComprobante(
        EmpresaConfiguracionEcommerce config, CheckoutRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.TipoComprobante))
            return;

        var tipo = request.TipoComprobante.Trim().ToLowerInvariant();
        if (tipo is not ("factura" or "ticket"))
            throw new ArgumentException("TipoComprobante inválido (use factura o ticket).");

        if (request.Delivery)
        {
            if (tipo == "factura" && !config.DeliveryEmiteFactura)
                throw new InvalidOperationException("Factura no disponible para delivery.");
            if (tipo == "ticket" && !config.DeliveryEmiteTicket)
                throw new InvalidOperationException("Ticket no disponible para delivery.");
        }
        else
        {
            if (tipo == "factura" && !config.PickupEmiteFactura)
                throw new InvalidOperationException("Factura no disponible para retiro.");
            if (tipo == "ticket" && !config.PickupEmiteTicket)
                throw new InvalidOperationException("Ticket no disponible para retiro.");
        }
    }

    private static string BuildObservacion(CheckoutRequest r)
    {
        var baseObs =
            $"Cliente: {r.Nombre.Trim()} {r.Apellido.Trim()} | Tel: {r.NroTelefono.Trim()} | Email: {r.Email.Trim()}"
            + (string.IsNullOrWhiteSpace(r.LugarEntrega) ? "" : $" | Entrega: {r.LugarEntrega.Trim()}")
            + (string.IsNullOrWhiteSpace(r.Observacion) ? "" : $" | Obs: {r.Observacion.Trim()}");

        if (string.IsNullOrWhiteSpace(r.TipoComprobante))
            return baseObs;

        var label = r.TipoComprobante.Trim().Equals("ticket", StringComparison.OrdinalIgnoreCase)
            ? "Ticket"
            : "Factura";
        return $"{baseObs} | Comprobante: {label}";
    }

    private async Task<CheckoutResultDto> MapResultAsync(
        CarritoCompra cart,
        PedidoMovil? pedido,
        bool requierePago,
        string mensaje,
        CancellationToken ct,
        string? processId = null)
    {
        var lines = cart.DetalleCarritoCompras?.Where(d => d.Activo).ToList()
                    ?? await _db.DetallesCarritoCompra.AsNoTracking()
                        .Where(d => d.IDCarritoCompra == cart.ID && d.Activo)
                        .ToListAsync(ct);

        var artIds = lines.Select(l => l.IDArticulo).Distinct().ToList();
        var arts = await _db.Articulos.AsNoTracking()
            .Where(a => artIds.Contains(a.ID))
            .Select(a => new { a.ID, a.SKU, a.DescripcionArticulo })
            .ToListAsync(ct);
        var map = arts.ToDictionary(a => a.ID);

        var dtoLines = lines.Select(l =>
        {
            map.TryGetValue(l.IDArticulo, out var a);
            return new CheckoutLineDto(
                l.IDArticulo,
                a?.SKU,
                a?.DescripcionArticulo ?? $"Artículo {l.IDArticulo}",
                l.Cantidad,
                l.PrecioNetoConImpuesto,
                l.MontoTotal);
        }).ToList();

        if (processId is null && requierePago && cart.IDProcesoNegocio is long sp)
        {
            processId = await _db.LogsPagoTarjeta.AsNoTracking()
                .Where(l => l.IDCarritoCompra == cart.ID && l.ShopProcessId == sp)
                .OrderByDescending(l => l.ID)
                .Select(l => l.ProcessId)
                .FirstOrDefaultAsync(ct);
        }

        var nroFactura = pedido?.NroFactura;
        if (nroFactura is null && cart.IDPedidoMovil is long idPm)
        {
            nroFactura = await _db.PedidosMovil.AsNoTracking()
                .Where(p => p.ID == idPm)
                .Select(p => p.NroFactura)
                .FirstOrDefaultAsync(ct);
        }

        var facturado = nroFactura is > 0;
        var puedePdf = facturado
                       && _pentaho.Enabled
                       && !string.IsNullOrWhiteSpace(_pentaho.UrlReportPentaho);

        return new CheckoutResultDto(
            cart.ID,
            cart.NroPedidoCarritoCompra,
            pedido?.ID ?? cart.IDPedidoMovil,
            cart.Estado,
            cart.TipoEntrega,
            cart.TipoPago,
            cart.MontoTotal,
            cart.FechaPrevistaEntrega,
            cart.RazonSocial,
            cart.LugarEntrega,
            requierePago,
            mensaje,
            dtoLines,
            processId,
            _bancard.Mode,
            nroFactura,
            facturado,
            puedePdf);
    }

    private async Task RecordCouponUsageAsync(CarritoCompra cart, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(cart.CodigoCuponAplicado) || cart.MontoDescuento <= 0)
            return;

        var code = cart.CodigoCuponAplicado.Trim().ToUpperInvariant();
        var cupon = await _db.CuponesEcommerce
            .FirstOrDefaultAsync(c => c.IDEmpresa == cart.IDEmpresa && c.Codigo == code && c.Activo, ct);
        if (cupon is null)
            return;

        _db.UsosCuponEcommerce.Add(new UsoCuponEcommerce
        {
            IDEmpresa = cart.IDEmpresa,
            IDCuponEcommerce = cupon.ID,
            IDUsuarioEcommerce = _tenant.IDUsuarioEcommerce,
            IDCarritoCompra = cart.ID,
            Email = null,
            MontoDescontado = cart.MontoDescuento,
            FechaUso = DateTime.UtcNow,
            Activo = true,
            UltModificacion = DateTime.UtcNow,
            UserName = "ecom-checkout"
        });
        cupon.UsosActuales += 1;
        cupon.UltModificacion = DateTime.UtcNow;
    }

    /// <summary>
    /// Paridad ERP: DetallePedidoMovil.IDDetalleListaPrecio → dbo.detallelistaprecio (NO condicionventa).
    /// </summary>
    private async Task<long> ResolveDetalleListaPrecioIdAsync(
        long idEmpresa, long? idListaPrecio, long idArticulo, long? idEmbalaje, CancellationToken ct)
    {
        if (idListaPrecio is > 0)
        {
            if (idEmbalaje is > 0)
            {
                var byEmb = await _db.DetallesListaPrecio.AsNoTracking()
                    .Where(d => d.IDEmpresa == idEmpresa
                                && d.IDListaPrecio == idListaPrecio
                                && d.IDArticuloEmbalaje == idEmbalaje
                                && d.Activo)
                    .Select(d => d.ID)
                    .FirstOrDefaultAsync(ct);
                if (byEmb > 0) return byEmb;
            }

            var byArt = await _db.DetallesListaPrecio.AsNoTracking()
                .Where(d => d.IDEmpresa == idEmpresa
                            && d.IDListaPrecio == idListaPrecio
                            && d.Activo
                            && _db.ArticuloEmbalajes.Any(e =>
                                e.ID == d.IDArticuloEmbalaje && e.IDArticulo == idArticulo))
                .Select(d => d.ID)
                .FirstOrDefaultAsync(ct);
            if (byArt > 0) return byArt;
        }

        // Fallback ERP: a veces usan 1; preferimos cualquier DLP del artículo en lista ecom.
        var any = await _db.DetallesListaPrecio.AsNoTracking()
            .Where(d => d.IDEmpresa == idEmpresa
                        && d.Activo
                        && _db.ArticuloEmbalajes.Any(e =>
                            e.ID == d.IDArticuloEmbalaje && e.IDArticulo == idArticulo)
                        && _db.ListasPrecio.Any(l =>
                            l.ID == d.IDListaPrecio && l.EsListaPrecioEcommerce && l.Activo))
            .Select(d => d.ID)
            .FirstOrDefaultAsync(ct);

        return any > 0 ? any : 1;
    }

    private long RequireTenant()
    {
        if (!_tenant.IsResolved || !_tenant.IDEmpresa.HasValue)
            throw new InvalidOperationException("Tenant no resuelto.");
        return _tenant.IDEmpresa.Value;
    }
}
