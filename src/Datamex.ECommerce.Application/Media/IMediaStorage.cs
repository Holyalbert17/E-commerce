namespace Datamex.ECommerce.Application.Media;

public record StoredMediaFile(
    string RelativePath,
    string PublicUrl,
    string ContentType,
    long SizeBytes,
    string OriginalFileName);

public interface IMediaStorage
{
    Task<StoredMediaFile> SaveProductImageAsync(
        long idEmpresa,
        long idArticulo,
        Stream content,
        string contentType,
        string originalFileName,
        CancellationToken ct = default);

    Task<StoredMediaFile> SaveChatAttachmentAsync(
        long idEmpresa,
        long idConversacion,
        Stream content,
        string contentType,
        string originalFileName,
        CancellationToken ct = default);

    Task<StoredMediaFile> SaveBrandingAssetAsync(
        long idEmpresa,
        string assetKind,
        Stream content,
        string contentType,
        string originalFileName,
        CancellationToken ct = default);

    Task DeleteIfOwnedAsync(string? publicOrRelativeUrl, CancellationToken ct = default);
}
