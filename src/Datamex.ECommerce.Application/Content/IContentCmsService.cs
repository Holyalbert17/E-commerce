namespace Datamex.ECommerce.Application.Content;

public record ContenidoBloqueItemDto(
    long Id,
    string ItemType,
    string? Title,
    string? Subtitle,
    string? Description,
    string? ImageUrl,
    string? MobileImageUrl,
    string? LinkUrl,
    string? LinkTarget,
    string? BadgeText,
    string? PriceText,
    int DisplayOrder,
    bool Activo);

public record ContenidoBloqueDto(
    long Id,
    string PageCode,
    string? PageZone,
    string BlockType,
    string InternalName,
    string? Title,
    string? Subtitle,
    string? Description,
    int DisplayOrder,
    string ContentStatus,
    bool Activo,
    bool IsReadOnlyWhenIntegrated,
    DateTime? StartDate,
    DateTime? EndDate,
    string? SettingsJson,
    IReadOnlyList<ContenidoBloqueItemDto> Items);

public record UpsertContenidoBloqueRequest(
    string PageCode,
    string? PageZone,
    string BlockType,
    string InternalName,
    string? Title,
    string? Subtitle,
    string? Description,
    int DisplayOrder,
    string ContentStatus,
    bool Activo,
    bool IsReadOnlyWhenIntegrated = false,
    DateTime? StartDate = null,
    DateTime? EndDate = null,
    string? SettingsJson = null,
    IReadOnlyList<UpsertContenidoBloqueItemRequest>? Items = null);

public record UpsertContenidoBloqueItemRequest(
    long? Id,
    string ItemType,
    string? Title,
    string? Subtitle,
    string? Description,
    string? ImageUrl,
    string? MobileImageUrl,
    string? LinkUrl,
    string? LinkTarget,
    string? BadgeText,
    string? PriceText,
    int DisplayOrder,
    bool Activo = true);

public record NavegacionMenuItemDto(
    long Id,
    string ItemType,
    string Label,
    string? Url,
    string? Target,
    string? BadgeText,
    int DisplayOrder,
    long? IDGrupoArticulo,
    bool Activo);

public record NavegacionMenuDto(
    long Id,
    string Code,
    string Name,
    string MenuType,
    string? PageCode,
    string? PageZone,
    int DisplayOrder,
    string ContentStatus,
    bool Activo,
    IReadOnlyList<NavegacionMenuItemDto> Items);

public record UpsertNavegacionMenuRequest(
    string Code,
    string Name,
    string MenuType,
    string? PageCode,
    string? PageZone,
    int DisplayOrder,
    string ContentStatus,
    bool Activo,
    IReadOnlyList<UpsertNavegacionMenuItemRequest>? Items = null);

public record UpsertNavegacionMenuItemRequest(
    long? Id,
    string ItemType,
    string Label,
    string? Url,
    string? Target,
    string? BadgeText,
    int DisplayOrder,
    long? IDGrupoArticulo,
    bool Activo = true);

public record StorefrontHomeContentDto(
    IReadOnlyList<ContenidoBloqueDto> Blocks,
    IReadOnlyList<NavegacionMenuDto> Menus);

public interface IContentCmsService
{
    Task<StorefrontHomeContentDto> GetPublicHomeAsync(CancellationToken ct = default);
    Task<IReadOnlyList<ContenidoBloqueDto>> ListBlocksAsync(string? pageCode, CancellationToken ct = default);
    Task<ContenidoBloqueDto?> GetBlockAsync(long id, CancellationToken ct = default);
    Task<ContenidoBloqueDto> UpsertBlockAsync(long? id, UpsertContenidoBloqueRequest request, long? adminUserId, CancellationToken ct = default);
    Task DeleteBlockAsync(long id, CancellationToken ct = default);
    Task SeedDefaultsIfEmptyAsync(CancellationToken ct = default);
    /// <summary>Crea bloques home estándar que aún no existen. Devuelve cuántos agregó.</summary>
    Task<int> EnsureMissingHomeBlocksAsync(CancellationToken ct = default);

    Task<IReadOnlyList<NavegacionMenuDto>> ListMenusAsync(CancellationToken ct = default);
    Task<NavegacionMenuDto> UpsertMenuAsync(long? id, UpsertNavegacionMenuRequest request, CancellationToken ct = default);
    Task DeleteMenuAsync(long id, CancellationToken ct = default);
}
