namespace Datamex.ECommerce.Application.Catalog;

public record CatalogPingResponse(
    long IDEmpresa,
    string? TenantSlug,
    string IntegrationMode,
    int ArticulosActivosEcommerce,
    DateTime ServerUtc);

public record PagedResult<T>(IReadOnlyList<T> Items, int Page, int PageSize, int TotalCount)
{
    public int TotalPages => PageSize <= 0 ? 0 : (int)Math.Ceiling(TotalCount / (double)PageSize);
}

public record ProductListItemDto(
    long Id,
    string? Sku,
    string Nombre,
    string NombreCorto,
    string? Marca,
    string EstadoEcommerce,
    string? ImagenPrincipalUrl,
    decimal? PrecioDesde,
    int EmbalajesCount,
    string Canal,
    string? ListaPrecio,
    decimal? StockLibre = null,
    bool StockOk = true,
    string? SlugSeo = null,
    decimal? PrecioLista = null,
    decimal? PorcentajeDescuento = null);

public record ProductEmbalajeDto(
    long Id,
    string? Descripcion,
    double CantidadPorEmbalaje,
    string? CodigoBarras,
    decimal? Precio,
    decimal? PrecioCredito,
    decimal CantidadMinimaMayorista,
    bool ParaFacturacion,
    string CodigoUnidadMedida = "UN",
    string? UnidadSimbolo = null,
    string? UnidadNombre = null,
    bool PermiteFraccion = false,
    decimal IncrementoVenta = 1,
    decimal CantidadMinimaVenta = 1,
    decimal? PrecioLista = null,
    decimal? PorcentajeDescuento = null);

public record ProductImagenDto(long Id, string Url, string? Titulo, int Orden, bool EsPrincipal);

public record ProductDetailDto(
    long Id,
    string? Sku,
    string Nombre,
    string NombreCorto,
    string DescripcionExtensa,
    string? CodReferenciaProveedor,
    string? Marca,
    string EstadoEcommerce,
    string? FichaTecnicaUrl,
    string? VideoUrl,
    string Canal,
    string? ListaPrecio,
    IReadOnlyList<ProductEmbalajeDto> Embalajes,
    IReadOnlyList<ProductImagenDto> Imagenes,
    decimal? StockLibre = null,
    bool StockOk = true,
    string? DepositoNombre = null,
    double PromedioCalificacion = 0,
    int TotalResenas = 0,
    string? SlugSeo = null);

public record CollectionDto(
    long Id,
    int Codigo,
    string Nombre,
    string? ImagenUrl,
    int ArticulosCount,
    int TipoGrupo = 0,
    string? UrlDestino = null,
    string? Subtitulo = null);

public record CatalogQuery(
    string? Search = null,
    string? Estado = null,
    long? GrupoId = null,
    long? MarcaId = null,
    string? Sort = null,
    string Canal = "B2C",
    int Page = 1,
    int PageSize = 12,
    long? LineaId = null,
    long? TipoId = null);

public record CatalogMarcaDto(long Id, string Nombre);

public record CatalogFacetDto(long Id, string Nombre);

public record MarketingSlotDto(
    long Id,
    int TipoGrupo,
    string TipoNombre,
    string Titulo,
    string? Subtitulo,
    string? ImagenUrl,
    string? UrlDestino,
    int Orden);

public interface ICatalogService
{
    Task<CatalogPingResponse> PingAsync(CancellationToken ct = default);
    Task<PagedResult<ProductListItemDto>> SearchProductsAsync(CatalogQuery query, CancellationToken ct = default);
    Task<ProductDetailDto?> GetProductAsync(long id, string canal = "B2C", CancellationToken ct = default);
    Task<ProductDetailDto?> GetProductBySkuAsync(string sku, string canal = "B2C", CancellationToken ct = default);
    Task<ProductDetailDto?> GetProductBySlugAsync(string slug, string canal = "B2C", CancellationToken ct = default);
    Task<IReadOnlyList<CollectionDto>> GetCollectionsAsync(int? tipoGrupo = null, CancellationToken ct = default);
    Task<IReadOnlyList<MarketingSlotDto>> GetMarketingSlotsAsync(CancellationToken ct = default);
    Task<IReadOnlyList<CatalogMarcaDto>> GetMarcasAsync(CancellationToken ct = default);
    Task<IReadOnlyList<CatalogFacetDto>> GetLineasAsync(CancellationToken ct = default);
    Task<IReadOnlyList<CatalogFacetDto>> GetTiposAsync(CancellationToken ct = default);
    Task<IReadOnlyList<ProductListItemDto>> GetMasVendidosAsync(int take = 10, string canal = "B2C", CancellationToken ct = default);
    /// <summary>Productos de vitrinas TipoGrupo=3 (promoción/oferta) vigentes.</summary>
    Task<IReadOnlyList<ProductListItemDto>> GetOfertasAsync(int take = 12, string canal = "B2C", CancellationToken ct = default);
}
