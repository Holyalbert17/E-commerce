using Datamex.ECommerce.Application.Abstractions;
using Datamex.ECommerce.Application.Shipping;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Datamex.ECommerce.Api.Controllers;

[ApiController]
[Route("api/shipping")]
[EnableRateLimiting("api")]
public sealed class ShippingController : ControllerBase
{
    private readonly IShippingService _shipping;
    private readonly ITenantContext _tenant;

    public ShippingController(IShippingService shipping, ITenantContext tenant)
    {
        _shipping = shipping;
        _tenant = tenant;
    }

    [HttpGet("tarifas")]
    public async Task<ActionResult<IReadOnlyList<TarifaDeliveryDto>>> Tarifas(CancellationToken ct)
    {
        if (!_tenant.IsResolved)
            return BadRequest(new { error = "Falta tenant." });

        return Ok(await _shipping.GetActiveTarifasAsync(ct));
    }

    [HttpPost("apply")]
    public async Task<IActionResult> Apply([FromBody] ApplyShippingRequest request, CancellationToken ct)
    {
        if (!_tenant.IsResolved)
            return BadRequest(new { error = "Falta tenant." });

        var session = RequireSession();
        if (session is null)
            return BadRequest(new { error = "Falta X-Cart-Session." });

        try
        {
            await _shipping.ApplyToCartAsync(session, request, ct);
            return Ok(new { ok = true });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpDelete]
    public async Task<IActionResult> Clear(CancellationToken ct)
    {
        if (!_tenant.IsResolved)
            return BadRequest(new { error = "Falta tenant." });

        var session = RequireSession();
        if (session is null)
            return BadRequest(new { error = "Falta X-Cart-Session." });

        await _shipping.ClearFromCartAsync(session, ct);
        return NoContent();
    }

    private string? RequireSession()
    {
        if (Request.Headers.TryGetValue("X-Cart-Session", out var values))
        {
            var v = values.FirstOrDefault();
            if (!string.IsNullOrWhiteSpace(v))
                return v.Trim();
        }

        return null;
    }
}
