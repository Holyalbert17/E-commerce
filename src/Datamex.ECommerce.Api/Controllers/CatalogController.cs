using Datamex.ECommerce.Application.Abstractions;
using Datamex.ECommerce.Application.Branding;
using Datamex.ECommerce.Application.Catalog;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Datamex.ECommerce.Api.Controllers;

[ApiController]
[Route("api/catalog")]
[EnableRateLimiting("api")]
public sealed class CatalogController : ControllerBase
{
    private readonly ICatalogService _catalog;
    private readonly IStorefrontBrandingService _branding;
    private readonly ITenantContext _tenant;

    public CatalogController(
        ICatalogService catalog,
        IStorefrontBrandingService branding,
        ITenantContext tenant)
    {
        _catalog = catalog;
        _branding = branding;
        _tenant = tenant;
    }

    [HttpGet("ping")]
    public async Task<ActionResult<CatalogPingResponse>> Ping(CancellationToken ct)
    {
        if (!_tenant.IsResolved)
            return BadRequest(new { error = "Falta tenant. Use header X-Tenant-ID, X-Tenant-Slug o ?tenant=casa-imperial" });

        return Ok(await _catalog.PingAsync(ct));
    }

    [HttpGet("branding")]
    public async Task<ActionResult<StorefrontBrandingDto>> Branding(CancellationToken ct)
    {
        if (!_tenant.IsResolved)
            return BadRequest(new { error = "Falta tenant." });

        var b = await _branding.GetPublicAsync(ct);
        return b is null ? NotFound() : Ok(b);
    }

    [HttpGet("products")]
    public async Task<ActionResult<PagedResult<ProductListItemDto>>> Products(
        [FromQuery] string? search,
        [FromQuery] string? estado,
        [FromQuery] long? grupoId,
        [FromQuery] long? marcaId,
        [FromQuery] long? lineaId,
        [FromQuery] long? tipoId,
        [FromQuery] string? sort,
        [FromQuery] string canal = "B2C",
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 12,
        CancellationToken ct = default)
    {
        if (!_tenant.IsResolved)
            return BadRequest(new { error = "Falta tenant." });

        var result = await _catalog.SearchProductsAsync(
            new CatalogQuery(search, estado, grupoId, marcaId, sort, canal, page, pageSize, lineaId, tipoId), ct);
        return Ok(result);
    }

    [HttpGet("mas-vendidos")]
    public async Task<ActionResult<IReadOnlyList<ProductListItemDto>>> MasVendidos(
        [FromQuery] int take = 10,
        [FromQuery] string canal = "B2C",
        CancellationToken ct = default)
    {
        if (!_tenant.IsResolved)
            return BadRequest(new { error = "Falta tenant." });

        return Ok(await _catalog.GetMasVendidosAsync(take, canal, ct));
    }

    [HttpGet("ofertas")]
    public async Task<ActionResult<IReadOnlyList<ProductListItemDto>>> Ofertas(
        [FromQuery] int take = 12,
        [FromQuery] string canal = "B2C",
        CancellationToken ct = default)
    {
        if (!_tenant.IsResolved)
            return BadRequest(new { error = "Falta tenant." });

        return Ok(await _catalog.GetOfertasAsync(take, canal, ct));
    }

    [HttpGet("products/{id:long}")]
    public async Task<ActionResult<ProductDetailDto>> ProductById(
        long id, [FromQuery] string canal = "B2C", CancellationToken ct = default)
    {
        if (!_tenant.IsResolved)
            return BadRequest(new { error = "Falta tenant." });

        var product = await _catalog.GetProductAsync(id, canal, ct);
        return product is null ? NotFound() : Ok(product);
    }

    [HttpGet("products/sku/{sku}")]
    public async Task<ActionResult<ProductDetailDto>> ProductBySku(
        string sku, [FromQuery] string canal = "B2C", CancellationToken ct = default)
    {
        if (!_tenant.IsResolved)
            return BadRequest(new { error = "Falta tenant." });

        var product = await _catalog.GetProductBySkuAsync(sku, canal, ct);
        return product is null ? NotFound() : Ok(product);
    }

    [HttpGet("products/slug/{slug}")]
    public async Task<ActionResult<ProductDetailDto>> ProductBySlug(
        string slug, [FromQuery] string canal = "B2C", CancellationToken ct = default)
    {
        if (!_tenant.IsResolved)
            return BadRequest(new { error = "Falta tenant." });

        var product = await _catalog.GetProductBySlugAsync(slug, canal, ct);
        return product is null ? NotFound() : Ok(product);
    }

    [HttpGet("collections")]
    public async Task<ActionResult<IReadOnlyList<CollectionDto>>> Collections(
        [FromQuery] int? tipoGrupo, CancellationToken ct)
    {
        if (!_tenant.IsResolved)
            return BadRequest(new { error = "Falta tenant." });

        return Ok(await _catalog.GetCollectionsAsync(tipoGrupo, ct));
    }

    [HttpGet("marketing")]
    public async Task<ActionResult<IReadOnlyList<MarketingSlotDto>>> Marketing(CancellationToken ct)
    {
        if (!_tenant.IsResolved)
            return BadRequest(new { error = "Falta tenant." });

        return Ok(await _catalog.GetMarketingSlotsAsync(ct));
    }

    [HttpGet("marcas")]
    public async Task<ActionResult<IReadOnlyList<CatalogMarcaDto>>> Marcas(CancellationToken ct)
    {
        if (!_tenant.IsResolved)
            return BadRequest(new { error = "Falta tenant." });

        return Ok(await _catalog.GetMarcasAsync(ct));
    }

    [HttpGet("lineas")]
    public async Task<ActionResult<IReadOnlyList<CatalogFacetDto>>> Lineas(CancellationToken ct)
    {
        if (!_tenant.IsResolved)
            return BadRequest(new { error = "Falta tenant." });

        return Ok(await _catalog.GetLineasAsync(ct));
    }

    [HttpGet("tipos")]
    public async Task<ActionResult<IReadOnlyList<CatalogFacetDto>>> Tipos(CancellationToken ct)
    {
        if (!_tenant.IsResolved)
            return BadRequest(new { error = "Falta tenant." });

        return Ok(await _catalog.GetTiposAsync(ct));
    }
}
