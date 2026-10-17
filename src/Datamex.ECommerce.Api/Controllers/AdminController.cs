using Datamex.ECommerce.Application.Abstractions;
using Datamex.ECommerce.Application.Admin;
using Datamex.ECommerce.Application.Catalog;
using Datamex.ECommerce.Application.Inventory;
using Datamex.ECommerce.Application.Invoices;
using Datamex.ECommerce.Api.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Datamex.ECommerce.Api.Controllers;

[ApiController]
[Route("api/admin")]
[EnableRateLimiting("api")]
[Authorize]
public sealed class AdminController : ControllerBase
{
    private readonly IAdminCatalogService _catalog;
    private readonly IAdminOrdersService _orders;
    private readonly IAdminDashboardService _dashboard;
    private readonly IInvoiceService _invoices;
    private readonly IInventoryService _inventory;
    private readonly ITenantContext _tenant;

    public AdminController(
        IAdminCatalogService catalog,
        IAdminOrdersService orders,
        IAdminDashboardService dashboard,
        IInvoiceService invoices,
        IInventoryService inventory,
        ITenantContext tenant)
    {
        _catalog = catalog;
        _orders = orders;
        _dashboard = dashboard;
        _invoices = invoices;
        _inventory = inventory;
        _tenant = tenant;
    }

    [HttpGet("dashboard")]
    [RequirePermission("Site_App_Ecommerce")]
    public async Task<ActionResult<AdminDashboardDto>> Dashboard(CancellationToken ct)
    {
        if (!_tenant.IsResolved) return BadRequest(new { error = "Falta tenant." });
        return Ok(await _dashboard.GetAsync(ct));
    }

    [HttpGet("products")]
    [RequirePermission("Site_App_Ecommerce_Productos")]
    public async Task<ActionResult<PagedResult<AdminProductDto>>> Products(
        [FromQuery] string? search,
        [FromQuery] string? estado,
        [FromQuery] bool? activo,
        [FromQuery] bool? activoEcommerce,
        [FromQuery] string? calidad,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        if (!_tenant.IsResolved) return BadRequest(new { error = "Falta tenant." });
        return Ok(await _catalog.SearchAsync(search, estado, page, pageSize, activo, activoEcommerce, calidad, ct));
    }

    [HttpGet("products/{id:long}")]
    [RequirePermission("Site_App_Ecommerce_Productos")]
    public async Task<ActionResult<AdminProductDto>> Product(long id, CancellationToken ct)
    {
        if (!_tenant.IsResolved) return BadRequest(new { error = "Falta tenant." });
        var p = await _catalog.GetAsync(id, ct);
        return p is null ? NotFound() : Ok(p);
    }

    [HttpGet("products/{id:long}/detail")]
    [RequirePermission("Site_App_Ecommerce_Productos")]
    public async Task<ActionResult<AdminArticuloDetailDto>> ProductDetail(long id, CancellationToken ct)
    {
        if (!_tenant.IsResolved) return BadRequest(new { error = "Falta tenant." });
        var p = await _catalog.GetDetailAsync(id, ct);
        return p is null ? NotFound() : Ok(p);
    }

    [HttpPost("products")]
    [RequirePermission("Site_App_Ecommerce_Productos_Edit")]
    public async Task<ActionResult<AdminArticuloDetailDto>> CreateProduct(
        [FromBody] CreateAdminArticuloRequest request, CancellationToken ct)
    {
        if (!_tenant.IsResolved) return BadRequest(new { error = "Falta tenant." });
        try
        {
            var created = await _catalog.CreateAsync(request, ct);
            return CreatedAtAction(nameof(ProductDetail), new { id = created.Id }, created);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpPut("products/{id:long}/master")]
    [RequirePermission("Site_App_Ecommerce_Productos_Edit")]
    public async Task<ActionResult<AdminArticuloDetailDto>> UpsertProductMaster(
        long id, [FromBody] UpsertAdminArticuloRequest request, CancellationToken ct)
    {
        if (!_tenant.IsResolved) return BadRequest(new { error = "Falta tenant." });
        try
        {
            return Ok(await _catalog.UpsertMasterAsync(id, request, ct));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpDelete("products/{id:long}")]
    [RequirePermission("Site_App_Ecommerce_Productos_Edit")]
    public async Task<IActionResult> DeleteProduct(long id, CancellationToken ct)
    {
        if (!_tenant.IsResolved) return BadRequest(new { error = "Falta tenant." });
        try
        {
            await _catalog.DeleteAsync(id, ct);
            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpPut("products/{id:long}")]
    [RequirePermission("Site_App_Ecommerce_Productos_Edit")]
    public async Task<ActionResult<AdminProductDto>> UpdateProduct(
        long id, [FromBody] UpdateAdminProductRequest request, CancellationToken ct)
    {
        if (!_tenant.IsResolved) return BadRequest(new { error = "Falta tenant." });
        try
        {
            return Ok(await _catalog.UpdateAsync(id, request, ct));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpPut("products/{id:long}/stock")]
    [RequirePermission("Site_App_Ecommerce_Productos_Stock_Edit")]
    public async Task<ActionResult<AdminProductDto>> UpdateStock(
        long id, [FromBody] UpdateStockRequest request, CancellationToken ct)
    {
        if (!_tenant.IsResolved) return BadRequest(new { error = "Falta tenant." });
        try
        {
            return Ok(await _catalog.UpdateStockAsync(id, request, ct));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpGet("inventory/movimientos")]
    [RequirePermission("Site_App_Ecommerce_Productos")]
    public async Task<ActionResult<IReadOnlyList<InventarioMovimientoDto>>> Movimientos(
        [FromQuery] long? articuloId,
        [FromQuery] int take = 100,
        CancellationToken ct = default)
    {
        if (!_tenant.IsResolved) return BadRequest(new { error = "Falta tenant." });
        return Ok(await _inventory.ListMovimientosAsync(articuloId, take, ct));
    }

    [HttpPut("products/bulk-activo")]
    [RequirePermission("Site_App_Ecommerce_Productos_ActivarInactivar")]
    public async Task<ActionResult<BulkActivoEcommerceResult>> BulkActivo(
        [FromBody] BulkActivoEcommerceRequest request, CancellationToken ct)
    {
        if (!_tenant.IsResolved) return BadRequest(new { error = "Falta tenant." });
        try
        {
            return Ok(await _catalog.BulkSetActivoEcommerceAsync(request, ct));
        }
        catch (Exception ex) when (ex is InvalidOperationException or ArgumentException)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpGet("products/{id:long}/images")]
    [RequirePermission("Site_App_Ecommerce_Productos_Imagenes")]
    public async Task<ActionResult<IReadOnlyList<AdminProductImageDto>>> ListImages(long id, CancellationToken ct)
    {
        if (!_tenant.IsResolved) return BadRequest(new { error = "Falta tenant." });
        try
        {
            return Ok(await _catalog.ListImagesAsync(id, ct));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpPost("products/{id:long}/images")]
    [RequirePermission("Site_App_Ecommerce_Productos_Imagenes")]
    public async Task<ActionResult<AdminProductImageDto>> CreateImage(
        long id, [FromBody] UpsertProductImageRequest request, CancellationToken ct)
    {
        if (!_tenant.IsResolved) return BadRequest(new { error = "Falta tenant." });
        try
        {
            return Ok(await _catalog.CreateImageAsync(id, request, ct));
        }
        catch (Exception ex) when (ex is InvalidOperationException or ArgumentException)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpPost("products/{id:long}/images/upload")]
    [RequirePermission("Site_App_Ecommerce_Productos_Imagenes")]
    [RequestSizeLimit(25_000_000)]
    public async Task<ActionResult<AdminProductImageDto>> UploadImage(
        long id,
        IFormFile file,
        [FromForm] string? titulo,
        [FromForm] bool esPrincipal = false,
        CancellationToken ct = default)
    {
        if (!_tenant.IsResolved) return BadRequest(new { error = "Falta tenant." });
        if (file is null || file.Length == 0)
            return BadRequest(new { error = "Archivo requerido." });

        try
        {
            await using var stream = file.OpenReadStream();
            return Ok(await _catalog.UploadImageAsync(
                id, stream, file.ContentType, file.FileName, titulo, esPrincipal, ct));
        }
        catch (Exception ex) when (ex is InvalidOperationException or ArgumentException)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpPost("products/{id:long}/images/reorder")]
    [RequirePermission("Site_App_Ecommerce_Productos_Imagenes")]
    public async Task<IActionResult> ReorderImages(
        long id, [FromBody] ReorderImagesRequest body, CancellationToken ct)
    {
        if (!_tenant.IsResolved) return BadRequest(new { error = "Falta tenant." });
        try
        {
            await _catalog.ReorderImagesAsync(id, body.OrderedIds, ct);
            return NoContent();
        }
        catch (Exception ex) when (ex is InvalidOperationException or ArgumentException)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpPost("products/{id:long}/images/{imageId:long}/principal")]
    [RequirePermission("Site_App_Ecommerce_Productos_Imagenes")]
    public async Task<ActionResult<AdminProductImageDto>> SetPrincipal(
        long id, long imageId, CancellationToken ct)
    {
        if (!_tenant.IsResolved) return BadRequest(new { error = "Falta tenant." });
        try
        {
            return Ok(await _catalog.SetPrincipalImageAsync(id, imageId, ct));
        }
        catch (Exception ex) when (ex is InvalidOperationException or ArgumentException)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpPut("products/{id:long}/images/{imageId:long}")]
    [RequirePermission("Site_App_Ecommerce_Productos_Imagenes")]
    public async Task<ActionResult<AdminProductImageDto>> UpdateImage(
        long id, long imageId, [FromBody] UpsertProductImageRequest request, CancellationToken ct)
    {
        if (!_tenant.IsResolved) return BadRequest(new { error = "Falta tenant." });
        try
        {
            return Ok(await _catalog.UpdateImageAsync(id, imageId, request, ct));
        }
        catch (Exception ex) when (ex is InvalidOperationException or ArgumentException)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpDelete("products/{id:long}/images/{imageId:long}")]
    [RequirePermission("Site_App_Ecommerce_Productos_Imagenes")]
    public async Task<IActionResult> DeleteImage(long id, long imageId, CancellationToken ct)
    {
        if (!_tenant.IsResolved) return BadRequest(new { error = "Falta tenant." });
        try
        {
            await _catalog.DeleteImageAsync(id, imageId, ct);
            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpGet("orders")]
    [RequirePermission("Site_App_Ecommerce_Pedidos")]
    public async Task<ActionResult<PagedResult<AdminOrderListItemDto>>> Orders(
        [FromQuery] string? search,
        [FromQuery] bool? migrado,
        [FromQuery] string? estadoFulfillment,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        if (!_tenant.IsResolved) return BadRequest(new { error = "Falta tenant." });
        return Ok(await _orders.SearchAsync(search, migrado, page, pageSize, estadoFulfillment, ct));
    }

    [HttpGet("orders/{id:long}")]
    [RequirePermission("Site_App_Ecommerce_Pedidos_View")]
    public async Task<ActionResult<AdminOrderDetailDto>> Order(long id, CancellationToken ct)
    {
        if (!_tenant.IsResolved) return BadRequest(new { error = "Falta tenant." });
        var o = await _orders.GetAsync(id, ct);
        return o is null ? NotFound() : Ok(o);
    }

    [HttpGet("orders/{id:long}/factura.pdf")]
    [RequirePermission("Site_App_Ecommerce_Pedidos_View")]
    public async Task<IActionResult> OrderFacturaPdf(long id, CancellationToken ct)
    {
        if (!_tenant.IsResolved) return BadRequest(new { error = "Falta tenant." });
        try
        {
            var pdf = await _invoices.DownloadPdfByPedidoMovilAsync(id, ct);
            Response.Headers["Content-Disposition"] = $"inline; filename=\"{pdf.FileName}\"";
            return File(pdf.Content, "application/pdf");
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    public record AsignarFacturaRequest(long? NroFactura = null);
    public record AsignarFacturaBulkRequest(IReadOnlyList<long> Ids);

    [HttpPost("orders/{id:long}/factura")]
    [RequirePermission("Site_App_Ecommerce_Pedidos")]
    public async Task<ActionResult<AsignarFacturaResult>> AsignarFactura(
        long id, [FromBody] AsignarFacturaRequest? body, CancellationToken ct)
    {
        if (!_tenant.IsResolved) return BadRequest(new { error = "Falta tenant." });
        try
        {
            return Ok(await _orders.AsignarFacturaAsync(id, body?.NroFactura, ct));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpPost("orders/{id:long}/factura/sync-erp")]
    [RequirePermission("Site_App_Ecommerce_Pedidos")]
    public async Task<ActionResult<ErpFacturaSyncResultDto>> SyncFacturaErp(long id, CancellationToken ct)
    {
        if (!_tenant.IsResolved) return BadRequest(new { error = "Falta tenant." });
        try
        {
            return Ok(await _orders.SyncFacturaErpAsync(id, ct));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpPost("orders/facturar")]
    [RequirePermission("Site_App_Ecommerce_Pedidos")]
    public async Task<ActionResult<AsignarFacturaBulkResult>> AsignarFacturaBulk(
        [FromBody] AsignarFacturaBulkRequest body, CancellationToken ct)
    {
        if (!_tenant.IsResolved) return BadRequest(new { error = "Falta tenant." });
        if (body.Ids is null || body.Ids.Count == 0)
            return BadRequest(new { error = "Ids requeridos." });
        return Ok(await _orders.AsignarFacturaBulkAsync(body.Ids, ct));
    }

    [HttpPost("orders/migrar")]
    [RequirePermission("Site_App_Ecommerce_Pedidos")]
    public async Task<ActionResult<MarkMigradosResult>> MigrarOrders(
        [FromBody] MarkMigradosRequest body, CancellationToken ct)
    {
        if (!_tenant.IsResolved) return BadRequest(new { error = "Falta tenant." });
        return Ok(await _orders.MarkMigradosAsync(body.Ids, ct));
    }

    /// <summary>Export CSV de pedidos e-com (filtros opcionales de fecha).</summary>
    [HttpGet("orders/export")]
    [HttpGet("pedidos/export")]
    [HttpPost("orders/export")]
    [HttpPost("pedidos/export")]
    [RequirePermission("Site_App_Ecommerce_Pedidos")]
    public async Task<IActionResult> ExportOrders(
        [FromQuery] DateTime? fechaDesde,
        [FromQuery] DateTime? fechaHasta,
        CancellationToken ct = default)
    {
        if (!_tenant.IsResolved) return BadRequest(new { error = "Falta tenant." });
        var rows = await _orders.GetExportAsync(fechaDesde, fechaHasta, ct);
        var csv = ToOrdersCsv(rows);
        return File(System.Text.Encoding.UTF8.GetBytes(csv), "text/csv",
            $"pedidos-export-{DateTime.UtcNow:yyyyMMddHHmm}.csv");
    }

    private static string ToOrdersCsv(IReadOnlyList<AdminOrderExportRowDto> rows)
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine("IdPedidoMovil;NroPedido;FechaPedido;RazonSocial;Documento;MontoTotal;TipoEntrega;TipoPago;Estado;Migrado;LugarEntrega;NroFactura");
        foreach (var r in rows)
        {
            sb.Append(r.IdPedidoMovil).Append(';')
                .Append(r.NroPedido).Append(';')
                .Append(r.FechaPedido.ToString("o")).Append(';')
                .Append(Csv(r.RazonSocial)).Append(';')
                .Append(Csv(r.NumeroDocumento)).Append(';')
                .Append(r.MontoTotal.ToString(System.Globalization.CultureInfo.InvariantCulture)).Append(';')
                .Append(r.TipoEntrega).Append(';')
                .Append(r.TipoPago).Append(';')
                .Append(r.Estado).Append(';')
                .Append(r.Migrado ? "1" : "0").Append(';')
                .Append(Csv(r.LugarEntrega ?? "")).Append(';')
                .Append(r.NroFactura?.ToString() ?? "")
                .AppendLine();
        }
        return sb.ToString();
    }

    private static string Csv(string v) =>
        "\"" + v.Replace("\"", "\"\"") + "\"";
}

public record ReorderImagesRequest(IReadOnlyList<long> OrderedIds);
