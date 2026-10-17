using Datamex.ECommerce.Application.Abstractions;
using Datamex.ECommerce.Application.Checkout;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Datamex.ECommerce.Api.Controllers;

[ApiController]
[Route("api/checkout")]
[EnableRateLimiting("api")]
public sealed class CheckoutController : ControllerBase
{
    private readonly ICheckoutService _checkout;
    private readonly ITenantContext _tenant;

    public CheckoutController(ICheckoutService checkout, ITenantContext tenant)
    {
        _checkout = checkout;
        _tenant = tenant;
    }

    [HttpPost]
    public async Task<ActionResult<CheckoutResultDto>> Checkout(
        [FromBody] CheckoutRequest request, CancellationToken ct)
    {
        if (!_tenant.IsResolved)
            return BadRequest(new { error = "Falta tenant." });

        var session = GetSession();
        if (session is null)
            return BadRequest(new { error = "Falta X-Cart-Session." });

        try
        {
            return Ok(await _checkout.CheckoutAsync(session, request, ct));
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

    [HttpGet("orders/{nroPedido:long}")]
    public async Task<ActionResult<CheckoutResultDto>> GetOrder(long nroPedido, CancellationToken ct)
    {
        if (!_tenant.IsResolved)
            return BadRequest(new { error = "Falta tenant." });

        var session = GetSession();
        if (session is null)
            return BadRequest(new { error = "Falta X-Cart-Session." });

        var order = await _checkout.GetOrderAsync(nroPedido, session, ct);
        return order is null ? NotFound() : Ok(order);
    }

    private string? GetSession()
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
