using Datamex.ECommerce.Application.Abstractions;
using Datamex.ECommerce.Application.Reviews;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Datamex.ECommerce.Api.Controllers;

[ApiController]
[Route("api/reviews")]
[EnableRateLimiting("api")]
public sealed class ReviewsController : ControllerBase
{
    private readonly IReviewService _reviews;
    private readonly ITenantContext _tenant;

    public ReviewsController(IReviewService reviews, ITenantContext tenant)
    {
        _reviews = reviews;
        _tenant = tenant;
    }

    [HttpGet("product/{idArticulo:long}/summary")]
    public async Task<ActionResult<ResumenCalificacionDto>> Summary(long idArticulo, CancellationToken ct)
    {
        if (!_tenant.IsResolved) return BadRequest(new { error = "Falta tenant." });
        return Ok(await _reviews.GetResumenAsync(idArticulo, ct));
    }

    [HttpGet("product/{idArticulo:long}")]
    public async Task<ActionResult<IReadOnlyList<ResenaDto>>> ByProduct(long idArticulo, [FromQuery] int take = 50, CancellationToken ct = default)
    {
        if (!_tenant.IsResolved) return BadRequest(new { error = "Falta tenant." });
        return Ok(await _reviews.ListByProductAsync(idArticulo, take, ct));
    }

    [Authorize]
    [HttpGet("elegibles")]
    public async Task<ActionResult<IReadOnlyList<ProductoElegibleResenaDto>>> Elegibles(CancellationToken ct)
    {
        if (!_tenant.IsResolved) return BadRequest(new { error = "Falta tenant." });
        if (!_tenant.IDUsuarioEcommerce.HasValue) return Unauthorized();
        return Ok(await _reviews.ListElegiblesAsync(_tenant.IDUsuarioEcommerce.Value, ct));
    }

    [Authorize]
    [HttpPost]
    public async Task<ActionResult<ResenaDto>> Create([FromBody] CrearResenaRequest request, CancellationToken ct)
    {
        if (!_tenant.IsResolved) return BadRequest(new { error = "Falta tenant." });
        if (!_tenant.IDUsuarioEcommerce.HasValue) return Unauthorized();
        try
        {
            return Ok(await _reviews.CreateAsync(_tenant.IDUsuarioEcommerce.Value, request, ct));
        }
        catch (Exception ex) when (ex is InvalidOperationException or ArgumentException)
        {
            return BadRequest(new { error = ex.Message });
        }
    }
}
