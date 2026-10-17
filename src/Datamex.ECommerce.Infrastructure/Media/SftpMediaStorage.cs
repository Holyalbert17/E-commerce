using Datamex.ECommerce.Application.Media;
using Datamex.ECommerce.Application.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Renci.SshNet;

namespace Datamex.ECommerce.Infrastructure.Media;

/// <summary>
/// Guarda medios vía SFTP en el mismo host que Postgres (ej. 10.99.99.205:/var/lib/ecommerce/media).
/// </summary>
public sealed class SftpMediaStorage : IMediaStorage
{
    private readonly MediaOptions _options;
    private readonly ILogger<SftpMediaStorage> _logger;

    public SftpMediaStorage(IOptions<MediaOptions> options, ILogger<SftpMediaStorage> logger)
    {
        _options = options.Value;
        _logger = logger;
        if (string.IsNullOrWhiteSpace(_options.Sftp.Host))
            throw new InvalidOperationException("Media:Sftp:Host es requerido cuando UseRemoteSftp=true.");
        if (string.IsNullOrWhiteSpace(_options.Sftp.Password) && string.IsNullOrWhiteSpace(_options.Sftp.Username))
            throw new InvalidOperationException("Media:Sftp credenciales incompletas.");
    }

    public Task<StoredMediaFile> SaveProductImageAsync(
        long idEmpresa,
        long idArticulo,
        Stream content,
        string contentType,
        string originalFileName,
        CancellationToken ct = default)
    {
        var relative = RelPath(idEmpresa, "products", idArticulo.ToString(), contentType, originalFileName);
        return SaveAsync(relative, content, contentType, originalFileName, ct);
    }

    public Task<StoredMediaFile> SaveChatAttachmentAsync(
        long idEmpresa,
        long idConversacion,
        Stream content,
        string contentType,
        string originalFileName,
        CancellationToken ct = default)
    {
        var relative = RelPath(idEmpresa, "chat", idConversacion.ToString(), contentType, originalFileName);
        return SaveAsync(relative, content, contentType, originalFileName, ct);
    }

    public Task<StoredMediaFile> SaveBrandingAssetAsync(
        long idEmpresa,
        string assetKind,
        Stream content,
        string contentType,
        string originalFileName,
        CancellationToken ct = default)
    {
        var kind = string.IsNullOrWhiteSpace(assetKind) ? "asset" : assetKind.Trim().ToLowerInvariant();
        var relative = RelPath(idEmpresa, "branding", kind, contentType, originalFileName);
        return SaveAsync(relative, content, contentType, originalFileName, ct);
    }

    public Task DeleteIfOwnedAsync(string? publicOrRelativeUrl, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(publicOrRelativeUrl))
            return Task.CompletedTask;

        try
        {
            var marker = $"{_options.RequestPath.TrimEnd('/')}/";
            var idx = publicOrRelativeUrl.IndexOf(marker, StringComparison.OrdinalIgnoreCase);
            if (idx < 0) return Task.CompletedTask;

            var relative = publicOrRelativeUrl[(idx + marker.Length)..]
                .Split('?', '#')[0]
                .TrimStart('/')
                .Replace('\\', '/');

            if (relative.Contains("..", StringComparison.Ordinal))
                return Task.CompletedTask;

            using var client = Connect();
            var remote = CombineRemote(relative);
            if (client.Exists(remote))
            {
                client.DeleteFile(remote);
                _logger.LogInformation("SFTP media deleted {Path}", relative);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "No se pudo borrar media SFTP {Url}", publicOrRelativeUrl);
        }

        return Task.CompletedTask;
    }

    private async Task<StoredMediaFile> SaveAsync(
        string relative,
        Stream content,
        string contentType,
        string originalFileName,
        CancellationToken ct)
    {
        var normalizedType = (contentType ?? "").Split(';')[0].Trim().ToLowerInvariant();
        ValidateType(normalizedType, relative.Contains("/branding/favicon/", StringComparison.Ordinal));

        await using var ms = new MemoryStream();
        await content.CopyToAsync(ms, ct);
        if (ms.Length > _options.MaxUploadBytes)
            throw new ArgumentException(
                $"La imagen supera el máximo de {_options.MaxUploadBytes / (1024 * 1024)} MB.");
        ms.Position = 0;

        using var client = Connect();
        EnsureRemoteDirs(client, relative);
        var remote = CombineRemote(relative);
        client.UploadFile(ms, remote, true);

        var pathPart = $"{_options.RequestPath.TrimEnd('/')}/{relative}".Replace("//", "/");
        if (!pathPart.StartsWith('/')) pathPart = "/" + pathPart;
        var publicUrl = string.IsNullOrWhiteSpace(_options.PublicBaseUrl)
            ? pathPart
            : $"{_options.PublicBaseUrl.TrimEnd('/')}{pathPart}";

        _logger.LogInformation("SFTP media saved {Path} ({Bytes} bytes, {Type})", relative, ms.Length, normalizedType);
        return new StoredMediaFile(relative, publicUrl, normalizedType, ms.Length, originalFileName);
    }

    private void ValidateType(string normalizedType, bool allowIco)
    {
        if (_options.AllowedContentTypes.Contains(normalizedType, StringComparer.OrdinalIgnoreCase))
            return;
        if (allowIco && normalizedType is "image/x-icon" or "image/vnd.microsoft.icon")
            return;
        throw new ArgumentException(
            $"Formato no permitido ({normalizedType}). Usá JPEG, PNG, WebP, GIF, AVIF o ICO.");
    }

    private static string RelPath(
        long idEmpresa,
        string folder,
        string id,
        string contentType,
        string originalFileName)
    {
        var normalizedType = (contentType ?? "").Split(';')[0].Trim().ToLowerInvariant();
        var ext = ExtFromContentType(normalizedType, originalFileName);
        return Path.Combine("tenants", idEmpresa.ToString(), folder, id, $"{Guid.NewGuid():N}{ext}")
            .Replace('\\', '/');
    }

    private SftpClient Connect()
    {
        var s = _options.Sftp;
        var client = new SftpClient(s.Host, s.Port, s.Username, s.Password);
        client.Connect();
        return client;
    }

    private string CombineRemote(string relative)
    {
        var root = _options.Sftp.RemoteRoot.TrimEnd('/').TrimEnd('\\');
        return $"{root}/media/{relative.TrimStart('/')}".Replace('\\', '/');
    }

    private void EnsureRemoteDirs(SftpClient client, string relative)
    {
        var parts = relative.Split('/', StringSplitOptions.RemoveEmptyEntries);
        var current = $"{_options.Sftp.RemoteRoot.TrimEnd('/')}/media";
        if (!client.Exists(current))
            client.CreateDirectory(current);

        // Create all parent dirs except the file name
        for (var i = 0; i < parts.Length - 1; i++)
        {
            current = $"{current}/{parts[i]}";
            if (!client.Exists(current))
                client.CreateDirectory(current);
        }
    }

    private static string ExtFromContentType(string contentType, string originalFileName)
    {
        return contentType switch
        {
            "image/jpeg" => ".jpg",
            "image/png" => ".png",
            "image/webp" => ".webp",
            "image/gif" => ".gif",
            "image/avif" => ".avif",
            "image/x-icon" or "image/vnd.microsoft.icon" => ".ico",
            _ => Path.GetExtension(originalFileName) is { Length: > 0 } e ? e.ToLowerInvariant() : ".bin"
        };
    }
}
