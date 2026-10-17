using Datamex.ECommerce.Application.Abstractions;
using Datamex.ECommerce.Application.Contacto;
using Datamex.ECommerce.Api.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Datamex.ECommerce.Api.Controllers;

[ApiController]
[Route("api/contacto")]
[EnableRateLimiting("contacto")]
public sealed class ContactoController : ControllerBase
{
    private readonly IContactoService _contacto;
    private readonly ITenantContext _tenant;

    public ContactoController(IContactoService contacto, ITenantContext tenant)
    {
        _contacto = contacto;
        _tenant = tenant;
    }

    [HttpPost]
    [AllowAnonymous]
    public async Task<ActionResult<ContactoResultDto>> Enviar(
        [FromBody] ContactoRequest request, CancellationToken ct)
    {
        if (!_tenant.IsResolved)
            return BadRequest(new { error = "Falta tenant." });
        try
        {
            return Ok(await _contacto.EnviarAsync(request, ct));
        }
        catch (Exception ex) when (ex is ArgumentException or InvalidOperationException)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpGet]
    [Authorize]
    [RequirePermission("Site_App_Ecommerce")]
    [EnableRateLimiting("api")]
    public async Task<ActionResult<IReadOnlyList<ContactoMensajeDto>>> List(
        [FromQuery] bool? leido, CancellationToken ct)
    {
        if (!_tenant.IsResolved)
            return BadRequest(new { error = "Falta tenant." });
        return Ok(await _contacto.ListAdminAsync(leido, ct));
    }

    [HttpPost("{id:long}/leido")]
    [Authorize]
    [RequirePermission("Site_App_Ecommerce")]
    [EnableRateLimiting("api")]
    public async Task<IActionResult> MarcarLeido(long id, [FromQuery] bool leido = true, CancellationToken ct = default)
    {
        if (!_tenant.IsResolved)
            return BadRequest(new { error = "Falta tenant." });
        try
        {
            await _contacto.MarcarLeidoAsync(id, leido, ct);
            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }
}
