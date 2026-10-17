using Datamex.ECommerce.Application.Abstractions;
using Datamex.ECommerce.Application.Options;
using Datamex.ECommerce.Application.Sync;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Npgsql;

namespace Datamex.ECommerce.Api.Controllers;

/// <summary>
/// Proxy de imágenes de producto del ERP.
/// En Datamex ERP, dbo.articuloimagen.imagen es el nombre de archivo en disco
/// (PersistentImagesPath/Articulos/...), no un bytea.
/// </summary>
[ApiController]
[Route("media/erp")]
public sealed class ErpMediaController : ControllerBase
{
    private readonly IErpConnectionResolver _erp;
    private readonly ITenantContext _tenant;
    private readonly MediaOptions _media;
    private readonly ILogger<ErpMediaController> _log;

    private static readonly Dictionary<string, string> _mimeByExt = new(StringComparer.OrdinalIgnoreCase)
    {
        [".jpg"] = "image/jpeg",
        [".jpeg"] = "image/jpeg",
        [".png"] = "image/png",
        [".webp"] = "image/webp",
        [".gif"] = "image/gif",
        [".avif"] = "image/avif",
        [".bmp"] = "image/bmp",
    };

    public ErpMediaController(
        IErpConnectionResolver erp,
        ITenantContext tenant,
        IOptions<MediaOptions> media,
        ILogger<ErpMediaController> log)
    {
        _erp = erp;
        _tenant = tenant;
        _media = media.Value;
        _log = log;
    }

    [HttpGet("{filename}")]
    [ResponseCache(Duration = 86400, Location = ResponseCacheLocation.Any)]
    public Task<IActionResult> ByFilename(string filename, CancellationToken ct)
        => ServeAsync(filename, null, ct);

    [HttpGet("id/{id:long}")]
    [ResponseCache(Duration = 86400, Location = ResponseCacheLocation.Any)]
    public Task<IActionResult> ById(long id, CancellationToken ct)
        => ServeAsync(null, id, ct);

    private async Task<IActionResult> ServeAsync(string? filename, long? id, CancellationToken ct)
    {
        var idEmpresa = _tenant.IDEmpresa;
        if (idEmpresa is null
            && HttpContext.Request.Query.TryGetValue("tenant", out var ts)
            && long.TryParse(ts.FirstOrDefault(), out var tid))
        {
            idEmpresa = tid;
        }

        var cs = idEmpresa.HasValue
            ? await _erp.ResolveAsync(idEmpresa.Value, ct)
            : null;

        if (string.IsNullOrWhiteSpace(cs))
        {
            _log.LogDebug("ErpMedia: sin CS ERP (empresa={E})", idEmpresa);
            return NotFound();
        }

        try
        {
            await using var conn = new NpgsqlConnection(cs);
            await conn.OpenAsync(ct);

            var (diskName, displayName) = id.HasValue
                ? await LookupByIdAsync(conn, id.Value, ct)
                : await LookupByFilenameAsync(conn, filename!, ct);

            if (string.IsNullOrWhiteSpace(diskName) && string.IsNullOrWhiteSpace(displayName))
                return NotFound();

            var bytes = TryReadFromDisk(diskName, displayName, idEmpresa);
            if (bytes is null || bytes.Length == 0)
            {
                _log.LogWarning(
                    "ErpMedia: archivo no encontrado disk={D} display={N} root={R} empresa={E}",
                    diskName, displayName, _media.ErpImagesRootPath, idEmpresa);
                return NotFound();
            }

            var contentType = ResolveContentType(displayName ?? diskName ?? filename ?? "");
            Response.Headers.CacheControl = "public, max-age=86400";
            return File(bytes, contentType);
        }
        catch (Exception ex)
        {
            _log.LogWarning(ex, "ErpMediaProxy error filename={F} id={I}", filename, id);
            return NotFound();
        }
    }

    private byte[]? TryReadFromDisk(string? diskName, string? displayName, long? idEmpresa)
    {
        var root = (_media.ErpImagesRootPath ?? "").Trim();
        if (string.IsNullOrWhiteSpace(root))
            return null;

        var sub = string.IsNullOrWhiteSpace(_media.ErpImagesSubdirectory)
            ? "Articulos"
            : _media.ErpImagesSubdirectory.Trim();

        var candidates = new List<string>();
        foreach (var name in new[] { diskName, displayName })
        {
            if (string.IsNullOrWhiteSpace(name)) continue;
            var safe = Path.GetFileName(name.Trim());
            if (string.IsNullOrWhiteSpace(safe)) continue;
            candidates.Add(Path.Combine(root, sub, safe));
            if (idEmpresa is > 0)
                candidates.Add(Path.Combine(root, sub, idEmpresa.Value.ToString(), safe));
        }

        foreach (var path in candidates.Distinct(StringComparer.OrdinalIgnoreCase))
        {
            try
            {
                if (System.IO.File.Exists(path))
                    return System.IO.File.ReadAllBytes(path);
            }
            catch (Exception ex)
            {
                _log.LogDebug(ex, "ErpMedia: no se pudo leer {P}", path);
            }
        }

        return null;
    }

    private static async Task<(string? diskName, string? displayName)> LookupByIdAsync(
        NpgsqlConnection conn, long id, CancellationToken ct)
    {
        await using var cmd = new NpgsqlCommand(
            "SELECT imagen, nombrearchivoimagen FROM dbo.articuloimagen WHERE id = @id AND activo = true LIMIT 1",
            conn);
        cmd.Parameters.AddWithValue("id", id);
        await using var r = await cmd.ExecuteReaderAsync(ct);
        if (!await r.ReadAsync(ct)) return (null, null);
        return (
            r.IsDBNull(0) ? null : r.GetString(0),
            r.IsDBNull(1) ? null : r.GetString(1));
    }

    private static async Task<(string? diskName, string? displayName)> LookupByFilenameAsync(
        NpgsqlConnection conn, string filename, CancellationToken ct)
    {
        await using var cmd = new NpgsqlCommand(
            """
            SELECT imagen, nombrearchivoimagen
            FROM dbo.articuloimagen
            WHERE activo = true
              AND (nombrearchivoimagen = @fn OR imagen = @fn)
            LIMIT 1
            """,
            conn);
        cmd.Parameters.AddWithValue("fn", filename);
        await using var r = await cmd.ExecuteReaderAsync(ct);
        if (!await r.ReadAsync(ct)) return (null, null);
        return (
            r.IsDBNull(0) ? null : r.GetString(0),
            r.IsDBNull(1) ? null : r.GetString(1));
    }

    private static string ResolveContentType(string nameOrPath)
    {
        var ext = Path.GetExtension(nameOrPath);
        return _mimeByExt.TryGetValue(ext, out var mime) ? mime : "image/jpeg";
    }
}
