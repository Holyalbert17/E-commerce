using Datamex.ECommerce.Application.Abstractions;
using Datamex.ECommerce.Application.Account;
using Datamex.ECommerce.Application.Admin;
using Datamex.ECommerce.Application.Catalog;
using Datamex.ECommerce.Api.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Datamex.ECommerce.Api.Controllers;

[ApiController]
[Route("api/admin/clientes")]
[EnableRateLimiting("api")]
[Authorize]
public sealed class AdminClienteController : ControllerBase
{
    private readonly IAdminClienteService _svc;
    private readonly IMayoristaService _mayorista;
    private readonly ITenantContext _tenant;

    public AdminClienteController(IAdminClienteService svc, IMayoristaService mayorista, ITenantContext tenant)
    {
        _svc = svc;
        _mayorista = mayorista;
        _tenant = tenant;
    }

    [HttpGet]
    [RequirePermission("Site_App_Ecommerce_Clientes", "Site_App_Ecommerce_Clientes_ClientesCuentas")]
    public async Task<ActionResult<PagedResult<AdminClienteDto>>> Search(
        [FromQuery] string? search,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        if (!_tenant.IsResolved) return BadRequest(new { error = "Falta tenant." });
        return Ok(await _svc.SearchAsync(search, page, pageSize, ct));
    }

    [HttpGet("solicitudes-mayorista")]
    [RequirePermission("Site_App_Ecommerce_Clientes", "Site_App_Ecommerce_Clientes_ClientesCuentas")]
    public async Task<ActionResult<IReadOnlyList<AdminMayoristaSolicitudDto>>> SolicitudesMayorista(CancellationToken ct)
    {
        if (!_tenant.IsResolved) return BadRequest(new { error = "Falta tenant." });
        return Ok(await _svc.ListSolicitudesMayoristaAsync(ct));
    }

    [HttpPost("mayorista/{idUsuario:long}/aprobar")]
    [RequirePermission("Site_App_Ecommerce_Clientes_ClientesCuentas_AsigListaPrecio")]
    public async Task<IActionResult> AprobarMayorista(
        long idUsuario, [FromBody] ResolverMayoristaRequest? body, CancellationToken ct)
    {
        if (!_tenant.IsResolved) return BadRequest(new { error = "Falta tenant." });
        try
        {
            await _mayorista.ApproveAsync(idUsuario, body?.IdListaPrecio, ct);
            return Ok(new { ok = true });
        }
        catch (Exception ex) when (ex is InvalidOperationException or ArgumentException)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpPost("mayorista/{idUsuario:long}/rechazar")]
    [RequirePermission("Site_App_Ecommerce_Clientes_ClientesCuentas_Edit")]
    public async Task<IActionResult> RechazarMayorista(long idUsuario, CancellationToken ct)
    {
        if (!_tenant.IsResolved) return BadRequest(new { error = "Falta tenant." });
        try
        {
            await _mayorista.RejectAsync(idUsuario, ct);
            return Ok(new { ok = true });
        }
        catch (Exception ex) when (ex is InvalidOperationException or ArgumentException)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpGet("listas-vigentes")]
    [RequirePermission("Site_App_Ecommerce_Clientes_ClientesCuentas_AsigListaPrecio")]
    public async Task<ActionResult<IReadOnlyList<AdminListaPrecioDto>>> ListasVigentes(CancellationToken ct)
    {
        if (!_tenant.IsResolved) return BadRequest(new { error = "Falta tenant." });
        return Ok(await _svc.ListasVigentesAsync(ct));
    }

    [HttpGet("{id:long}")]
    [RequirePermission("Site_App_Ecommerce_Clientes_ClientesCuentas_View")]
    public async Task<ActionResult<AdminClienteDto>> Get(long id, CancellationToken ct)
    {
        if (!_tenant.IsResolved) return BadRequest(new { error = "Falta tenant." });
        var row = await _svc.GetAsync(id, ct);
        return row is null ? NotFound() : Ok(row);
    }

    [HttpPost]
    [RequirePermission("Site_App_Ecommerce_Clientes_ClientesCuentas_Add")]
    public async Task<ActionResult<AdminClienteDto>> Create(
        [FromBody] UpsertClienteRequest request, CancellationToken ct)
    {
        if (!_tenant.IsResolved) return BadRequest(new { error = "Falta tenant." });
        try { return Ok(await _svc.CreateAsync(request, ct)); }
        catch (Exception ex) when (ex is InvalidOperationException or ArgumentException)
        { return BadRequest(new { error = ex.Message }); }
    }

    [HttpPut("{id:long}")]
    [RequirePermission("Site_App_Ecommerce_Clientes_ClientesCuentas_Edit")]
    public async Task<ActionResult<AdminClienteDto>> Update(
        long id, [FromBody] UpsertClienteRequest request, CancellationToken ct)
    {
        if (!_tenant.IsResolved) return BadRequest(new { error = "Falta tenant." });
        try { return Ok(await _svc.UpdateAsync(id, request, ct)); }
        catch (Exception ex) when (ex is InvalidOperationException or ArgumentException)
        { return BadRequest(new { error = ex.Message }); }
    }

    [HttpDelete("{id:long}")]
    [RequirePermission("Site_App_Ecommerce_Clientes_ClientesCuentas_Del")]
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

    [HttpPut("{id:long}/lista-precio")]
    [RequirePermission("Site_App_Ecommerce_Clientes_ClientesCuentas_AsigListaPrecio")]
    public async Task<ActionResult<AdminClienteDto>> AsignarLista(
        long id, [FromBody] AsignarListaPrecioRequest request, CancellationToken ct)
    {
        if (!_tenant.IsResolved) return BadRequest(new { error = "Falta tenant." });
        try { return Ok(await _svc.AsignarListaPrecioAsync(id, request, ct)); }
        catch (Exception ex) when (ex is InvalidOperationException or ArgumentException)
        { return BadRequest(new { error = ex.Message }); }
    }

    [HttpGet("{id:long}/listas-precio")]
    [RequirePermission("Site_App_Ecommerce_Clientes_ClientesCuentas_View")]
    public async Task<ActionResult<IReadOnlyList<AdminListaPrecioCuentaDto>>> HistorialListas(
        long id, CancellationToken ct)
    {
        if (!_tenant.IsResolved) return BadRequest(new { error = "Falta tenant." });
        try { return Ok(await _svc.HistorialListasAsync(id, ct)); }
        catch (InvalidOperationException ex)
        { return BadRequest(new { error = ex.Message }); }
    }
}
