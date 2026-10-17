using Datamex.ECommerce.Application.Abstractions;
using Datamex.ECommerce.Application.Admin;
using Datamex.ECommerce.Api.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;

namespace Datamex.ECommerce.Api.Controllers;

[ApiController]
[Route("api/admin/parametros-producto")]
[EnableRateLimiting("api")]
[Authorize]
public sealed class AdminParametrosProductoController : ControllerBase
{
    private readonly IAdminParametrosProductoService _svc;
    private readonly ITenantContext _tenant;

    public AdminParametrosProductoController(
        IAdminParametrosProductoService svc, ITenantContext tenant)
    {
        _svc = svc;
        _tenant = tenant;
    }

    [HttpGet]
    [RequirePermission("Site_App_Ecommerce_Productos")]
    public async Task<ActionResult<IReadOnlyList<CatalogoParametroDto>>> List(
        [FromQuery] string tipo,
        [FromQuery] bool incluirInactivos = false,
        CancellationToken ct = default)
    {
        if (!_tenant.IsResolved) return BadRequest(new { error = "Falta tenant." });
        try
        {
            return Ok(await _svc.ListAsync(tipo, incluirInactivos, ct));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex) when (IsTransientDb(ex))
        {
            return StatusCode(StatusCodes.Status503ServiceUnavailable, new
            {
                error = "Base de datos no disponible temporalmente. Reintentá en unos segundos.",
                detail = ex.Message
            });
        }
    }

    private static bool IsTransientDb(Exception ex)
    {
        for (var e = ex; e != null; e = e.InnerException!)
        {
            if (e is TimeoutException or DbUpdateException)
                return true;
            var name = e.GetType().FullName ?? e.GetType().Name;
            if (name.Contains("Npgsql", StringComparison.OrdinalIgnoreCase))
                return true;
        }
        return false;
    }

    [HttpPost]
    [RequirePermission("Site_App_Ecommerce_Productos_Edit")]
    public async Task<ActionResult<CatalogoParametroDto>> Create(
        [FromBody] UpsertCatalogoParametroRequest request, CancellationToken ct)
    {
        if (!_tenant.IsResolved) return BadRequest(new { error = "Falta tenant." });
        try
        {
            return Ok(await _svc.CreateAsync(request, ct));
        }
        catch (Exception ex) when (ex is ArgumentException or InvalidOperationException)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpPut("{id:long}")]
    [RequirePermission("Site_App_Ecommerce_Productos_Edit")]
    public async Task<ActionResult<CatalogoParametroDto>> Update(
        long id, [FromBody] UpsertCatalogoParametroRequest request, CancellationToken ct)
    {
        if (!_tenant.IsResolved) return BadRequest(new { error = "Falta tenant." });
        try
        {
            return Ok(await _svc.UpdateAsync(id, request, ct));
        }
        catch (Exception ex) when (ex is ArgumentException or InvalidOperationException)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpDelete("{id:long}")]
    [RequirePermission("Site_App_Ecommerce_Productos_Edit")]
    public async Task<IActionResult> Delete(long id, CancellationToken ct)
    {
        if (!_tenant.IsResolved) return BadRequest(new { error = "Falta tenant." });
        try
        {
            await _svc.DeleteAsync(id, ct);
            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }
}
