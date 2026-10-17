using Datamex.ECommerce.Application.Abstractions;
using Datamex.ECommerce.Application.Admin;
using Datamex.ECommerce.Api.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Datamex.ECommerce.Api.Controllers;

[ApiController]
[Route("api/admin/listas-precios")]
[EnableRateLimiting("api")]
[Authorize]
public sealed class AdminListaPrecioController : ControllerBase
{
    private readonly IAdminListaPrecioService _svc;
    private readonly ITenantContext _tenant;

    public AdminListaPrecioController(IAdminListaPrecioService svc, ITenantContext tenant)
    {
        _svc = svc;
        _tenant = tenant;
    }

    [HttpGet]
    [RequirePermission("Site_App_Ecommerce_ListasPrecios", "Site_App_Ecommerce_ListasPrecios_ListaPrecio")]
    public async Task<ActionResult<IReadOnlyList<AdminListaPrecioDto>>> List(CancellationToken ct)
    {
        if (!_tenant.IsResolved) return BadRequest(new { error = "Falta tenant." });
        return Ok(await _svc.ListAsync(ct));
    }

    [HttpGet("{id:long}")]
    [RequirePermission(
        "Site_App_Ecommerce_ListasPrecios",
        "Site_App_Ecommerce_ListasPrecios_ListaPrecio",
        "Site_App_Ecommerce_ListasPrecios_ListaPrecio_View")]
    public async Task<ActionResult<AdminListaPrecioDto>> Get(long id, CancellationToken ct)
    {
        if (!_tenant.IsResolved) return BadRequest(new { error = "Falta tenant." });
        var row = await _svc.GetAsync(id, ct);
        return row is null ? NotFound() : Ok(row);
    }

    [HttpPost]
    [RequirePermission("Site_App_Ecommerce_ListasPrecios_ListaPrecio_Add")]
    public async Task<ActionResult<AdminListaPrecioDto>> Create(
        [FromBody] UpsertListaPrecioRequest request, CancellationToken ct)
    {
        if (!_tenant.IsResolved) return BadRequest(new { error = "Falta tenant." });
        try { return Ok(await _svc.CreateAsync(request, ct)); }
        catch (Exception ex) when (ex is InvalidOperationException or ArgumentException)
        { return BadRequest(new { error = ex.Message }); }
    }

    [HttpPut("{id:long}")]
    [RequirePermission("Site_App_Ecommerce_ListasPrecios_ListaPrecio_Edit")]
    public async Task<ActionResult<AdminListaPrecioDto>> Update(
        long id, [FromBody] UpsertListaPrecioRequest request, CancellationToken ct)
    {
        if (!_tenant.IsResolved) return BadRequest(new { error = "Falta tenant." });
        try { return Ok(await _svc.UpdateAsync(id, request, ct)); }
        catch (Exception ex) when (ex is InvalidOperationException or ArgumentException)
        { return BadRequest(new { error = ex.Message }); }
    }

    [HttpDelete("{id:long}")]
    [RequirePermission("Site_App_Ecommerce_ListasPrecios_ListaPrecio_Del")]
    public async Task<IActionResult> Delete(long id, CancellationToken ct)
    {
        if (!_tenant.IsResolved) return BadRequest(new { error = "Falta tenant." });
        try
        {
            await _svc.DeleteAsync(id, ct);
            return NoContent();
        }
        catch (InvalidOperationException ex)
        { return BadRequest(new { error = ex.Message }); }
    }

    [HttpGet("embalajes")]
    [RequirePermission("Site_App_Ecommerce_ListasPrecios_DetListPrecioCondicionVenta")]
    public async Task<ActionResult<IReadOnlyList<AdminEmbalajeOptionDto>>> Embalajes(
        [FromQuery] string? search, [FromQuery] int take = 30, CancellationToken ct = default)
    {
        if (!_tenant.IsResolved) return BadRequest(new { error = "Falta tenant." });
        return Ok(await _svc.SearchEmbalajesAsync(search, take, ct));
    }

    [HttpGet("{id:long}/detalles")]
    [RequirePermission("Site_App_Ecommerce_ListasPrecios_DetListPrecioCondicionVenta")]
    public async Task<ActionResult<IReadOnlyList<AdminDetalleListaDto>>> Detalles(long id, CancellationToken ct)
    {
        if (!_tenant.IsResolved) return BadRequest(new { error = "Falta tenant." });
        try { return Ok(await _svc.ListDetallesAsync(id, ct)); }
        catch (InvalidOperationException ex)
        { return BadRequest(new { error = ex.Message }); }
    }

    [HttpPost("{id:long}/detalles")]
    [RequirePermission("Site_App_Ecommerce_ListasPrecios_DetListPrecioCondicionVenta_Add")]
    public async Task<ActionResult<AdminDetalleListaDto>> UpsertDetalle(
        long id, [FromBody] UpsertDetalleListaRequest request, CancellationToken ct)
    {
        if (!_tenant.IsResolved) return BadRequest(new { error = "Falta tenant." });
        try { return Ok(await _svc.UpsertDetalleAsync(id, request, ct)); }
        catch (Exception ex) when (ex is InvalidOperationException or ArgumentException)
        { return BadRequest(new { error = ex.Message }); }
    }

    [HttpPut("{id:long}/detalles/{detalleId:long}")]
    [RequirePermission("Site_App_Ecommerce_ListasPrecios_DetListPrecioCondicionVenta_Add")]
    public async Task<ActionResult<AdminDetalleListaDto>> UpdateDetalle(
        long id, long detalleId, [FromBody] UpdateDetalleListaRequest request, CancellationToken ct)
    {
        if (!_tenant.IsResolved) return BadRequest(new { error = "Falta tenant." });
        try { return Ok(await _svc.UpdateDetalleAsync(id, detalleId, request, ct)); }
        catch (Exception ex) when (ex is InvalidOperationException or ArgumentException)
        { return BadRequest(new { error = ex.Message }); }
    }

    [HttpDelete("{id:long}/detalles/{detalleId:long}")]
    [RequirePermission("Site_App_Ecommerce_ListasPrecios_DetListPrecioCondicionVenta_Add")]
    public async Task<IActionResult> DeleteDetalle(long id, long detalleId, CancellationToken ct)
    {
        if (!_tenant.IsResolved) return BadRequest(new { error = "Falta tenant." });
        try
        {
            await _svc.DeleteDetalleAsync(id, detalleId, ct);
            return NoContent();
        }
        catch (InvalidOperationException ex)
        { return BadRequest(new { error = ex.Message }); }
    }

    [HttpPost("{id:long}/autorizar")]
    [RequirePermission("Site_App_Ecommerce_ListasPrecios_AutDetListPrecio")]
    public async Task<ActionResult<AdminListaPrecioDto>> Autorizar(long id, CancellationToken ct)
    {
        if (!_tenant.IsResolved) return BadRequest(new { error = "Falta tenant." });
        try { return Ok(await _svc.AutorizarAsync(id, ct)); }
        catch (Exception ex) when (ex is InvalidOperationException or ArgumentException)
        { return BadRequest(new { error = ex.Message }); }
    }

    [HttpPost("{id:long}/anular")]
    [RequirePermission("Site_App_Ecommerce_ListasPrecios_AutDetListPrecio")]
    public async Task<ActionResult<AdminListaPrecioDto>> Anular(long id, CancellationToken ct)
    {
        if (!_tenant.IsResolved) return BadRequest(new { error = "Falta tenant." });
        try { return Ok(await _svc.AnularAsync(id, ct)); }
        catch (Exception ex) when (ex is InvalidOperationException or ArgumentException)
        { return BadRequest(new { error = ex.Message }); }
    }

    [HttpPost("{id:long}/cancelar")]
    [RequirePermission("Site_App_Ecommerce_ListasPrecios_AutDetListPrecio")]
    public async Task<ActionResult<AdminListaPrecioDto>> Cancelar(long id, CancellationToken ct)
    {
        if (!_tenant.IsResolved) return BadRequest(new { error = "Falta tenant." });
        try { return Ok(await _svc.CancelarAsync(id, ct)); }
        catch (Exception ex) when (ex is InvalidOperationException or ArgumentException)
        { return BadRequest(new { error = ex.Message }); }
    }

    /// <summary>Paridad ERP CambioVigenciaAsignacionPrecio (bulk).</summary>
    [HttpPost("cambio-vigencia")]
    [RequirePermission("Site_App_Ecommerce_ListasPrecios_ListaPrecio_Edit")]
    public async Task<ActionResult<object>> CambioVigencia(
        [FromBody] CambioVigenciaListasRequest request, CancellationToken ct)
    {
        if (!_tenant.IsResolved) return BadRequest(new { error = "Falta tenant." });
        try
        {
            var n = await _svc.CambioVigenciaAsync(request, ct);
            return Ok(new { updatedCount = n });
        }
        catch (Exception ex) when (ex is InvalidOperationException or ArgumentException)
        {
            return BadRequest(new { error = ex.Message });
        }
    }
}
