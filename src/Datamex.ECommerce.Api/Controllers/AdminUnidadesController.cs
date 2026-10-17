using Datamex.ECommerce.Application.Abstractions;
using Datamex.ECommerce.Application.Admin;
using Datamex.ECommerce.Api.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Datamex.ECommerce.Api.Controllers;

[ApiController]
[Route("api/admin")]
[EnableRateLimiting("api")]
[Authorize]
public sealed class AdminUnidadesController : ControllerBase
{
    private readonly IAdminUnidadesService _svc;
    private readonly ITenantContext _tenant;

    public AdminUnidadesController(IAdminUnidadesService svc, ITenantContext tenant)
    {
        _svc = svc;
        _tenant = tenant;
    }

    [HttpGet("unidades-medida")]
    [RequirePermission("Site_App_Ecommerce_Productos")]
    public async Task<ActionResult<IReadOnlyList<AdminUnidadMedidaDto>>> ListUnidades(CancellationToken ct)
    {
        if (!_tenant.IsResolved) return BadRequest(new { error = "Falta tenant." });
        return Ok(await _svc.ListUnidadesAsync(ct));
    }

    [HttpPost("unidades-medida")]
    [RequirePermission("Site_App_Ecommerce_Productos")]
    public async Task<ActionResult<AdminUnidadMedidaDto>> CreateUnidad(
        [FromBody] UpsertUnidadMedidaRequest request, CancellationToken ct)
    {
        if (!_tenant.IsResolved) return BadRequest(new { error = "Falta tenant." });
        try { return Ok(await _svc.UpsertUnidadAsync(null, request, ct)); }
        catch (Exception ex) when (ex is InvalidOperationException or ArgumentException)
        { return BadRequest(new { error = ex.Message }); }
    }

    [HttpPut("unidades-medida/{id:long}")]
    [RequirePermission("Site_App_Ecommerce_Productos")]
    public async Task<ActionResult<AdminUnidadMedidaDto>> UpdateUnidad(
        long id, [FromBody] UpsertUnidadMedidaRequest request, CancellationToken ct)
    {
        if (!_tenant.IsResolved) return BadRequest(new { error = "Falta tenant." });
        try { return Ok(await _svc.UpsertUnidadAsync(id, request, ct)); }
        catch (Exception ex) when (ex is InvalidOperationException or ArgumentException)
        { return BadRequest(new { error = ex.Message }); }
    }

    [HttpDelete("unidades-medida/{id:long}")]
    [RequirePermission("Site_App_Ecommerce_Productos")]
    public async Task<IActionResult> DeleteUnidad(long id, CancellationToken ct)
    {
        if (!_tenant.IsResolved) return BadRequest(new { error = "Falta tenant." });
        try { await _svc.DeleteUnidadAsync(id, ct); return NoContent(); }
        catch (InvalidOperationException ex) { return BadRequest(new { error = ex.Message }); }
    }

    [HttpGet("products/{idArticulo:long}/embalajes")]
    [RequirePermission("Site_App_Ecommerce_Productos")]
    public async Task<ActionResult<IReadOnlyList<AdminProductEmbalajeDto>>> ListEmbalajes(long idArticulo, CancellationToken ct)
    {
        if (!_tenant.IsResolved) return BadRequest(new { error = "Falta tenant." });
        try { return Ok(await _svc.ListEmbalajesAsync(idArticulo, ct)); }
        catch (InvalidOperationException ex) { return BadRequest(new { error = ex.Message }); }
    }

    [HttpPost("products/{idArticulo:long}/embalajes")]
    [RequirePermission("Site_App_Ecommerce_Productos")]
    public async Task<ActionResult<AdminProductEmbalajeDto>> CreateEmbalaje(
        long idArticulo, [FromBody] UpsertProductEmbalajeRequest request, CancellationToken ct)
    {
        if (!_tenant.IsResolved) return BadRequest(new { error = "Falta tenant." });
        try { return Ok(await _svc.UpsertEmbalajeAsync(idArticulo, null, request, ct)); }
        catch (Exception ex) when (ex is InvalidOperationException or ArgumentException)
        { return BadRequest(new { error = ex.Message }); }
    }

    [HttpPut("products/{idArticulo:long}/embalajes/{id:long}")]
    [RequirePermission("Site_App_Ecommerce_Productos")]
    public async Task<ActionResult<AdminProductEmbalajeDto>> UpdateEmbalaje(
        long idArticulo, long id, [FromBody] UpsertProductEmbalajeRequest request, CancellationToken ct)
    {
        if (!_tenant.IsResolved) return BadRequest(new { error = "Falta tenant." });
        try { return Ok(await _svc.UpsertEmbalajeAsync(idArticulo, id, request, ct)); }
        catch (Exception ex) when (ex is InvalidOperationException or ArgumentException)
        { return BadRequest(new { error = ex.Message }); }
    }

    [HttpDelete("products/{idArticulo:long}/embalajes/{id:long}")]
    [RequirePermission("Site_App_Ecommerce_Productos")]
    public async Task<IActionResult> DeleteEmbalaje(long idArticulo, long id, CancellationToken ct)
    {
        if (!_tenant.IsResolved) return BadRequest(new { error = "Falta tenant." });
        try { await _svc.DeleteEmbalajeAsync(idArticulo, id, ct); return NoContent(); }
        catch (InvalidOperationException ex) { return BadRequest(new { error = ex.Message }); }
    }

    [HttpGet("embalajes/{idEmbalaje:long}/codigos-barra")]
    [RequirePermission("Site_App_Ecommerce_Productos")]
    public async Task<ActionResult<IReadOnlyList<AdminCodigoBarraDto>>> ListCodigos(long idEmbalaje, CancellationToken ct)
    {
        if (!_tenant.IsResolved) return BadRequest(new { error = "Falta tenant." });
        try { return Ok(await _svc.ListCodigosBarraAsync(idEmbalaje, ct)); }
        catch (InvalidOperationException ex) { return BadRequest(new { error = ex.Message }); }
    }

    [HttpPost("embalajes/{idEmbalaje:long}/codigos-barra")]
    [RequirePermission("Site_App_Ecommerce_Productos")]
    public async Task<ActionResult<AdminCodigoBarraDto>> CreateCodigo(
        long idEmbalaje, [FromBody] UpsertCodigoBarraRequest request, CancellationToken ct)
    {
        if (!_tenant.IsResolved) return BadRequest(new { error = "Falta tenant." });
        try { return Ok(await _svc.UpsertCodigoBarraAsync(idEmbalaje, null, request, ct)); }
        catch (Exception ex) when (ex is InvalidOperationException or ArgumentException)
        { return BadRequest(new { error = ex.Message }); }
    }

    [HttpPut("embalajes/{idEmbalaje:long}/codigos-barra/{id:long}")]
    [RequirePermission("Site_App_Ecommerce_Productos")]
    public async Task<ActionResult<AdminCodigoBarraDto>> UpdateCodigo(
        long idEmbalaje, long id, [FromBody] UpsertCodigoBarraRequest request, CancellationToken ct)
    {
        if (!_tenant.IsResolved) return BadRequest(new { error = "Falta tenant." });
        try { return Ok(await _svc.UpsertCodigoBarraAsync(idEmbalaje, id, request, ct)); }
        catch (Exception ex) when (ex is InvalidOperationException or ArgumentException)
        { return BadRequest(new { error = ex.Message }); }
    }

    [HttpDelete("embalajes/{idEmbalaje:long}/codigos-barra/{id:long}")]
    [RequirePermission("Site_App_Ecommerce_Productos")]
    public async Task<IActionResult> DeleteCodigo(long idEmbalaje, long id, CancellationToken ct)
    {
        if (!_tenant.IsResolved) return BadRequest(new { error = "Falta tenant." });
        try { await _svc.DeleteCodigoBarraAsync(idEmbalaje, id, ct); return NoContent(); }
        catch (InvalidOperationException ex) { return BadRequest(new { error = ex.Message }); }
    }
}
