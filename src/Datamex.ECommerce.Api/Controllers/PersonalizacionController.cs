using Datamex.ECommerce.Api.Authorization;
using Datamex.ECommerce.Application.Abstractions;
using Datamex.ECommerce.Application.Personalizacion;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Datamex.ECommerce.Api.Controllers;

[ApiController]
[Route("api/admin/personalizacion")]
[EnableRateLimiting("api")]
[Authorize]
public sealed class AdminPersonalizacionController : ControllerBase
{
    private readonly IPersonalizacionService _svc;
    private readonly ITenantContext _tenant;

    public AdminPersonalizacionController(IPersonalizacionService svc, ITenantContext tenant)
    {
        _svc = svc;
        _tenant = tenant;
    }

    [HttpGet("schemas")]
    [RequirePermission("Site_App_Ecommerce_Personalizacion")]
    public ActionResult<object> Schemas()
    {
        if (!_tenant.IsResolved) return BadRequest(new { error = "Falta tenant." });
        return Ok(_svc.GetDefaultSchemas());
    }

    [HttpGet("{clave}")]
    [RequirePermission("Site_App_Ecommerce_Personalizacion")]
    public async Task<ActionResult<PersonalizacionItemDto>> Get(string clave, CancellationToken ct)
    {
        if (!_tenant.IsResolved) return BadRequest(new { error = "Falta tenant." });
        try
        {
            var item = await _svc.GetActivaAsync(clave, ct);
            return item is null ? NotFound() : Ok(item);
        }
        catch (ArgumentException ex) { return BadRequest(new { error = ex.Message }); }
    }

    [HttpGet("{clave}/versiones")]
    [RequirePermission("Site_App_Ecommerce_Personalizacion")]
    public async Task<ActionResult<IReadOnlyList<PersonalizacionItemDto>>> Versiones(string clave, CancellationToken ct)
    {
        if (!_tenant.IsResolved) return BadRequest(new { error = "Falta tenant." });
        try { return Ok(await _svc.ListVersionesAsync(clave, ct)); }
        catch (ArgumentException ex) { return BadRequest(new { error = ex.Message }); }
    }

    [HttpPut("{clave}")]
    [RequirePermission("Site_App_Ecommerce_Personalizacion")]
    public async Task<ActionResult<PersonalizacionItemDto>> Put(
        string clave, [FromBody] UpsertPersonalizacionRequest body, CancellationToken ct)
    {
        if (!_tenant.IsResolved) return BadRequest(new { error = "Falta tenant." });
        try
        {
            var user = User.Identity?.Name ?? "admin";
            return Ok(await _svc.UpsertAsync(clave, body, user, ct));
        }
        catch (Exception ex) when (ex is ArgumentException or InvalidOperationException)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpPost("{clave}/programar")]
    [RequirePermission("Site_App_Ecommerce_Personalizacion")]
    public async Task<ActionResult<PersonalizacionItemDto>> Programar(
        string clave, [FromBody] ProgramarPersonalizacionRequest body, CancellationToken ct)
    {
        if (!_tenant.IsResolved) return BadRequest(new { error = "Falta tenant." });
        try
        {
            var user = User.Identity?.Name ?? "admin";
            return Ok(await _svc.ProgramarAsync(clave, body, user, ct));
        }
        catch (Exception ex) when (ex is ArgumentException or InvalidOperationException)
        {
            return BadRequest(new { error = ex.Message });
        }
    }
}

[ApiController]
[Route("api/storefront/personalizacion")]
[EnableRateLimiting("api")]
[AllowAnonymous]
public sealed class StorefrontPersonalizacionController : ControllerBase
{
    private readonly IPersonalizacionService _svc;
    private readonly ITenantContext _tenant;

    public StorefrontPersonalizacionController(IPersonalizacionService svc, ITenantContext tenant)
    {
        _svc = svc;
        _tenant = tenant;
    }

    [HttpGet]
    public async Task<ActionResult<StorefrontPersonalizacionDto>> Get(CancellationToken ct)
    {
        if (!_tenant.IsResolved) return BadRequest(new { error = "Falta tenant." });
        return Ok(await _svc.GetPublicasAsync(ct));
    }
}
