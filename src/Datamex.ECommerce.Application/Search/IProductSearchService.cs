namespace Datamex.ECommerce.Application.Search;

public record SearchDocDto(
    long Id,
    string? Sku,
    string Nombre,
    string? NombreCorto,
    string? Marca,
    string? ImagenPrincipalUrl,
    decimal? PrecioDesde,
    decimal? StockLibre,
    bool StockOk,
    string? SlugSeo,
    string? EstadoEcommerce);

public record SearchSuggestResult(
    string Query,
    IReadOnlyList<SearchDocDto> Items,
    string Source,
    int TookMs,
    bool FromCache);

public record SearchSnapshotDto(
    long IDEmpresa,
    string? TenantSlug,
    DateTime GeneratedUtc,
    int TotalDocs,
    IReadOnlyList<SearchDocDto> Docs,
    string Engine);

public record SearchPatchDto(long Id, decimal? StockLibre = null, bool? StockOk = null, decimal? PrecioDesde = null);

public interface IProductSearchService
{
    Task<SearchSuggestResult> SuggestAsync(string query, string canal = "B2C", int take = 8, CancellationToken ct = default);
    Task<SearchSuggestResult> SearchAsync(string query, string canal = "B2C", int page = 1, int pageSize = 24, CancellationToken ct = default);
    Task<SearchSnapshotDto> GetSnapshotAsync(string canal = "B2C", int? maxDocs = null, CancellationToken ct = default);
    Task RebuildIndexAsync(long? idEmpresa = null, CancellationToken ct = default);
    Task ApplyPatchAsync(long idEmpresa, SearchPatchDto patch, CancellationToken ct = default);
}
