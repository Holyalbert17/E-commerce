using Datamex.ECommerce.Application.Media;
using Datamex.ECommerce.Application.Options;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Datamex.ECommerce.Infrastructure.Media;

/// <summary>
/// Guarda el archivo original sin recomprimir (máxima calidad).
/// </summary>
public sealed class LocalMediaStorage : IMediaStorage
{
    private readonly MediaOptions _options;
    private readonly ILogger<LocalMediaStorage> _logger;
    private readonly string _root;

    public LocalMediaStorage(
        IOptions<MediaOptions> options,
        IHostEnvironment env,
        ILogger<LocalMediaStorage> logger)
    {
        _options = options.Value;
        _logger = logger;
        _root = Path.IsPathRooted(_options.RootPath)
            ? _options.RootPath
            : Path.GetFullPath(Path.Combine(env.ContentRootPath, _options.RootPath));
        Directory.CreateDirectory(_root);
    }

    public async Task<StoredMediaFile> SaveProductImageAsync(
        long idEmpresa,
        long idArticulo,
        Stream content,
        string contentType,
        string originalFileName,
        CancellationToken ct = default)
    {
        var normalizedType = (contentType ?? "").Split(';')[0].Trim().ToLowerInvariant();
        if (!_options.AllowedContentTypes.Contains(normalizedType, StringComparer.OrdinalIgnoreCase))
            throw new ArgumentException(
                $"Formato no permitido ({contentType}). Usá JPEG, PNG, WebP, GIF o AVIF.");

        if (content.CanSeek && content.Length > _options.MaxUploadBytes)
            throw new ArgumentException(
                $"La imagen supera el máximo de {_options.MaxUploadBytes / (1024 * 1024)} MB.");

        var ext = ExtFromContentType(normalizedType, originalFileName);
        var relative = Path.Combine(
                "tenants",
                idEmpresa.ToString(),
                "products",
                idArticulo.ToString(),
                $"{Guid.NewGuid():N}{ext}")
            .Replace('\\', '/');

        var full = Path.Combine(_root, relative.Replace('/', Path.DirectorySeparatorChar));
        Directory.CreateDirectory(Path.GetDirectoryName(full)!);

        // Copia binaria intacta — sin re-encode.
        await using (var fs = new FileStream(full, FileMode.CreateNew, FileAccess.Write, FileShare.None, 81920,
                         FileOptions.Asynchronous | FileOptions.SequentialScan))
        {
            await content.CopyToAsync(fs, ct);
            if (fs.Length > _options.MaxUploadBytes)
            {
                fs.Close();
                File.Delete(full);
                throw new ArgumentException(
                    $"La imagen supera el máximo de {_options.MaxUploadBytes / (1024 * 1024)} MB.");
            }
        }

        var size = new FileInfo(full).Length;
        var publicUrl =
            $"{_options.PublicBaseUrl.TrimEnd('/')}{_options.RequestPath.TrimEnd('/')}/{relative}";

        _logger.LogInformation("Media saved {Path} ({Bytes} bytes, {Type})", relative, size, normalizedType);
        return new StoredMediaFile(relative, publicUrl, normalizedType, size, originalFileName);
    }

    public async Task<StoredMediaFile> SaveChatAttachmentAsync(
        long idEmpresa,
        long idConversacion,
        Stream content,
        string contentType,
        string originalFileName,
        CancellationToken ct = default)
    {
        var normalizedType = (contentType ?? "").Split(';')[0].Trim().ToLowerInvariant();
        if (!_options.AllowedContentTypes.Contains(normalizedType, StringComparer.OrdinalIgnoreCase))
            throw new ArgumentException(
                $"Formato no permitido ({contentType}). Usá JPEG, PNG, WebP, GIF o AVIF.");

        if (content.CanSeek && content.Length > _options.MaxUploadBytes)
            throw new ArgumentException(
                $"La imagen supera el máximo de {_options.MaxUploadBytes / (1024 * 1024)} MB.");

        var ext = ExtFromContentType(normalizedType, originalFileName);
        var relative = Path.Combine(
                "tenants",
                idEmpresa.ToString(),
                "chat",
                idConversacion.ToString(),
                $"{Guid.NewGuid():N}{ext}")
            .Replace('\\', '/');

        var full = Path.Combine(_root, relative.Replace('/', Path.DirectorySeparatorChar));
        Directory.CreateDirectory(Path.GetDirectoryName(full)!);

        await using (var fs = new FileStream(full, FileMode.CreateNew, FileAccess.Write, FileShare.None, 81920,
                         FileOptions.Asynchronous | FileOptions.SequentialScan))
        {
            await content.CopyToAsync(fs, ct);
            if (fs.Length > _options.MaxUploadBytes)
            {
                fs.Close();
                File.Delete(full);
                throw new ArgumentException(
                    $"La imagen supera el máximo de {_options.MaxUploadBytes / (1024 * 1024)} MB.");
            }
        }

        var size = new FileInfo(full).Length;
        var pathPart = $"{_options.RequestPath.TrimEnd('/')}/{relative}".Replace("//", "/");
        if (!pathPart.StartsWith('/')) pathPart = "/" + pathPart;
        var publicUrl = string.IsNullOrWhiteSpace(_options.PublicBaseUrl)
            ? pathPart
            : $"{_options.PublicBaseUrl.TrimEnd('/')}{pathPart}";

        _logger.LogInformation("Chat media saved {Path} ({Bytes} bytes)", relative, size);
        return new StoredMediaFile(relative, publicUrl, normalizedType, size, originalFileName);
    }

    public async Task<StoredMediaFile> SaveBrandingAssetAsync(
        long idEmpresa,
        string assetKind,
        Stream content,
        string contentType,
        string originalFileName,
        CancellationToken ct = default)
    {
        var kind = string.IsNullOrWhiteSpace(assetKind) ? "asset" : assetKind.Trim().ToLowerInvariant();
        var normalizedType = (contentType ?? "").Split(';')[0].Trim().ToLowerInvariant();
        if (!_options.AllowedContentTypes.Contains(normalizedType, StringComparer.OrdinalIgnoreCase)
            && !(kind == "favicon" && (normalizedType is "image/x-icon" or "image/vnd.microsoft.icon")))
            throw new ArgumentException(
                $"Formato no permitido ({contentType}). Usá JPEG, PNG, WebP, GIF, AVIF o ICO.");

        if (content.CanSeek && content.Length > _options.MaxUploadBytes)
            throw new ArgumentException(
                $"La imagen supera el máximo de {_options.MaxUploadBytes / (1024 * 1024)} MB.");

        var ext = ExtFromContentType(normalizedType, originalFileName);
        var relative = Path.Combine(
                "tenants",
                idEmpresa.ToString(),
                "branding",
                kind,
                $"{Guid.NewGuid():N}{ext}")
            .Replace('\\', '/');

        var full = Path.Combine(_root, relative.Replace('/', Path.DirectorySeparatorChar));
        Directory.CreateDirectory(Path.GetDirectoryName(full)!);

        await using (var fs = new FileStream(full, FileMode.CreateNew, FileAccess.Write, FileShare.None, 81920,
                         FileOptions.Asynchronous | FileOptions.SequentialScan))
        {
            await content.CopyToAsync(fs, ct);
            if (fs.Length > _options.MaxUploadBytes)
            {
                fs.Close();
                File.Delete(full);
                throw new ArgumentException(
                    $"La imagen supera el máximo de {_options.MaxUploadBytes / (1024 * 1024)} MB.");
            }
        }

        var size = new FileInfo(full).Length;
        var pathPart = $"{_options.RequestPath.TrimEnd('/')}/{relative}".Replace("//", "/");
        if (!pathPart.StartsWith('/')) pathPart = "/" + pathPart;
        var publicUrl = string.IsNullOrWhiteSpace(_options.PublicBaseUrl)
            ? pathPart
            : $"{_options.PublicBaseUrl.TrimEnd('/')}{pathPart}";

        _logger.LogInformation("Branding media saved {Path} ({Bytes} bytes)", relative, size);
        return new StoredMediaFile(relative, publicUrl, normalizedType, size, originalFileName);
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
                .Replace('/', Path.DirectorySeparatorChar);

            // Evitar path traversal
            var full = Path.GetFullPath(Path.Combine(_root, relative));
            if (!full.StartsWith(_root, StringComparison.OrdinalIgnoreCase))
                return Task.CompletedTask;

            if (File.Exists(full))
            {
                File.Delete(full);
                _logger.LogInformation("Media deleted {Path}", relative);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "No se pudo borrar media {Url}", publicOrRelativeUrl);
        }

        return Task.CompletedTask;
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
