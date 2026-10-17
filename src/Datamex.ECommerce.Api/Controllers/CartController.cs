using Datamex.ECommerce.Application.Abstractions;
using Datamex.ECommerce.Application.Cart;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Datamex.ECommerce.Api.Controllers;

[ApiController]
[Route("api/cart")]
[EnableRateLimiting("api")]
public sealed class CartController : ControllerBase
{
    private readonly ICartService _cart;
    private readonly ITenantContext _tenant;

    public CartController(ICartService cart, ITenantContext tenant)
    {
        _cart = cart;
        _tenant = tenant;
    }

    [HttpGet]
    public async Task<ActionResult<CartDto>> Get([FromQuery] string canal = "B2C", CancellationToken ct = default)
    {
        if (!_tenant.IsResolved)
            return BadRequest(new { error = "Falta tenant." });

        var session = RequireSession();
        if (session is null)
            return BadRequest(new { error = "Falta X-Cart-Session." });

        return Ok(await _cart.GetOrCreateAsync(session, canal, ct));
    }

    [HttpPost("items")]
    public async Task<ActionResult<CartDto>> AddItem([FromBody] AddCartItemRequest request, CancellationToken ct = default)
    {
        if (!_tenant.IsResolved)
            return BadRequest(new { error = "Falta tenant." });

        var session = RequireSession();
        if (session is null)
            return BadRequest(new { error = "Falta X-Cart-Session." });

        try
        {
            return Ok(await _cart.AddItemAsync(session, request, ct));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpPut("items/{detalleId:long}")]
    public async Task<ActionResult<CartDto>> UpdateItem(
        long detalleId,
        [FromBody] UpdateCartItemRequest request,
        [FromQuery] string canal = "B2C",
        CancellationToken ct = default)
    {
        if (!_tenant.IsResolved)
            return BadRequest(new { error = "Falta tenant." });

        var session = RequireSession();
        if (session is null)
            return BadRequest(new { error = "Falta X-Cart-Session." });

        try
        {
            return Ok(await _cart.UpdateItemAsync(session, detalleId, request.Cantidad, canal, ct));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpDelete("items/{detalleId:long}")]
    public async Task<ActionResult<CartDto>> RemoveItem(
        long detalleId, [FromQuery] string canal = "B2C", CancellationToken ct = default)
    {
        if (!_tenant.IsResolved)
            return BadRequest(new { error = "Falta tenant." });

        var session = RequireSession();
        if (session is null)
            return BadRequest(new { error = "Falta X-Cart-Session." });

        try
        {
            return Ok(await _cart.RemoveItemAsync(session, detalleId, canal, ct));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpDelete]
    public async Task<IActionResult> Clear(CancellationToken ct = default)
    {
        if (!_tenant.IsResolved)
            return BadRequest(new { error = "Falta tenant." });

        var session = RequireSession();
        if (session is null)
            return BadRequest(new { error = "Falta X-Cart-Session." });

        await _cart.ClearAsync(session, ct);
        return NoContent();
    }

    /// <summary>Paridad ERP SetSucursal — pickup + depósito de stock.</summary>
    [HttpPut("pickup")]
    public async Task<ActionResult<CartDto>> SetPickup([FromBody] SetPickupRequest request, CancellationToken ct = default)
    {
        if (!_tenant.IsResolved)
            return BadRequest(new { error = "Falta tenant." });
        var session = RequireSession();
        if (session is null)
            return BadRequest(new { error = "Falta X-Cart-Session." });

        try
        {
            return Ok(await _cart.SetPickupAsync(session, request.IDAgencia, ct));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>Paridad ERP VolverAPedir.</summary>
    [HttpPost("reorder/{nroPedido:long}")]
    public async Task<ActionResult<CartDto>> Reorder(
        long nroPedido, [FromQuery] string canal = "B2C", CancellationToken ct = default)
    {
        if (!_tenant.IsResolved)
            return BadRequest(new { error = "Falta tenant." });
        var session = RequireSession();
        if (session is null)
            return BadRequest(new { error = "Falta X-Cart-Session." });

        try
        {
            return Ok(await _cart.ReorderAsync(session, nroPedido, canal, ct));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
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
