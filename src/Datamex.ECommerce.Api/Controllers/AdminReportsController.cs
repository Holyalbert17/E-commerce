using Datamex.ECommerce.Api.Authorization;
using Datamex.ECommerce.Application.Abstractions;
using Datamex.ECommerce.Application.Admin;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Datamex.ECommerce.Api.Controllers;

[ApiController]
[Route("api/admin/reports")]
[EnableRateLimiting("api")]
[Authorize]
public sealed class AdminReportsController : ControllerBase
{
    private readonly IAdminReportsService _reports;
    private readonly ITenantContext _tenant;

    public AdminReportsController(IAdminReportsService reports, ITenantContext tenant)
    {
        _reports = reports;
        _tenant = tenant;
    }

    [HttpGet("ventas")]
    [RequirePermission(
        "Site_App_Ecommerce",
        "Site_App_Ecommerce_Pedidos",
        "Site_App_Ecommerce_Pedidos_View",
        "Site_App_Ecommerce_CarritoCompras")]
    public async Task<ActionResult<VentasResumenDto>> Ventas(
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        CancellationToken ct)
    {
        if (!_tenant.IsResolved) return BadRequest(new { error = "Falta tenant." });
        var hasta = to?.Date ?? DateTime.UtcNow.Date;
        var desde = from?.Date ?? hasta.AddDays(-30);
        try
        {
            return Ok(await _reports.GetVentasResumenAsync(desde, hasta, ct));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }
}
