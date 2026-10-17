using Datamex.ECommerce.Application.Provisioning;
using Datamex.ECommerce.Api.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.RateLimiting;

namespace Datamex.ECommerce.Api.Controllers;

/// <summary>
/// Alta de empresas/tiendas. NO requiere X-Tenant-Slug (son endpoints de plataforma).
/// Manual = Standalone; FromErp = Integrated. Ambas usan las mismas tablas ERP.
/// </summary>
[ApiController]
[Route("api/admin/tenants")]
[EnableRateLimiting("api")]
[Authorize]
public sealed class TenantsController : ControllerBase
{
    private readonly ITenantProvisioningService _provisioning;

    public TenantsController(ITenantProvisioningService provisioning) => _provisioning = provisioning;

    [HttpGet]
    [RequirePermission("manager:tenants")]
    public async Task<ActionResult<IReadOnlyList<TenantSummaryDto>>> List(CancellationToken ct) =>
        Ok(await _provisioning.ListAsync(ct));

    [HttpGet("{idEmpresa:long}")]
    [RequirePermission("manager:tenants")]
    public async Task<ActionResult<TenantSummaryDto>> Get(long idEmpresa, CancellationToken ct)
    {
        var t = await _provisioning.GetAsync(idEmpresa, ct);
        return t is null ? NotFound() : Ok(t);
    }

    /// <summary>Alta manual sin ERP (Standalone). Genera IDEmpresa local + defaults.</summary>
    [HttpPost("manual")]
    [RequirePermission("manager:tenants")]
    public async Task<ActionResult<ProvisionResultDto>> Manual(
        [FromBody] ManualTenantRequest request, CancellationToken ct)
    {
        try
        {
            var result = await _provisioning.ProvisionManualAsync(request, ct);
            return CreatedAtAction(nameof(Get), new { idEmpresa = result.IDEmpresa }, result);
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

    /// <summary>
    /// Provisiona/actualiza con IDEmpresa del ERP (Integrated).
    /// No crea tablas distintas: misma empresa + empresaconfiguracionecommerce + maestros.
    /// </summary>
    [HttpPost("from-erp")]
    [RequirePermission("manager:tenants")]
    public async Task<ActionResult<ProvisionResultDto>> FromErp(
        [FromBody] ErpTenantRequest request, CancellationToken ct)
    {
        try
        {
            var result = await _provisioning.ProvisionFromErpAsync(request, ct);
            return Ok(result);
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
}
