using Datamex.ECommerce.Application.Abstractions;
using Datamex.ECommerce.Application.Checkout;
using Datamex.ECommerce.Application.Integration;
using Datamex.ECommerce.Application.Inventory;
using Datamex.ECommerce.Application.Notifications;
using Datamex.ECommerce.Application.Options;
using Datamex.ECommerce.Application.Payments;
using Datamex.ECommerce.Application.Sync;
using Datamex.ECommerce.Domain.Entities;
using Datamex.ECommerce.Infrastructure.Persistence;
using Datamex.ECommerce.Infrastructure.Pricing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Datamex.ECommerce.Infrastructure.Payments;

/// <summary>
/// Confirmación / rollback Bancard alineado a CarritoCompraService.Checkout del ERP.
/// </summary>
public sealed class PaymentService : IPaymentService
{
    private readonly EcomDbContext _db;
    private readonly ITenantContext _tenant;
    private readonly IBancardClient _bancard;
    private readonly IEmailSender _email;
    private readonly IInventoryService _inventory;
    private readonly ECommerceOptions _options;
    private readonly BancardOptions _bancardOptions;
    private readonly IPedidoErpWriteBackDispatcher _writeBack;
    private readonly PentahoOptions _pentaho;
    private readonly ILogger<PaymentService> _logger;

    public PaymentService(
        EcomDbContext db,
        ITenantContext tenant,
        IBancardClient bancard,
        IEmailSender email,
        IInventoryService inventory,
        IOptions<ECommerceOptions> options,
        IOptions<BancardOptions> bancardOptions,
        IPedidoErpWriteBackDispatcher writeBack,
        IOptions<PentahoOptions> pentaho,
        ILogger<PaymentService> logger)
    {
        _db = db;
        _tenant = tenant;
        _bancard = bancard;
        _email = email;
        _inventory = inventory;
        _options = options.Value;
        _bancardOptions = bancardOptions.Value;
        _writeBack = writeBack;
        _pentaho = pentaho.Value;
        _logger = logger;
    }

    public async Task<PaymentSessionDto?> GetSessionAsync(long nroPedido, string sessionId, CancellationToken ct = default)
    {
        var idEmpresa = RequireTenant();
        var cart = await _db.CarritosCompra.AsNoTracking()
            .FirstOrDefaultAsync(c =>
                c.IDEmpresa == idEmpresa
                && c.NroPedidoCarritoCompra == nroPedido
                && (c.IDSesion == sessionId || c.Estado == EstadoCarrito.Cerrado), ct);
        if (cart is null) return null;

        var log = cart.IDProcesoNegocio is long sp
            ? await _db.LogsPagoTarjeta.AsNoTracking()
                .Where(l => l.IDEmpresa == idEmpresa && l.ShopProcessId == sp)
                .OrderByDescending(l => l.ID)
                .FirstOrDefaultAsync(ct)
            : null;

        var completed = cart.Estado == EstadoCarrito.Cerrado;
        return new PaymentSessionDto(
            cart.ID,
            cart.NroPedidoCarritoCompra,
            cart.IDProcesoNegocio ?? 0,
            log?.ProcessId,
            cart.MontoTotal,
            "PYG",
            cart.TipoPago == TipoPagoEcommerce.Pedido && cart.Estado == EstadoCarrito.Abierto,
            completed,
            _bancard.Mode,
            _bancard.CheckoutScriptUrl,
            completed
                ? $"Pago confirmado. Pedido N° {cart.NroPedidoCarritoCompra}."
                : cart.Estado == EstadoCarrito.Anulado
                    ? "Pago cancelado / anulado."
                    : "Pendiente de pago online (Bancard).");
    }

    public async Task<CheckoutResultDto> ConfirmWebhookAsync(BancardWebhookPayload payload, CancellationToken ct = default)
    {
        var idEmpresa = RequireTenant();

        // Transacción solo en providers relacionales (SQLite/Postgres); InMemory no la soporta.
        await using var tx = _db.Database.IsRelational()
            ? await _db.Database.BeginTransactionAsync(ct)
            : null;

        var cart = await _db.CarritosCompra
            .Include(c => c.DetalleCarritoCompras)
            .FirstOrDefaultAsync(c =>
                c.IDEmpresa == idEmpresa
                && c.IDProcesoNegocio == payload.ShopProcessId
                && c.Activo, ct)
            ?? throw new InvalidOperationException("Carrito no encontrado para shop_process_id.");

        // Idempotencia: webhook duplicado tras éxito.
        if (cart.Estado == EstadoCarrito.Cerrado
            || await HasSuccessfulPaymentAsync(idEmpresa, payload.ShopProcessId, ct))
        {
            if (tx is not null) await tx.CommitAsync(ct);
            return await MapCheckoutAsync(cart, "Pago ya confirmado.", ct);
        }

        if (cart.Estado == EstadoCarrito.Anulado)
            throw new InvalidOperationException("El carrito está anulado.");

        var okItems = payload.Items.Count == 0
            || payload.Items.Any(i => i.ResponseCode == "00" || string.IsNullOrEmpty(i.ResponseCode));
        if (!okItems)
        {
            await AppendPaymentLogAsync(
                cart, payload, "rejected",
                payload.Items.FirstOrDefault()?.AuthorizationCode, ct);
            await _db.SaveChangesAsync(ct);
            if (tx is not null) await tx.CommitAsync(ct);
            _logger.LogWarning(
                "Bancard rechazó pago shop_process_id={Sp} carrito={Id}",
                payload.ShopProcessId, cart.ID);
            throw new InvalidOperationException("Bancard rechazó el pago (response_code != 00).");
        }

        if (!string.IsNullOrWhiteSpace(payload.AmountInGs)
            && decimal.TryParse(payload.AmountInGs.Replace(",", "."),
                System.Globalization.NumberStyles.Any,
                System.Globalization.CultureInfo.InvariantCulture, out var paid)
            && Math.Abs(paid - cart.MontoTotal) > 0.5m)
        {
            await AppendPaymentLogAsync(cart, payload, "amount_mismatch", null, ct);
            await _db.SaveChangesAsync(ct);
            if (tx is not null) await tx.CommitAsync(ct);
            throw new InvalidOperationException(
                $"Monto Bancard ({paid}) no coincide con el carrito ({cart.MontoTotal}).");
        }

        var result = await FinalizePaidCartAsync(cart, payload, ct);
        if (tx is not null) await tx.CommitAsync(ct);
        return result;
    }

    public async Task<CheckoutResultDto> SimulateSuccessAsync(
        long shopProcessId, string sessionId, CancellationToken ct = default)
    {
        if (!_bancardOptions.IsStub)
            throw new InvalidOperationException("SimulateSuccess solo disponible en Bancard Mode=Stub.");

        var idEmpresa = RequireTenant();
        var cart = await _db.CarritosCompra
            .Include(c => c.DetalleCarritoCompras)
            .FirstOrDefaultAsync(c =>
                c.IDEmpresa == idEmpresa
                && c.IDProcesoNegocio == shopProcessId
                && c.IDSesion == sessionId
                && c.Activo, ct)
            ?? throw new InvalidOperationException("Carrito no encontrado para simulación.");

        if (cart.Estado == EstadoCarrito.Cerrado
            || await HasSuccessfulPaymentAsync(idEmpresa, shopProcessId, ct))
            return await MapCheckoutAsync(cart, "Pago ya confirmado.", ct);

        var payload = new BancardWebhookPayload(
            shopProcessId,
            BancardCrypto.FormatAmount(cart.MontoTotal),
            BancardCrypto.FormatAmount(0),
            "PYG",
            [new BancardConfirmItem("00", "STUB-AUTH", "STUB-00", BancardCrypto.FormatAmount(cart.MontoTotal), "PYG")],
            "stub-simulate");

        return await FinalizePaidCartAsync(cart, payload, ct);
    }

    public async Task RollbackAsync(long shopProcessId, CancellationToken ct = default)
    {
        var idEmpresa = RequireTenant();
        var config = await _db.EmpresaConfiguracionesEcommerce.FirstAsync(c => c.ID == idEmpresa, ct);
        var cart = await _db.CarritosCompra
            .Include(c => c.DetalleCarritoCompras)
            .FirstOrDefaultAsync(c => c.IDEmpresa == idEmpresa && c.IDProcesoNegocio == shopProcessId, ct)
            ?? throw new InvalidOperationException("Carrito no encontrado.");

        if (cart.Estado == EstadoCarrito.Anulado)
        {
            _logger.LogInformation(
                "Rollback idempotente shop_process_id={Sp} carrito={Id} ya anulado",
                shopProcessId, cart.ID);
            return;
        }

        if (cart.Migrado)
            throw new InvalidOperationException("No se puede anular un carrito ya migrado.");

        // Stock solo se aplica al confirmar (Cerrado). Restaurar solo si hubo baja.
        var stockFueAplicado = cart.Estado == EstadoCarrito.Cerrado
            || await HasSuccessfulPaymentAsync(idEmpresa, shopProcessId, ct);

        if (!string.IsNullOrWhiteSpace(config.ClavePublicaProcesadoraTarjeta)
            && !string.IsNullOrWhiteSpace(config.ClavePrivadaProcesadoraTarjeta))
        {
            var resp = await _bancard.RollbackAsync(
                config.ClavePublicaProcesadoraTarjeta,
                config.ClavePrivadaProcesadoraTarjeta,
                shopProcessId, ct);
            if (!string.Equals(resp.Status, "success", StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException(resp.Description ?? "Rollback Bancard falló.");
        }

        var now = DateTime.UtcNow;
        cart.Estado = EstadoCarrito.Anulado;
        cart.UltModificacion = now;
        cart.UserName = "bancard-rollback";

        if (cart.IDPedidoMovil is long idPedido)
        {
            var pedido = await _db.PedidosMovil.FirstOrDefaultAsync(p => p.ID == idPedido, ct);
            if (pedido is not null)
            {
                if (pedido.Migrado)
                    throw new InvalidOperationException("Pedido móvil ya migrado; no se anula.");
                pedido.Estado = EstadoPedidoMovil.Anulado;
                pedido.Activo = false;
                pedido.UltModificacion = now;
            }
        }

        _db.LogsPagoTarjeta.Add(new LogPagoTarjeta
        {
            IDEmpresa = idEmpresa,
            Activo = true,
            UltModificacion = now,
            UserName = "bancard-rollback",
            IDCarritoCompra = cart.ID,
            ShopProcessId = shopProcessId,
            Monto = cart.MontoTotal,
            Status = "rollback",
            Fecha = now
        });

        if (stockFueAplicado
            && TenantIntegrationMode.MutatesLocalStock(config, _options.IntegrationMode))
        {
            var lines = cart.DetalleCarritoCompras
                .Where(d => d.Activo)
                .Select(d => new StockLineMove(d.IDArticulo, d.Cantidad))
                .ToList();
            await _inventory.RestorePedidoStockAsync(
                cart.IDDeposito, lines, config.IDArticuloDelivery, ct);
        }

        await _db.SaveChangesAsync(ct);
        _logger.LogInformation(
            "Rollback carrito {Id} shop_process_id={Sp} stockRestaurado={Restore}",
            cart.ID, shopProcessId, stockFueAplicado);
    }

    private async Task<bool> HasSuccessfulPaymentAsync(long idEmpresa, long shopProcessId, CancellationToken ct)
    {
        return await _db.LogsPagoTarjeta.AsNoTracking().AnyAsync(
            l => l.IDEmpresa == idEmpresa
                 && l.ShopProcessId == shopProcessId
                 && l.Status == "success"
                 && l.Activo, ct);
    }

    private async Task AppendPaymentLogAsync(
        CarritoCompra cart,
        BancardWebhookPayload payload,
        string status,
        string? auth,
        CancellationToken ct)
    {
        var now = DateTime.UtcNow;
        var pending = await _db.LogsPagoTarjeta
            .Where(l => l.IDEmpresa == cart.IDEmpresa
                        && l.ShopProcessId == payload.ShopProcessId
                        && l.IDCarritoCompra == cart.ID)
            .OrderByDescending(l => l.ID)
            .FirstOrDefaultAsync(ct);

        _db.LogsPagoTarjeta.Add(new LogPagoTarjeta
        {
            IDEmpresa = cart.IDEmpresa,
            Activo = true,
            UltModificacion = now,
            UserName = "bancard-checkout",
            IDCarritoCompra = cart.ID,
            ShopProcessId = payload.ShopProcessId,
            ProcessId = pending?.ProcessId ?? $"bancard-{payload.ShopProcessId}",
            Monto = cart.MontoTotal,
            Status = status,
            AuthorizationCode = auth,
            RawBody = payload.RawBody,
            Fecha = now
        });
    }

    private async Task<CheckoutResultDto> FinalizePaidCartAsync(
        CarritoCompra cart, BancardWebhookPayload payload, CancellationToken ct)
    {
        var idEmpresa = cart.IDEmpresa;
        var config = await _db.EmpresaConfiguracionesEcommerce.FirstAsync(c => c.ID == idEmpresa, ct);
        var lines = cart.DetalleCarritoCompras.Where(d => d.Activo).ToList();
        var now = DateTime.UtcNow;

        cart.Estado = EstadoCarrito.Cerrado;
        cart.UltModificacion = now;
        cart.UserName = "bancard-checkout";

        var auth = payload.Items.FirstOrDefault()?.AuthorizationCode
                   ?? payload.Items.FirstOrDefault()?.AuthorizationId;

        await AppendPaymentLogAsync(cart, payload, "success", auth, ct);

        PedidoMovil? pedido = null;
        if (TenantIntegrationMode.CreatesLocalPedidoMovil(config, _options.IntegrationMode))
        {
            if (cart.IDPedidoMovil.HasValue)
            {
                pedido = await _db.PedidosMovil.FirstOrDefaultAsync(p => p.ID == cart.IDPedidoMovil, ct);
            }
            else
            {
                pedido = await CreatePedidoMovilAsync(cart, lines, ct);
                cart.IDPedidoMovil = pedido.ID;
            }
        }

        // Stock una sola vez: guardado por Estado/log success antes de llegar aquí en reintentos.
        if (TenantIntegrationMode.MutatesLocalStock(config, _options.IntegrationMode))
        {
            await _inventory.ApplyPedidoStockAsync(
                cart.IDDeposito,
                lines.Select(l => new StockLineMove(l.IDArticulo, l.Cantidad)).ToList(),
                config.IDArticuloDelivery,
                ct);
        }

        await _db.SaveChangesAsync(ct);

        if (pedido is not null && config.MigrarPedidosAlGenerar)
        {
            try
            {
                var wb = await _writeBack.FlushPedidoAsync(idEmpresa, pedido.ID, ct);
                if (wb is null || wb.Errores > 0)
                {
                    _logger.LogWarning(
                        "Write-back sync incompleto post-pago pedido={Id} ok={Ok} err={Err}",
                        pedido.ID, wb?.Exitosos, wb?.Errores);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Write-back sync post-pago falló pedido={Id}", pedido.ID);
                _writeBack.SchedulePedido(idEmpresa, pedido.ID);
            }
        }

        try
        {
            var email = ExtractEmail(cart.Observacion) ?? "cliente@datamex.local";
            await _email.SendOrderConfirmationAsync(new OrderConfirmationEmail(
                email, cart.RazonSocial, cart.NroPedidoCarritoCompra, cart.MontoTotal,
                cart.LugarEntrega,
                lines.Select(l => ($"Artículo {l.IDArticulo}", l.Cantidad, l.MontoTotal)).ToList()), ct);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Email post-pago falló para carrito {Id}", cart.ID);
        }

        return await MapCheckoutAsync(cart, $"Pago confirmado. Pedido N° {cart.NroPedidoCarritoCompra}.", ct);
    }

    private async Task<PedidoMovil> CreatePedidoMovilAsync(
        CarritoCompra cart, List<DetalleCarritoCompra> lines, CancellationToken ct)
    {
        var now = DateTime.UtcNow;
        var idCuenta = cart.IDCuenta
            ?? throw new InvalidOperationException(
                "El carrito no tiene cuenta vinculada. Iniciá sesión con una cuenta activada.");
        var idListaPrecioCuenta = await ListaPrecioCuentaHelper.ResolveOrEnsureAssignmentIdAsync(
            _db, cart.IDEmpresa, idCuenta, cart.IDListaPrecio, "bancard-checkout", ct);

        var pedido = new PedidoMovil
        {
            IDEmpresa = cart.IDEmpresa,
            Activo = true,
            UltModificacion = now,
            UserName = "bancard-checkout",
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
            Host = "bancard"
        };

        foreach (var detalle in lines)
        {
            var emb = await _db.ArticuloEmbalajes.AsNoTracking()
                .Where(e => e.IDEmpresa == cart.IDEmpresa
                            && e.IDArticulo == detalle.IDArticulo
                            && e.Activo)
                .OrderByDescending(e => e.EmbalajeParaFacturacion)
                .ThenBy(e => e.ID)
                .Select(e => e.ID)
                .FirstOrDefaultAsync(ct);

            var dlpId = await ResolveDetalleListaPrecioIdAsync(
                cart.IDEmpresa, cart.IDListaPrecio, detalle.IDArticulo, emb > 0 ? emb : null, ct);

            pedido.DetallePedidoMoviles.Add(new DetallePedidoMovil
            {
                Activo = true,
                UltModificacion = now,
                UserName = "bancard-checkout",
                IDArticulo = detalle.IDArticulo,
                IDDetalleListaPrecio = dlpId,
                IDUsuario = 1,
                IDUsuarioTraspaso = 1,
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

        _db.SeguimientosPedidoEcommerce.Add(new SeguimientoPedidoEcommerce
        {
            IDEmpresa = cart.IDEmpresa,
            IDPedidoMovil = pedido.ID,
            IDCarritoCompra = cart.ID,
            Estado = "compra_realizada",
            Mensaje = cart.TipoEntrega == TipoEntregaEcommerce.Delivery
                ? $"Pedido pagado · delivery · {cart.LugarEntrega ?? "sin dirección"}"
                : "Pedido pagado · retiro en sucursal",
            CodigoTracking = $"ECO-{pedido.NroPedido}",
            FechaEstado = now,
            Activo = true,
            UltModificacion = now,
            UserName = "bancard-checkout"
        });
        _db.SeguimientosPedidoEcommerce.Add(new SeguimientoPedidoEcommerce
        {
            IDEmpresa = cart.IDEmpresa,
            IDPedidoMovil = pedido.ID,
            IDCarritoCompra = cart.ID,
            Estado = "preparando",
            Mensaje = "En preparación",
            FechaEstado = now.AddSeconds(1),
            Activo = true,
            UltModificacion = now,
            UserName = "bancard-checkout"
        });
        await _db.SaveChangesAsync(ct);
        return pedido;
    }

    private async Task<CheckoutResultDto> MapCheckoutAsync(CarritoCompra cart, string mensaje, CancellationToken ct)
    {
        var lines = cart.DetalleCarritoCompras?.Where(d => d.Activo).ToList()
                    ?? await _db.DetallesCarritoCompra.AsNoTracking()
                        .Where(d => d.IDCarritoCompra == cart.ID && d.Activo).ToListAsync(ct);

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
                l.IDArticulo, a?.SKU, a?.DescripcionArticulo ?? $"Artículo {l.IDArticulo}",
                l.Cantidad, l.PrecioNetoConImpuesto, l.MontoTotal);
        }).ToList();

        var nroFactura = cart.IDPedidoMovil is long idPm
            ? await _db.PedidosMovil.AsNoTracking()
                .Where(p => p.ID == idPm)
                .Select(p => p.NroFactura)
                .FirstOrDefaultAsync(ct)
            : null;
        var facturado = nroFactura is > 0;
        var puedePdf = facturado
                       && _pentaho.Enabled
                       && !string.IsNullOrWhiteSpace(_pentaho.UrlReportPentaho);

        return new CheckoutResultDto(
            cart.ID, cart.NroPedidoCarritoCompra, cart.IDPedidoMovil,
            cart.Estado, cart.TipoEntrega, cart.TipoPago, cart.MontoTotal,
            cart.FechaPrevistaEntrega, cart.RazonSocial, cart.LugarEntrega,
            cart.Estado == EstadoCarrito.Abierto && cart.TipoPago == TipoPagoEcommerce.Pedido,
            mensaje, dtoLines,
            cart.IDProcesoNegocio is long sp ? $"stub-{sp}" : null,
            _bancard.Mode,
            nroFactura,
            facturado,
            puedePdf);
    }

    private static string? ExtractEmail(string? observacion)
    {
        if (string.IsNullOrWhiteSpace(observacion)) return null;
        const string marker = "Email: ";
        var i = observacion.IndexOf(marker, StringComparison.OrdinalIgnoreCase);
        if (i < 0) return null;
        var rest = observacion[(i + marker.Length)..];
        var end = rest.IndexOf(" | ", StringComparison.Ordinal);
        return (end >= 0 ? rest[..end] : rest).Trim();
    }

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
