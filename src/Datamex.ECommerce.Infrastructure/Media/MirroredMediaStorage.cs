using Datamex.ECommerce.Application.Media;
using Microsoft.Extensions.Logging;

namespace Datamex.ECommerce.Infrastructure.Media;

/// <summary>
/// Guarda en disco local (sirve la API) y replica por SFTP al host de Postgres (205).
/// </summary>
public sealed class MirroredMediaStorage : IMediaStorage
{
    private readonly LocalMediaStorage _local;
    private readonly SftpMediaStorage _remote;
    private readonly ILogger<MirroredMediaStorage> _logger;

    public MirroredMediaStorage(
        LocalMediaStorage local,
        SftpMediaStorage remote,
        ILogger<MirroredMediaStorage> logger)
    {
        _local = local;
        _remote = remote;
        _logger = logger;
    }

    public async Task<StoredMediaFile> SaveProductImageAsync(
        long idEmpresa, long idArticulo, Stream content, string contentType, string originalFileName,
        CancellationToken ct = default)
    {
        await using var ms = await BufferAsync(content, ct);
        var local = await _local.SaveProductImageAsync(idEmpresa, idArticulo, Clone(ms), contentType, originalFileName, ct);
        await TryRemoteAsync(() => _remote.SaveProductImageAsync(idEmpresa, idArticulo, Clone(ms), contentType, originalFileName, ct));
        return local;
    }

    public async Task<StoredMediaFile> SaveChatAttachmentAsync(
        long idEmpresa, long idConversacion, Stream content, string contentType, string originalFileName,
        CancellationToken ct = default)
    {
        await using var ms = await BufferAsync(content, ct);
        var local = await _local.SaveChatAttachmentAsync(idEmpresa, idConversacion, Clone(ms), contentType, originalFileName, ct);
        await TryRemoteAsync(() => _remote.SaveChatAttachmentAsync(idEmpresa, idConversacion, Clone(ms), contentType, originalFileName, ct));
        return local;
    }

    public async Task<StoredMediaFile> SaveBrandingAssetAsync(
        long idEmpresa, string assetKind, Stream content, string contentType, string originalFileName,
        CancellationToken ct = default)
    {
        await using var ms = await BufferAsync(content, ct);
        var local = await _local.SaveBrandingAssetAsync(idEmpresa, assetKind, Clone(ms), contentType, originalFileName, ct);
        await TryRemoteAsync(() => _remote.SaveBrandingAssetAsync(idEmpresa, assetKind, Clone(ms), contentType, originalFileName, ct));
        return local;
    }

    public async Task DeleteIfOwnedAsync(string? publicOrRelativeUrl, CancellationToken ct = default)
    {
        await _local.DeleteIfOwnedAsync(publicOrRelativeUrl, ct);
        try
        {
            await _remote.DeleteIfOwnedAsync(publicOrRelativeUrl, ct);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "No se pudo borrar réplica SFTP {Url}", publicOrRelativeUrl);
        }
    }

    private async Task TryRemoteAsync(Func<Task<StoredMediaFile>> action)
    {
        try
        {
            await action();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Réplica SFTP de media falló (archivo local OK)");
        }
    }

    private static async Task<MemoryStream> BufferAsync(Stream content, CancellationToken ct)
    {
        var ms = new MemoryStream();
        await content.CopyToAsync(ms, ct);
        ms.Position = 0;
        return ms;
    }

    private static MemoryStream Clone(MemoryStream ms)
    {
        var copy = new MemoryStream(ms.ToArray());
        copy.Position = 0;
        return copy;
    }
}
