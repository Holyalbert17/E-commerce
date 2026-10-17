namespace Datamex.ECommerce.Application.Admin;

/// <summary>
/// Promociones/vitrinas = GrupoArticuloEcommerce del ERP (Site_App_Ecommerce_GrupoArticulo).
/// </summary>
public record AdminPromotionListItemDto(
    long Id,
    int CodGrupoArticulo,
    string Nombre,
    DateTime FechaInicioVigencia,
    DateTime FechaFinVigencia,
    int TipoGrupo,
    int Estado,
    int Orden,
    string? Imagen,
    bool Activo,
    int ArticulosCount,
    string? UrlDestino = null,
    string? Subtitulo = null);

public record AdminPromotionDetailDto(
    long Id,
    int CodGrupoArticulo,
    string Nombre,
    DateTime FechaInicioVigencia,
    DateTime FechaFinVigencia,
    int TipoGrupo,
    int Estado,
    int Orden,
    string? Imagen,
    bool Activo,
    IReadOnlyList<AdminPromotionArticleDto> Articulos,
    string? UrlDestino = null,
    string? Subtitulo = null);

public record AdminPromotionArticleDto(long IdArticulo, string? Sku, string Nombre);

public record UpsertPromotionRequest(
    string Nombre,
    DateTime FechaInicioVigencia,
    DateTime FechaFinVigencia,
    int TipoGrupo = 0,
    int Estado = 1,
    int Orden = 1,
    string? Imagen = null,
    bool Activo = true,
    IReadOnlyList<long>? ArticuloIds = null,
    string? UrlDestino = null,
    string? Subtitulo = null);

public record AdminTarifaDeliveryDto(
    long Id,
    long IDCiudad,
    long IDBarrio,
    string NombreCiudad,
    string NombreBarrio,
    decimal ImporteDelivery,
    DateTime FechaInicioVigencia,
    DateTime FechaFinVigencia,
    int Estado,
    bool Activo);

public record CreateTarifaDeliveryRequest(
    long IDCiudad,
    long IDBarrio,
    decimal ImporteDelivery,
    long? IDAgencia = null);

public record UpdateTarifaDeliveryRequest(
    decimal? ImporteDelivery = null,
    DateTime? FechaInicioVigencia = null,
    DateTime? FechaFinVigencia = null,
    int? Estado = null,
    bool? Activo = null);

public record TarifaIdsRequest(IReadOnlyList<long> Ids);

public record BulkEstadoResult(int UpdatedCount);

public record PromoImageUploadResult(string Url, long? IdPromocion = null);

public interface IAdminPromotionsService
{
    Task<IReadOnlyList<AdminPromotionListItemDto>> ListAsync(CancellationToken ct = default);
    Task<AdminPromotionDetailDto?> GetAsync(long id, CancellationToken ct = default);
    Task<AdminPromotionDetailDto> CreateAsync(UpsertPromotionRequest request, CancellationToken ct = default);
    Task<AdminPromotionDetailDto> UpdateAsync(long id, UpsertPromotionRequest request, CancellationToken ct = default);
    Task DeleteAsync(long id, CancellationToken ct = default);
    /// <summary>Sube flyer/publicidad. Si idPromocion tiene valor, actualiza Imagen del grupo.</summary>
    Task<PromoImageUploadResult> UploadImageAsync(
        Stream content,
        string contentType,
        string originalFileName,
        long? idPromocion = null,
        CancellationToken ct = default);
}

public interface IAdminShippingAdminService
{
    Task<IReadOnlyList<AdminTarifaDeliveryDto>> ListTarifasAsync(CancellationToken ct = default);
    Task<AdminTarifaDeliveryDto> CreateTarifaAsync(CreateTarifaDeliveryRequest request, CancellationToken ct = default);
    Task<AdminTarifaDeliveryDto> UpdateTarifaAsync(long id, UpdateTarifaDeliveryRequest request, CancellationToken ct = default);
    Task<BulkEstadoResult> AutorizarTarifasAsync(IReadOnlyList<long> ids, CancellationToken ct = default);
    Task<BulkEstadoResult> AnularTarifasAsync(IReadOnlyList<long> ids, CancellationToken ct = default);
    Task<BulkEstadoResult> CancelarTarifasAsync(IReadOnlyList<long> ids, CancellationToken ct = default);
}
