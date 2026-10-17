using Datamex.ECommerce.Application.Abstractions;
using Datamex.ECommerce.Application.Pricing;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Datamex.ECommerce.Api.Controllers;

[ApiController]
[Route("api/pricing")]
[EnableRateLimiting("api")]
public sealed class PricingController : ControllerBase
{
    private readonly IPricingService _pricing;
    private readonly ITenantContext _tenant;

    public PricingController(IPricingService pricing, ITenantContext tenant)
    {
        _pricing = pricing;
        _tenant = tenant;
    }

    [HttpGet("lista")]
    public async Task<ActionResult<ListaPrecioInfo>> ResolveLista(
        [FromQuery] string canal = "B2C", CancellationToken ct = default)
    {
        if (!_tenant.IsResolved)
            return BadRequest(new { error = "Falta tenant." });

        var lista = await _pricing.ResolveListaAsync(canal, null, ct);
        return lista is null ? NotFound(new { error = "No hay lista ecom vigente para el canal." }) : Ok(lista);
    }
}
