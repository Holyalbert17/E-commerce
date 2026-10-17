using System.Net;
using System.Text;
using Datamex.ECommerce.Application.Abstractions;
using Datamex.ECommerce.Application.Invoices;
using Datamex.ECommerce.Application.Options;
using Datamex.ECommerce.Application.Sync;
using Datamex.ECommerce.Domain.Entities;
using Datamex.ECommerce.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Npgsql;

namespace Datamex.ECommerce.Infrastructure.Invoices;

/// <summary>
/// Descarga de facturas vía Pentaho — mismo contrato que ERP Site/PortalDE:
/// comercial[:sub]:{reporte}.prpt/generatedContent?idpedido=&amp;idempresa=&amp;output-target=pageable/pdf
/// </summary>
public sealed class PentahoInvoiceService : IInvoiceService
{
    private readonly EcomDbContext _db;
    private readonly ITenantContext _tenant;
    private readonly IConfiguration _config;
    private readonly PentahoOptions _pentaho;
    private readonly ILogger<PentahoInvoiceService> _logger;
    private readonly IErpConnectionResolver _erpCs;

    public PentahoInvoiceService(
        EcomDbContext db,
        ITenantContext tenant,
        IConfiguration config,
        IOptions<PentahoOptions> pentaho,
        ILogger<PentahoInvoiceService> logger,
        IErpConnectionResolver erpCs)
    {
        _db = db;
        _tenant = tenant;
        _config = config;
        _pentaho = pentaho.Value;
        _logger = logger;
        _erpCs = erpCs;
    }

    public async Task<IReadOnlyList<AccountInvoiceDto>> ListAsync(
        long idUsuarioEcommerce, CancellationToken ct = default)
    {
        var idEmpresa = RequireTenant();
        var cuentaIds = await GetCuentaIdsAsync(idUsuarioEcommerce, idEmpresa, ct);
        if (cuentaIds.Count == 0)
            return Array.Empty<AccountInvoiceDto>();

        var carts = await _db.CarritosCompra.AsNoTracking()
            .Where(c => c.IDEmpresa == idEmpresa
                        && c.Estado != EstadoCarrito.Abierto
                        && c.IDCuenta != null
                        && cuentaIds.Contains(c.IDCuenta.Value)
                        && c.IDPedidoMovil != null)
            .OrderByDescending(c => c.FechaPedido)
            .Take(80)
            .ToListAsync(ct);

        var pmIds = carts.Select(c => c.IDPedidoMovil!.Value).Distinct().ToList();
        var pedidos = await _db.PedidosMovil.AsNoTracking()
            .Where(p => pmIds.Contains(p.ID))
            .ToDictionaryAsync(p => p.ID, ct);

        var pentahoOk = _pentaho.Enabled && !string.IsNullOrWhiteSpace(_pentaho.UrlReportPentaho);
        var list = new List<AccountInvoiceDto>();
        foreach (var c in carts)
        {
            var pm = pedidos.GetValueOrDefault(c.IDPedidoMovil!.Value);
            var nroFactura = pm?.NroFactura;
            if (!FacturaSerie.EsFacturado(nroFactura)) continue;

            list.Add(new AccountInvoiceDto(
                c.NroPedidoCarritoCompra,
                c.IDPedidoMovil,
                nroFactura,
                null,
                c.FechaPedido,
                c.MontoTotal,
                c.RazonSocial,
                true,
                true,
                "pentaho",
                null));
        }

        return list;
    }

    public async Task<InvoicePdfResult> DownloadPdfAsync(
        long idUsuarioEcommerce, long nroPedido, CancellationToken ct = default)
    {
        var idEmpresa = RequireTenant();
        var cuentaIds = await GetCuentaIdsAsync(idUsuarioEcommerce, idEmpresa, ct);
        var cart = await _db.CarritosCompra.AsNoTracking()
            .FirstOrDefaultAsync(c => c.IDEmpresa == idEmpresa
                                      && c.NroPedidoCarritoCompra == nroPedido
                                      && c.IDCuenta != null
                                      && cuentaIds.Contains(c.IDCuenta.Value), ct)
            ?? throw new InvalidOperationException("Pedido no encontrado.");

        if (cart.IDPedidoMovil is not long idPm)
            throw new InvalidOperationException("El pedido aún no tiene PedidoMovil.");

        var localPm = await _db.PedidosMovil.AsNoTracking()
            .FirstOrDefaultAsync(p => p.ID == idPm && p.IDEmpresa == idEmpresa, ct);
        var hasNc = await _db.DevolucionesEcommerce.AsNoTracking()
            .AnyAsync(d => d.IDEmpresa == idEmpresa && d.IDPedidoMovil == idPm && d.IdNotaCreditoErp != null, ct);
        var cancelado = cart.Estado == EstadoCarrito.Anulado
                        || localPm?.Estado == EstadoPedidoMovil.Anulado;
        if (hasNc || cancelado)
            throw new InvalidOperationException(
                "Este pedido fue devuelto. Imprimí la nota de crédito, no la factura.");

        return await DownloadWithFallbackAsync(
            idEmpresa, idPm, cart.NroPedidoCarritoCompra, originalClienteOnly: true, ct);
    }

    public async Task<InvoicePdfResult> DownloadNotaCreditoPdfAsync(
        long idUsuarioEcommerce, long nroPedido, CancellationToken ct = default)
    {
        var idEmpresa = RequireTenant();
        var cuentaIds = await GetCuentaIdsAsync(idUsuarioEcommerce, idEmpresa, ct);
        var cart = await _db.CarritosCompra.AsNoTracking()
            .FirstOrDefaultAsync(c => c.IDEmpresa == idEmpresa
                                      && c.NroPedidoCarritoCompra == nroPedido
                                      && c.IDCuenta != null
                                      && cuentaIds.Contains(c.IDCuenta.Value), ct)
            ?? throw new InvalidOperationException("Pedido no encontrado.");

        if (cart.IDPedidoMovil is not long idPm)
            throw new InvalidOperationException("El pedido aún no tiene PedidoMovil.");

        return await DownloadNotaCreditoCoreAsync(idEmpresa, idPm, cart, ct);
    }

    public async Task<InvoicePdfResult> DownloadPdfByPedidoMovilAsync(
        long idPedidoMovil, CancellationToken ct = default)
    {
        var idEmpresa = RequireTenant();
        var pedido = await _db.PedidosMovil.AsNoTracking()
            .FirstOrDefaultAsync(p => p.ID == idPedidoMovil && p.IDEmpresa == idEmpresa, ct)
            ?? throw new InvalidOperationException("Pedido no encontrado.");

        var nro = pedido.NroPedido > 0
            ? pedido.NroPedido
            : await _db.CarritosCompra.AsNoTracking()
                .Where(c => c.IDEmpresa == idEmpresa && c.IDPedidoMovil == idPedidoMovil)
                .OrderByDescending(c => c.ID)
                .Select(c => c.NroPedidoCarritoCompra)
                .FirstOrDefaultAsync(ct);

        return await DownloadWithFallbackAsync(
            idEmpresa, idPedidoMovil, nro > 0 ? nro : null, originalClienteOnly: false, ct);
    }

    private async Task<InvoicePdfResult> DownloadNotaCreditoCoreAsync(
        long idEmpresa, long idPm, CarritoCompra cart, CancellationToken ct)
    {
        var localPm = await _db.PedidosMovil.AsNoTracking()
            .FirstOrDefaultAsync(p => p.ID == idPm && p.IDEmpresa == idEmpresa, ct)
            ?? throw new InvalidOperationException("Pedido no encontrado.");

        var storedNc = await _db.DevolucionesEcommerce.AsNoTracking()
            .Where(d => d.IDEmpresa == idEmpresa && d.IDPedidoMovil == idPm && d.IdNotaCreditoErp != null)
            .OrderByDescending(d => d.ID)
            .Select(d => d.IdNotaCreditoErp)
            .FirstOrDefaultAsync(ct);

        var nc = await ResolveErpNotaCreditoAsync(idEmpresa, idPm, storedNc, ct)
            ?? throw new InvalidOperationException(
                "No hay nota de crédito en ERP para este pedido. Si la devolución está pendiente, esperá a que se apruebe.");

        var pentahoReady = _pentaho.Enabled && !string.IsNullOrWhiteSpace(_pentaho.UrlReportPentaho);
        if (pentahoReady)
        {
            try
            {
                var reports = ChooseNcReportNames(nc.Electronica);
                Exception? last = null;
                foreach (var report in reports)
                {
                    try
                    {
                        var sub = await ResolveSubCarpetaAsync(idEmpresa, report, ct);
                        var url = BuildPentahoNcUrl(_pentaho.UrlReportPentaho, report, sub, nc.IdNotaCredito, idEmpresa);
                        _logger.LogInformation(
                            "Pentaho NC nroEcom={Nro} idnc={Nc} report={Rep} sub={Sub} url={Url}",
                            cart.NroPedidoCarritoCompra, nc.IdNotaCredito, report, sub, url);
                        var bytes = await FetchPentahoBytesAsync(url, ct);
                        if (!LooksLikePdf(bytes))
                            throw new InvalidOperationException("No se pudo obtener el PDF de la nota de crédito.");

                        // Misma media hoja que la factura: original adquirente, sin duplicado inferior.
                        bytes = InvoicePdfOriginalCopy.KeepAdquirenteCopy(bytes);
                        var nro = nc.NroNotaCredito is > 0 ? nc.NroNotaCredito.Value : nc.IdNotaCredito;
                        return new InvoicePdfResult(
                            bytes,
                            $"nota-credito-{nro}.pdf",
                            "application/pdf",
                            nro,
                            nc.IdPedidoErp ?? 0);
                    }
                    catch (Exception ex) when (ex is InvalidOperationException or TaskCanceledException or TimeoutException or WebException)
                    {
                        last = ex;
                        _logger.LogWarning(ex, "Pentaho NC falló report={Rep} nc={Nc}", report, nc.IdNotaCredito);
                    }
                }

                if (last is not null)
                    _logger.LogWarning(last, "Pentaho NC agotó reportes; comprobante local.");
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Pentaho NC falló pm={Pm}; comprobante local.", idPm);
            }
        }

        return await BuildLocalNotaCreditoPdfAsync(idEmpresa, localPm, cart, nc, ct);
    }

    private async Task<InvoicePdfResult> BuildLocalNotaCreditoPdfAsync(
        long idEmpresa, PedidoMovil pedido, CarritoCompra cart, ErpNotaCreditoInfo nc, CancellationToken ct)
    {
        var detalles = await _db.DetallesPedidoMovil.AsNoTracking()
            .Where(d => d.IDPedidoMovil == pedido.ID && d.Activo)
            .ToListAsync(ct);
        var artIds = detalles.Select(d => d.IDArticulo).Distinct().ToList();
        var arts = await _db.Articulos.AsNoTracking()
            .Where(a => artIds.Contains(a.ID))
            .Select(a => new { a.ID, a.SKU, a.DescripcionArticulo })
            .ToDictionaryAsync(a => a.ID, ct);

        var empresa = await _db.Empresas.AsNoTracking()
            .Where(e => e.ID == idEmpresa)
            .Select(e => e.NombreFantasia ?? e.RazonSocial ?? "Tienda")
            .FirstOrDefaultAsync(ct) ?? "Tienda";

        var lines = detalles.Select(d =>
        {
            arts.TryGetValue(d.IDArticulo, out var a);
            var precio = (decimal)d.PrecioNetoConImpuesto;
            var cant = d.CantidadEmbalada;
            return new LocalComprobantePdf.Line(
                a?.SKU ?? "",
                a?.DescripcionArticulo ?? $"Artículo {d.IDArticulo}",
                cant,
                precio,
                precio * cant);
        }).ToList();

        var nroNc = nc.NroNotaCredito is > 0 ? nc.NroNotaCredito.Value : nc.IdNotaCredito;
        var nroPed = cart.NroPedidoCarritoCompra > 0 ? cart.NroPedidoCarritoCompra : pedido.NroPedido;
        var bytes = LocalComprobantePdf.Build(new LocalComprobantePdf.Model(
            empresa,
            nroPed,
            nroNc,
            pedido.FechaPedido,
            cart.RazonSocial ?? "",
            cart.NumeroDocumento ?? "",
            cart.TipoEntrega == TipoEntregaEcommerce.Pickup ? "Pickup" : "Delivery",
            "Nota de crédito por devolución",
            lines,
            cart.MontoTotal,
            cart.MontoIVA,
            "NOTA DE CREDITO",
            "Nota de credito N"));

        return new InvoicePdfResult(
            bytes,
            $"nota-credito-{nroNc}.pdf",
            "application/pdf",
            nroNc,
            nc.IdPedidoErp ?? 0);
    }

    private sealed record ErpNotaCreditoInfo(
        long IdNotaCredito,
        long? NroNotaCredito,
        long? IdPedidoErp,
        bool Electronica);

    private async Task<ErpNotaCreditoInfo?> ResolveErpNotaCreditoAsync(
        long idEmpresa, long idPedidoMovil, long? idNcKnown, CancellationToken ct)
    {
        var erpCs = await _erpCs.ResolveAsync(idEmpresa, ct)
                    ?? _config.GetConnectionString("Erp");
        if (string.IsNullOrWhiteSpace(erpCs))
            return idNcKnown is > 0 ? new ErpNotaCreditoInfo(idNcKnown.Value, null, null, true) : null;

        var erpPmId = await _db.MigracionesPedidoLog.AsNoTracking()
            .Where(l => l.IDEmpresa == idEmpresa && l.IDPedidoMovil == idPedidoMovil && l.Estado == "ok" && l.IDPedidoMovilErp != null)
            .OrderByDescending(l => l.FechaIntento)
            .Select(l => l.IDPedidoMovilErp)
            .FirstOrDefaultAsync(ct) ?? idPedidoMovil;

        try
        {
            await using var conn = new NpgsqlConnection(erpCs);
            await conn.OpenAsync(ct);
            await using (var path = new NpgsqlCommand("SET search_path TO dbo, public", conn))
                await path.ExecuteNonQueryAsync(ct);

            await using var cmd = new NpgsqlCommand(
                """
                SELECT nc.id, nc.nronotacredito, nc.idpedido,
                       CASE WHEN nce.id IS NOT NULL THEN true ELSE false END AS electronica
                FROM dbo.notacredito nc
                LEFT JOIN dbo.notacreditoelectronica nce ON nce.id = nc.id
                WHERE nc.idempresa = @e
                  AND COALESCE(nc.activo, true) = true
                  AND (
                        (@idnc IS NOT NULL AND nc.id = @idnc)
                     OR nc.idpedido IN (
                            SELECT p.id FROM dbo.pedido p
                            WHERE p.idempresa = @e AND p.idpedidomovil = @pm
                        )
                  )
                ORDER BY nc.id DESC
                LIMIT 1
                """, conn);
            cmd.Parameters.AddWithValue("e", idEmpresa);
            cmd.Parameters.AddWithValue("idnc", (object?)idNcKnown ?? DBNull.Value);
            cmd.Parameters.AddWithValue("pm", erpPmId);
            await using var r = await cmd.ExecuteReaderAsync(ct);
            if (!await r.ReadAsync(ct))
                return idNcKnown is > 0 ? new ErpNotaCreditoInfo(idNcKnown.Value, null, null, true) : null;

            return new ErpNotaCreditoInfo(
                r.GetInt64(0),
                r.IsDBNull(1) ? null : Convert.ToInt64(r.GetValue(1)),
                r.IsDBNull(2) ? null : Convert.ToInt64(r.GetValue(2)),
                !r.IsDBNull(3) && r.GetBoolean(3));
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "No se pudo resolver NC ERP pm={Pm}", idPedidoMovil);
            return idNcKnown is > 0 ? new ErpNotaCreditoInfo(idNcKnown.Value, null, null, true) : null;
        }
    }

    private static IReadOnlyList<string> ChooseNcReportNames(bool electronica) =>
        electronica
            ? ["notacreditovarioselectronica", "notacreditoelectronica", "notacreditovarios"]
            : ["notacreditovarios", "notacreditovarioselectronica"];

    private static string BuildPentahoNcUrl(
        string rawBase, string report, string? subCarpeta, long idNotaCredito, long idEmpresa)
    {
        var baseUrl = NormalizePentahoBaseUrl(rawBase);
        var subPart = string.IsNullOrWhiteSpace(subCarpeta) ? "" : $":{subCarpeta.Trim()}";
        var path =
            $"comercial{subPart}:{report}.prpt/generatedContent?idnotacredito={idNotaCredito}&idempresa={idEmpresa}&output-target=pageable/pdf";
        return baseUrl.EndsWith(':')
            ? baseUrl + path
            : $"{baseUrl.TrimEnd('/')}/{path}";
    }

    private async Task<InvoicePdfResult> DownloadWithFallbackAsync(
        long idEmpresa, long idPm, long? nroPedido, bool originalClienteOnly, CancellationToken ct)
    {
        var localPm = await _db.PedidosMovil.AsNoTracking()
            .FirstOrDefaultAsync(p => p.ID == idPm && p.IDEmpresa == idEmpresa, ct)
            ?? throw new InvalidOperationException("Pedido no encontrado.");

        if (!FacturaSerie.EsFacturado(localPm.NroFactura))
            throw new InvalidOperationException("El pedido aún no está facturado (sin N° de factura).");

        var pentahoReady = _pentaho.Enabled && !string.IsNullOrWhiteSpace(_pentaho.UrlReportPentaho);
        if (pentahoReady)
        {
            try
            {
                return await FetchPdfCoreAsync(idEmpresa, idPm, nroPedido, originalClienteOnly, ct);
            }
            catch (Exception ex) when (ex is InvalidOperationException or TaskCanceledException or TimeoutException or WebException)
            {
                _logger.LogWarning(ex, "Pentaho falló pm={Pm}; generando comprobante local.", idPm);
            }
        }

        return await BuildLocalPdfAsync(idEmpresa, localPm, nroPedido, ct);
    }

    private async Task<InvoicePdfResult> BuildLocalPdfAsync(
        long idEmpresa, PedidoMovil pedido, long? nroPedido, CancellationToken ct)
    {
        var cart = await _db.CarritosCompra.AsNoTracking()
            .Where(c => c.IDEmpresa == idEmpresa && c.IDPedidoMovil == pedido.ID)
            .OrderByDescending(c => c.ID)
            .FirstOrDefaultAsync(ct);

        var detalles = await _db.DetallesPedidoMovil.AsNoTracking()
            .Where(d => d.IDPedidoMovil == pedido.ID && d.Activo)
            .ToListAsync(ct);
        var artIds = detalles.Select(d => d.IDArticulo).Distinct().ToList();
        var arts = await _db.Articulos.AsNoTracking()
            .Where(a => artIds.Contains(a.ID))
            .Select(a => new { a.ID, a.SKU, a.DescripcionArticulo })
            .ToDictionaryAsync(a => a.ID, ct);

        var empresa = await _db.Empresas.AsNoTracking()
            .Where(e => e.ID == idEmpresa)
            .Select(e => e.NombreFantasia ?? e.RazonSocial ?? "Tienda")
            .FirstOrDefaultAsync(ct) ?? "Tienda";

        var lines = detalles.Select(d =>
        {
            arts.TryGetValue(d.IDArticulo, out var a);
            var precio = (decimal)d.PrecioNetoConImpuesto;
            var cant = d.CantidadEmbalada;
            return new LocalComprobantePdf.Line(
                a?.SKU ?? "",
                a?.DescripcionArticulo ?? $"Artículo {d.IDArticulo}",
                cant,
                precio,
                precio * cant);
        }).ToList();

        var nroFac = pedido.NroFactura!.Value;
        var nroPed = nroPedido is > 0 ? nroPedido.Value : pedido.NroPedido;
        var bytes = LocalComprobantePdf.Build(new LocalComprobantePdf.Model(
            empresa,
            nroPed,
            nroFac,
            pedido.FechaPedido,
            cart?.RazonSocial ?? "",
            cart?.NumeroDocumento ?? "",
            cart?.TipoEntrega == TipoEntregaEcommerce.Pickup ? "Pickup" : "Delivery",
            pedido.Observacion ?? cart?.Observacion ?? "",
            lines,
            cart?.MontoTotal ?? lines.Sum(l => l.Monto),
            cart?.MontoIVA ?? 0));

        return new InvoicePdfResult(
            bytes,
            $"comprobante-{nroFac}.pdf",
            "application/pdf",
            nroFac,
            0);
    }

    private async Task<InvoicePdfResult> FetchPdfCoreAsync(
        long idEmpresa, long idPm, long? nroPedido, bool originalClienteOnly, CancellationToken ct)
    {
        var erp = await ResolveErpFacturaAsync(idEmpresa, idPm, nroPedido, ct)
            ?? throw new InvalidOperationException("No hay factura emitida en ERP para este pedido.");

        if (erp.IDPedidoErp is not > 0)
            throw new InvalidOperationException("No se resolvió el ID de Pedido ERP para Pentaho (idpedido).");

        var report = string.IsNullOrWhiteSpace(erp.ReportName)
            ? ChooseReportName(erp.TipoFactura, erp.Electronica, erp.StockOServicio)
            : erp.ReportName!;

        var sub = await ResolveSubCarpetaAsync(idEmpresa, report, ct);
        var url = BuildPentahoUrl(_pentaho.UrlReportPentaho, report, sub, erp.IDPedidoErp.Value, idEmpresa);

        _logger.LogInformation(
            "Pentaho factura nroEcom={Nro} idpedido={Ped} report={Rep} sub={Sub} url={Url}",
            nroPedido, erp.IDPedidoErp, report, sub, url);

        var bytes = await FetchPentahoBytesAsync(url, ct);
        if (!LooksLikePdf(bytes))
        {
            var head = Encoding.ASCII.GetString(bytes.AsSpan(0, Math.Min(80, bytes.Length)));
            _logger.LogWarning("Pentaho respuesta no-PDF url={Url} len={Len} head={Head}", url, bytes.Length, head);
            throw new InvalidOperationException("No se pudo obtener el PDF de Pentaho.");
        }

        if (originalClienteOnly)
        {
            var cropped = InvoicePdfOriginalCopy.KeepAdquirenteCopy(bytes);
            _logger.LogInformation(
                "Pentaho PDF ok nroEcom={Nro} sub={Sub} bytes={Bytes} clienteOriginal={Cropped}",
                nroPedido, sub, bytes.Length, cropped.Length);
            bytes = cropped;
        }
        else
        {
            _logger.LogInformation("Pentaho PDF ok nroEcom={Nro} sub={Sub} bytes={Bytes}", nroPedido, sub, bytes.Length);
        }

        var fileName = erp.NroFactura is > 0
            ? $"factura-{erp.NroFactura}.pdf"
            : $"factura-pedido-{nroPedido ?? idPm}.pdf";

        var local = await _db.PedidosMovil.FirstOrDefaultAsync(p => p.ID == idPm, ct);
        if (local is not null && erp.NroFactura is > 0 && local.NroFactura != erp.NroFactura)
        {
            local.NroFactura = erp.NroFactura;
            local.UltModificacion = DateTime.UtcNow;
            local.UserName = "pentaho-invoice";
            await _db.SaveChangesAsync(ct);
        }

        return new InvoicePdfResult(bytes, fileName, "application/pdf", erp.NroFactura, erp.IDPedidoErp.Value);
    }

    private async Task<List<long>> GetCuentaIdsAsync(long idUsuario, long idEmpresa, CancellationToken ct) =>
        await _db.UsuariosEcommerceCuenta.AsNoTracking()
            .Where(c => c.IDEmpresa == idEmpresa && c.IDUsuarioEcommerce == idUsuario && c.Activo)
            .Select(c => c.IDCuenta)
            .ToListAsync(ct);

    private sealed record ErpFacturaInfo(
        long? IDPedidoErp,
        long? NroFactura,
        int TipoFactura,
        bool Electronica,
        int StockOServicio,
        string? ReportName);

    private async Task<ErpFacturaInfo?> ResolveErpFacturaAsync(
        long idEmpresa, long idPedidoMovil, long? nroPedidoEcom, CancellationToken ct)
    {
        var local = await _db.PedidosMovil.AsNoTracking()
            .FirstOrDefaultAsync(p => p.ID == idPedidoMovil, ct);

        // Paridad ERP: el PDF usa idpedido = dbo.pedido.id (no PedidoMovil).
        var erpPmId = await _db.MigracionesPedidoLog.AsNoTracking()
            .Where(l => l.IDEmpresa == idEmpresa && l.IDPedidoMovil == idPedidoMovil && l.Estado == "ok" && l.IDPedidoMovilErp != null)
            .OrderByDescending(l => l.FechaIntento)
            .Select(l => l.IDPedidoMovilErp)
            .FirstOrDefaultAsync(ct) ?? idPedidoMovil;

        var erpCs = await _erpCs.ResolveAsync(idEmpresa, ct)
                    ?? _config.GetConnectionString("Erp");
        if (string.IsNullOrWhiteSpace(erpCs))
            return null; // Sin ERP no hay factura fiscal imprimible

        try
        {
            await using var conn = new NpgsqlConnection(erpCs);
            await conn.OpenAsync(ct);
            await using (var path = new NpgsqlCommand("SET search_path TO dbo, public", conn))
                await path.ExecuteNonQueryAsync(ct);

            // 1) Pedido ERP ligado al pedidomovil e-com (mismo criterio que sync factura / Pentaho idpedido)
            await using (var cmd = new NpgsqlCommand(
                             """
                             SELECT p.id, p.nrofactura, p.tipofactura,
                                    COALESCE(p.facturastockoservicio, 0) AS stockoservicio,
                                    CASE WHEN pfe.id IS NOT NULL THEN true ELSE false END AS electronica
                             FROM dbo.pedido p
                             LEFT JOIN dbo.pedidofacturaelectronica pfe ON pfe.id = p.id
                             WHERE p.idempresa = @e
                               AND p.idpedidomovil = @pm
                               AND p.nrofactura IS NOT NULL AND p.nrofactura > 0
                             ORDER BY p.id DESC
                             LIMIT 1
                             """, conn))
            {
                cmd.Parameters.AddWithValue("e", idEmpresa);
                cmd.Parameters.AddWithValue("pm", erpPmId);
                await using var r = await cmd.ExecuteReaderAsync(ct);
                if (await r.ReadAsync(ct))
                {
                    var idPed = r.GetInt64(0);
                    var nro = Convert.ToInt64(r.GetValue(1));
                    var tipo = r.IsDBNull(2) ? 0 : Convert.ToInt32(r.GetValue(2));
                    var stock = r.IsDBNull(3) ? 0 : Convert.ToInt32(r.GetValue(3));
                    var elec = !r.IsDBNull(4) && r.GetBoolean(4);
                    return new ErpFacturaInfo(idPed, nro, tipo, elec, stock,
                        ChooseReportName(tipo, elec, stock));
                }
            }

            // 2) Factura fiscal en dbo.factura ligada al pedidomovil ERP
            await using (var cmd = new NpgsqlCommand(
                             """
                             SELECT p.id, f.nrofactura, COALESCE(p.tipofactura, 0),
                                    COALESCE(p.facturastockoservicio, 0) AS stockoservicio,
                                    CASE WHEN pfe.id IS NOT NULL THEN true ELSE false END AS electronica
                             FROM dbo.factura f
                             INNER JOIN dbo.pedido p ON p.id = f.idpedido AND p.idempresa = f.idempresa
                             LEFT JOIN dbo.pedidofacturaelectronica pfe ON pfe.id = p.id
                             WHERE f.idempresa = @e
                               AND p.idpedidomovil = @pm
                               AND COALESCE(f.activo, true) = true
                               AND f.nrofactura IS NOT NULL AND f.nrofactura > 0
                             ORDER BY f.id DESC
                             LIMIT 1
                             """, conn))
            {
                cmd.Parameters.AddWithValue("e", idEmpresa);
                cmd.Parameters.AddWithValue("pm", erpPmId);
                await using var r = await cmd.ExecuteReaderAsync(ct);
                if (await r.ReadAsync(ct))
                {
                    var idPed = r.GetInt64(0);
                    var nro = Convert.ToInt64(r.GetValue(1));
                    var tipo = r.IsDBNull(2) ? 0 : Convert.ToInt32(r.GetValue(2));
                    var stock = r.IsDBNull(3) ? 0 : Convert.ToInt32(r.GetValue(3));
                    var elec = !r.IsDBNull(4) && r.GetBoolean(4);
                    return new ErpFacturaInfo(idPed, nro, tipo, elec, stock,
                        ChooseReportName(tipo, elec, stock));
                }
            }

            // 3) pedidomovil ERP ya numerado → buscar pedido hijo por nrofactura
            await using (var cmd = new NpgsqlCommand(
                             """
                             SELECT pm.nrofactura, pm.tipofactura
                             FROM dbo.pedidomovil pm
                             WHERE pm.idempresa = @e
                               AND pm.id = @pm
                               AND pm.nrofactura IS NOT NULL AND pm.nrofactura > 0
                             ORDER BY pm.id DESC
                             LIMIT 1
                             """, conn))
            {
                cmd.Parameters.AddWithValue("e", idEmpresa);
                cmd.Parameters.AddWithValue("pm", erpPmId);
                await using var r = await cmd.ExecuteReaderAsync(ct);
                if (await r.ReadAsync(ct))
                {
                    var nro = Convert.ToInt64(r.GetValue(0));
                    var tipo = r.IsDBNull(1) ? (local?.TipoFactura ?? 0) : Convert.ToInt32(r.GetValue(1));
                    await r.DisposeAsync();
                    var byNroFac = await FindPedidoByNroFacturaAsync(conn, idEmpresa, nro, ct);
                    if (byNroFac is not null) return byNroFac;
                    return new ErpFacturaInfo(null, nro, tipo, true, 0, ChooseReportName(tipo, true, 0));
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "No se pudo resolver factura ERP pm={Pm}", idPedidoMovil);
        }

        return null;
    }

    private static async Task<ErpFacturaInfo?> FindPedidoByNroFacturaAsync(
        NpgsqlConnection conn, long idEmpresa, long nroFactura, CancellationToken ct)
    {
        await using var cmd = new NpgsqlCommand(
            """
            SELECT p.id, p.nrofactura, p.tipofactura,
                   COALESCE(p.facturastockoservicio, 0),
                   CASE WHEN pfe.id IS NOT NULL THEN true ELSE false END
            FROM dbo.pedido p
            LEFT JOIN dbo.pedidofacturaelectronica pfe ON pfe.id = p.id
            WHERE p.idempresa = @e AND p.nrofactura = @n
            ORDER BY p.id DESC
            LIMIT 1
            """, conn);
        cmd.Parameters.AddWithValue("e", idEmpresa);
        cmd.Parameters.AddWithValue("n", nroFactura);
        await using var r = await cmd.ExecuteReaderAsync(ct);
        if (!await r.ReadAsync(ct)) return null;
        var tipo = r.IsDBNull(2) ? 0 : Convert.ToInt32(r.GetValue(2));
        var stock = r.IsDBNull(3) ? 0 : Convert.ToInt32(r.GetValue(3));
        var elec = !r.IsDBNull(4) && r.GetBoolean(4);
        return new ErpFacturaInfo(
            r.GetInt64(0), Convert.ToInt64(r.GetValue(1)), tipo, elec, stock,
            ChooseReportName(tipo, elec, stock));
    }

    private static async Task<ErpFacturaInfo?> FindPedidoByNroAsync(
        NpgsqlConnection conn, long idEmpresa, long nroPedido, CancellationToken ct)
    {
        await using var cmd = new NpgsqlCommand(
            """
            SELECT p.id, p.nrofactura, p.tipofactura,
                   COALESCE(p.facturastockoservicio, 0),
                   CASE WHEN pfe.id IS NOT NULL THEN true ELSE false END
            FROM dbo.pedido p
            LEFT JOIN dbo.pedidofacturaelectronica pfe ON pfe.id = p.id
            WHERE p.idempresa = @e
              AND p.nrofactura IS NOT NULL AND p.nrofactura > 0
              AND (p.nropedido = @n OR p.nropedidodispositivomovil = @n)
            ORDER BY p.id DESC
            LIMIT 1
            """, conn);
        cmd.Parameters.AddWithValue("e", idEmpresa);
        cmd.Parameters.AddWithValue("n", nroPedido);
        await using var r = await cmd.ExecuteReaderAsync(ct);
        if (!await r.ReadAsync(ct)) return null;
        var tipo = r.IsDBNull(2) ? 0 : Convert.ToInt32(r.GetValue(2));
        var stock = r.IsDBNull(3) ? 0 : Convert.ToInt32(r.GetValue(3));
        var elec = !r.IsDBNull(4) && r.GetBoolean(4);
        return new ErpFacturaInfo(
            r.GetInt64(0), Convert.ToInt64(r.GetValue(1)), tipo, elec, stock,
            ChooseReportName(tipo, elec, stock));
    }

    /// <summary>Paridad FacturaController.GenerateReportFacturaSalon / PortalDE.</summary>
    private static string ChooseReportName(int tipoFactura, bool electronica, int stockOServicio)
    {
        // ValoresFacturaStockOServicio: STOCK=0, SERVICIO=1 (típico)
        var baseName = stockOServicio == 1 ? "facturaservicio" : "facturasalon";
        if (tipoFactura == TipoFacturaErp.Reparto)
            baseName = "facturareparto";

        return electronica ? baseName + "electronica" : baseName;
    }

    private async Task<string?> ResolveSubCarpetaAsync(long idEmpresa, string nombreReporte, CancellationToken ct)
    {
        var erpCs = await _erpCs.ResolveAsync(idEmpresa, ct)
                    ?? _config.GetConnectionString("Erp");
        if (string.IsNullOrWhiteSpace(erpCs)) return null;
        try
        {
            await using var conn = new NpgsqlConnection(erpCs);
            await conn.OpenAsync(ct);
            await using (var path = new NpgsqlCommand("SET search_path TO dbo, public", conn))
                await path.ExecuteNonQueryAsync(ct);
            // Paridad ERP: lookup con nombre completo del reporte (ej. facturasalonelectronica).
            await using var cmd = new NpgsqlCommand(
                """
                SELECT COALESCE(nombresubcarpeta, '')
                FROM dbo.empresaconfiguracionreporte
                WHERE idempresa = @e
                  AND lower(trim(nombrereporte)) = lower(trim(@n))
                  AND COALESCE(activo, true) = true
                LIMIT 1
                """, conn);
            cmd.Parameters.AddWithValue("e", idEmpresa);
            cmd.Parameters.AddWithValue("n", nombreReporte);
            var v = await cmd.ExecuteScalarAsync(ct);
            var s = Convert.ToString(v);
            return string.IsNullOrWhiteSpace(s) ? null : s.Trim();
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Subcarpeta Pentaho no disponible (tabla/columnas).");
            return null;
        }
    }

    private static string NormalizePentahoBaseUrl(string raw)
    {
        var baseUrl = raw.TrimEnd('/');
        if (!baseUrl.EndsWith(':') && !baseUrl.EndsWith("/"))
            baseUrl += "/";
        return baseUrl;
    }

    /// <summary>
    /// Paridad ERP FacturaController.GenerateReportFacturaSalon / PortalDE:
    /// comercial[:subcarpeta]:{reporte}.prpt/generatedContent?idpedido=&amp;idempresa=&amp;output-target=pageable/pdf
    /// </summary>
    private static string BuildPentahoUrl(
        string rawBase, string report, string? subCarpeta, long idPedidoErp, long idEmpresa)
    {
        var baseUrl = NormalizePentahoBaseUrl(rawBase);
        var subPart = string.IsNullOrWhiteSpace(subCarpeta) ? "" : $":{subCarpeta.Trim()}";
        var path =
            $"comercial{subPart}:{report}.prpt/generatedContent?idpedido={idPedidoErp}&idempresa={idEmpresa}&output-target=pageable/pdf";
        return baseUrl.EndsWith(':')
            ? baseUrl + path
            : $"{baseUrl.TrimEnd('/')}/{path}";
    }

    private async Task<byte[]> FetchPentahoBytesAsync(string url, CancellationToken ct)
    {
        // Pentaho CE + HttpClient en .NET 8 cuelga en HTTP plano; WebClient es estable (ERP MyWebClient).
        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        timeoutCts.CancelAfter(TimeSpan.FromSeconds(Math.Clamp(_pentaho.TimeoutSeconds, 10, 180)));

#pragma warning disable SYSLIB0014
        using var wc = new WebClient { Proxy = null };
#pragma warning restore SYSLIB0014
        wc.Headers[HttpRequestHeader.Accept] = "application/pdf,*/*";
        wc.Headers[HttpRequestHeader.UserAgent] = "Datamex.ECommerce/1.0";

        try
        {
            return await wc.DownloadDataTaskAsync(new Uri(url)).WaitAsync(timeoutCts.Token);
        }
        catch (OperationCanceledException) when (!ct.IsCancellationRequested)
        {
            throw new TimeoutException($"Pentaho timeout url={url}");
        }
    }

    private static bool LooksLikePdf(byte[] bytes) =>
        bytes.Length >= 5
        && bytes[0] == (byte)'%'
        && bytes[1] == (byte)'P'
        && bytes[2] == (byte)'D'
        && bytes[3] == (byte)'F';

    private long RequireTenant()
    {
        if (!_tenant.IsResolved || !_tenant.IDEmpresa.HasValue)
            throw new InvalidOperationException("Tenant no resuelto.");
        return _tenant.IDEmpresa.Value;
    }
}
