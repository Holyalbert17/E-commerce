using Datamex.ECommerce.Application.Abstractions;
using Datamex.ECommerce.Application.Inventory;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Datamex.ECommerce.Api.Controllers;

[ApiController]
[Route("api/inventory")]
[EnableRateLimiting("api")]
public sealed class InventoryController : ControllerBase
{
    private readonly IInventoryService _inventory;
    private readonly ITenantContext _tenant;

    public InventoryController(IInventoryService inventory, ITenantContext tenant)
    {
        _inventory = inventory;
        _tenant = tenant;
    }

    [HttpGet("deposito")]
    public async Task<ActionResult<DepositoDto>> Deposito(CancellationToken ct)
    {
        if (!_tenant.IsResolved)
            return BadRequest(new { error = "Falta tenant." });

        var dep = await _inventory.GetEcommerceDepositoAsync(ct);
        return dep is null ? NotFound() : Ok(dep);
    }

    [HttpGet("stock/{articuloId:long}")]
    public async Task<ActionResult<StockItemDto>> Stock(long articuloId, CancellationToken ct)
    {
        if (!_tenant.IsResolved)
            return BadRequest(new { error = "Falta tenant." });

        var stock = await _inventory.GetStockAsync(articuloId, null, ct);
        return stock is null ? NotFound() : Ok(stock);
    }
}
