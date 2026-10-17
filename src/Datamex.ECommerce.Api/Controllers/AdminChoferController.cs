using Datamex.ECommerce.Api.Authorization;
using Datamex.ECommerce.Application.Abstractions;
using Datamex.ECommerce.Application.Admin;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Datamex.ECommerce.Api.Controllers;

[ApiController]
[Route("api/admin")]
[EnableRateLimiting("api")]
[Authorize]
public sealed class AdminChoferController : ControllerBase
{
    private readonly IAdminChoferService _choferes;
    private readonly ITenantContext _tenant;

    public AdminChoferController(IAdminChoferService choferes, ITenantContext tenant)
    {
        _choferes = choferes;
        _tenant = tenant;
    }

    [HttpGet("choferes")]
    [RequirePermission("Site_App_Ecommerce_Pedidos")]
    public async Task<ActionResult<IReadOnlyList<AdminChoferDto>>> List(CancellationToken ct)
    {
        if (!_tenant.IsResolved) return BadRequest(new { error = "Falta tenant." });
        return Ok(await _choferes.ListAsync(ct));
    }

    [HttpPost("choferes")]
    [RequirePermission("Site_App_Ecommerce_Pedidos")]
    public async Task<ActionResult<AdminChoferDto>> Create([FromBody] UpsertChoferRequest body, CancellationToken ct)
    {
        if (!_tenant.IsResolved) return BadRequest(new { error = "Falta tenant." });
        try { return Ok(await _choferes.UpsertAsync(null, body, ct)); }
        catch (Exception ex) when (ex is ArgumentException or InvalidOperationException)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpPut("choferes/{id:long}")]
    [RequirePermission("Site_App_Ecommerce_Pedidos")]
    public async Task<ActionResult<AdminChoferDto>> Update(long id, [FromBody] UpsertChoferRequest body, CancellationToken ct)
    {
        if (!_tenant.IsResolved) return BadRequest(new { error = "Falta tenant." });
        try { return Ok(await _choferes.UpsertAsync(id, body, ct)); }
        catch (Exception ex) when (ex is ArgumentException or InvalidOperationException)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpDelete("choferes/{id:long}")]
    [RequirePermission("Site_App_Ecommerce_Pedidos")]
    public async Task<IActionResult> Delete(long id, CancellationToken ct)
    {
        if (!_tenant.IsResolved) return BadRequest(new { error = "Falta tenant." });
        try
        {
            await _choferes.DeleteAsync(id, ct);
            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpPost("pedidos/{idPedidoMovil:long}/asignar-chofer")]
    [RequirePermission("Site_App_Ecommerce_Pedidos")]
    public async Task<IActionResult> Asignar(long idPedidoMovil, [FromBody] AsignarChoferBody body, CancellationToken ct)
    {
        if (!_tenant.IsResolved) return BadRequest(new { error = "Falta tenant." });
        try
        {
            await _choferes.AsignarPedidoAsync(idPedidoMovil, body.IdChofer, ct);
            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    public record AsignarChoferBody(long? IdChofer);
}
