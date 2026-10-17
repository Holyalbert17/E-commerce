using Datamex.ECommerce.Application.Abstractions;
using Datamex.ECommerce.Application.Search;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Datamex.ECommerce.Api.Controllers;

/// <summary>
/// API de búsqueda rápida. Cadena: cache → índice local / OpenSearch → EF.
/// El storefront debe preferir RAM/IndexedDB antes de llamar aquí.
/// </summary>
[ApiController]
[Route("api/search")]
[EnableRateLimiting("api")]
public sealed class SearchController : ControllerBase
{
    private readonly IProductSearchService _search;
    private readonly ITenantContext _tenant;

    public SearchController(IProductSearchService search, ITenantContext tenant)
    {
        _search = search;
        _tenant = tenant;
    }

    /// <summary>Autocompletado / suggest (pocos hits).</summary>
    [HttpGet]
    public async Task<ActionResult<SearchSuggestResult>> Suggest(
        [FromQuery] string? q,
        [FromQuery] string canal = "B2C",
        [FromQuery] int take = 8,
        CancellationToken ct = default)
    {
        if (!_tenant.IsResolved) return BadRequest(new { error = "Falta tenant." });
        return Ok(await _search.SuggestAsync(q ?? "", canal, take, ct));
    }

    /// <summary>Resultados paginados para /buscar.</summary>
    [HttpGet("results")]
    public async Task<ActionResult<SearchSuggestResult>> Results(
        [FromQuery] string? q,
        [FromQuery] string canal = "B2C",
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 24,
        CancellationToken ct = default)
    {
        if (!_tenant.IsResolved) return BadRequest(new { error = "Falta tenant." });
        return Ok(await _search.SearchAsync(q ?? "", canal, page, pageSize, ct));
    }

    /// <summary>Snapshot compacto para prefetch IndexedDB / Web Worker.</summary>
    [HttpGet("snapshot")]
    public async Task<ActionResult<SearchSnapshotDto>> Snapshot(
        [FromQuery] string canal = "B2C",
        [FromQuery] int? max = null,
        CancellationToken ct = default)
    {
        if (!_tenant.IsResolved) return BadRequest(new { error = "Falta tenant." });
        return Ok(await _search.GetSnapshotAsync(canal, max, ct));
    }

    /// <summary>Patch incremental (stock/precio) sin re-descargar catálogo.</summary>
    [HttpPost("patch")]
    public async Task<IActionResult> Patch([FromBody] SearchPatchDto patch, CancellationToken ct)
    {
        if (!_tenant.IsResolved || !_tenant.IDEmpresa.HasValue)
            return BadRequest(new { error = "Falta tenant." });
        await _search.ApplyPatchAsync(_tenant.IDEmpresa.Value, patch, ct);
        return NoContent();
    }

    [HttpPost("rebuild")]
    public async Task<IActionResult> Rebuild(CancellationToken ct)
    {
        if (!_tenant.IsResolved) return BadRequest(new { error = "Falta tenant." });
        await _search.RebuildIndexAsync(null, ct);
        return Ok(new { ok = true });
    }
}
