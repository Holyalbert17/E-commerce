using Datamex.ECommerce.Application.Abstractions;
using Datamex.ECommerce.Application.Audit;
using Datamex.ECommerce.Application.Content;
using Datamex.ECommerce.Application.Media;
using Datamex.ECommerce.Api.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using System.Security.Claims;

namespace Datamex.ECommerce.Api.Controllers;

[ApiController]
[Route("api/admin/content")]
[EnableRateLimiting("api")]
[Authorize]
public sealed class AdminContentController : ControllerBase
{
    private readonly IContentCmsService _cms;
    private readonly IAdminAuditService _audit;
    private readonly ITenantContext _tenant;
    private readonly IMediaStorage _media;

    public AdminContentController(
        IContentCmsService cms,
        IAdminAuditService audit,
        ITenantContext tenant,
        IMediaStorage media)
    {
        _cms = cms;
        _audit = audit;
        _tenant = tenant;
        _media = media;
    }

    [HttpPost("blocks/ensure-home")]
    [RequirePermission("Site_App_Ecommerce_Personalizacion")]
    public async Task<ActionResult<object>> EnsureHomeBlocks(CancellationToken ct)
    {
        if (!_tenant.IsResolved) return BadRequest(new { error = "Falta tenant." });
        var added = await _cms.EnsureMissingHomeBlocksAsync(ct);
        await _audit.LogAsync("ensure", "ContenidoBloque", null, $"home blocks +{added}",
            TryAdminId(), User.Identity?.Name, null, ct);
        return Ok(new { added, mensaje = added > 0 ? $"Se agregaron {added} bloques." : "Ya estaban todos los bloques estándar." });
    }

    [HttpGet("blocks")]
    [RequirePermission("Site_App_Ecommerce_Personalizacion")]
    public async Task<ActionResult<IReadOnlyList<ContenidoBloqueDto>>> Blocks(
        [FromQuery] string? pageCode, CancellationToken ct)
    {
        if (!_tenant.IsResolved) return BadRequest(new { error = "Falta tenant." });
        return Ok(await _cms.ListBlocksAsync(pageCode, ct));
    }

    /// <summary>
    /// Sube una imagen de CMS al storage configurado (disco local, SFTP o espejo).
    /// La URL devuelta se asigna después al ítem del bloque que corresponda.
    /// </summary>
    [HttpPost("media/upload")]
    [RequirePermission("Site_App_Ecommerce_Personalizacion")]
    [RequestSizeLimit(25_000_000)]
    public async Task<ActionResult<object>> UploadCmsMedia(IFormFile file, CancellationToken ct)
    {
        if (!_tenant.IsResolved || _tenant.IDEmpresa is null)
            return BadRequest(new { error = "Falta tenant." });
        if (file is null || file.Length == 0)
            return BadRequest(new { error = "Archivo requerido." });
        if (!file.ContentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase))
            return BadRequest(new { error = "El archivo debe ser una imagen." });

        try
        {
            await using var stream = file.OpenReadStream();
            var stored = await _media.SaveBrandingAssetAsync(
                _tenant.IDEmpresa.Value, "cms", stream, file.ContentType, file.FileName, ct);
            await _audit.LogAsync(
                "upload", "ContenidoMedia", null, stored.PublicUrl,
                TryAdminId(), User.Identity?.Name, HttpContext.Connection.RemoteIpAddress?.ToString(), ct);
            return Ok(new { url = stored.PublicUrl, originalFileName = stored.OriginalFileName, sizeBytes = stored.SizeBytes });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpGet("blocks/{id:long}")]
    [RequirePermission("Site_App_Ecommerce_Personalizacion")]
    public async Task<ActionResult<ContenidoBloqueDto>> Block(long id, CancellationToken ct)
    {
        if (!_tenant.IsResolved) return BadRequest(new { error = "Falta tenant." });
        var b = await _cms.GetBlockAsync(id, ct);
        return b is null ? NotFound() : Ok(b);
    }

    [HttpPost("blocks")]
    [RequirePermission("Site_App_Ecommerce_Personalizacion")]
    public async Task<ActionResult<ContenidoBloqueDto>> CreateBlock(
        [FromBody] UpsertContenidoBloqueRequest body, CancellationToken ct)
    {
        if (!_tenant.IsResolved) return BadRequest(new { error = "Falta tenant." });
        try
        {
            var adminId = TryAdminId();
            var dto = await _cms.UpsertBlockAsync(null, body, adminId, ct);
            await _audit.LogAsync("create", "ContenidoBloque", dto.Id.ToString(), dto.InternalName,
                adminId, User.Identity?.Name, HttpContext.Connection.RemoteIpAddress?.ToString(), ct);
            return Ok(dto);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpPut("blocks/{id:long}")]
    [RequirePermission("Site_App_Ecommerce_Personalizacion")]
    public async Task<ActionResult<ContenidoBloqueDto>> UpdateBlock(
        long id, [FromBody] UpsertContenidoBloqueRequest body, CancellationToken ct)
    {
        if (!_tenant.IsResolved) return BadRequest(new { error = "Falta tenant." });
        try
        {
            var adminId = TryAdminId();
            var dto = await _cms.UpsertBlockAsync(id, body, adminId, ct);
            await _audit.LogAsync("update", "ContenidoBloque", id.ToString(), dto.InternalName,
                adminId, User.Identity?.Name, HttpContext.Connection.RemoteIpAddress?.ToString(), ct);
            return Ok(dto);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpDelete("blocks/{id:long}")]
    [RequirePermission("Site_App_Ecommerce_Personalizacion")]
    public async Task<IActionResult> DeleteBlock(long id, CancellationToken ct)
    {
        if (!_tenant.IsResolved) return BadRequest(new { error = "Falta tenant." });
        try
        {
            await _cms.DeleteBlockAsync(id, ct);
            await _audit.LogAsync("delete", "ContenidoBloque", id.ToString(), null,
                TryAdminId(), User.Identity?.Name, HttpContext.Connection.RemoteIpAddress?.ToString(), ct);
            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpGet("menus")]
    [RequirePermission("Site_App_Ecommerce_Personalizacion")]
    public async Task<ActionResult<IReadOnlyList<NavegacionMenuDto>>> Menus(CancellationToken ct)
    {
        if (!_tenant.IsResolved) return BadRequest(new { error = "Falta tenant." });
        return Ok(await _cms.ListMenusAsync(ct));
    }

    [HttpPost("menus")]
    [RequirePermission("Site_App_Ecommerce_Personalizacion")]
    public async Task<ActionResult<NavegacionMenuDto>> CreateMenu(
        [FromBody] UpsertNavegacionMenuRequest body, CancellationToken ct)
    {
        if (!_tenant.IsResolved) return BadRequest(new { error = "Falta tenant." });
        try
        {
            var dto = await _cms.UpsertMenuAsync(null, body, ct);
            await _audit.LogAsync("create", "NavegacionMenu", dto.Id.ToString(), dto.Code,
                TryAdminId(), User.Identity?.Name, null, ct);
            return Ok(dto);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpPut("menus/{id:long}")]
    [RequirePermission("Site_App_Ecommerce_Personalizacion")]
    public async Task<ActionResult<NavegacionMenuDto>> UpdateMenu(
        long id, [FromBody] UpsertNavegacionMenuRequest body, CancellationToken ct)
    {
        if (!_tenant.IsResolved) return BadRequest(new { error = "Falta tenant." });
        try
        {
            var dto = await _cms.UpsertMenuAsync(id, body, ct);
            await _audit.LogAsync("update", "NavegacionMenu", id.ToString(), dto.Code,
                TryAdminId(), User.Identity?.Name, null, ct);
            return Ok(dto);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpDelete("menus/{id:long}")]
    [RequirePermission("Site_App_Ecommerce_Personalizacion")]
    public async Task<IActionResult> DeleteMenu(long id, CancellationToken ct)
    {
        if (!_tenant.IsResolved) return BadRequest(new { error = "Falta tenant." });
        try
        {
            await _cms.DeleteMenuAsync(id, ct);
            await _audit.LogAsync("delete", "NavegacionMenu", id.ToString(), null,
                TryAdminId(), User.Identity?.Name, null, ct);
            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpGet("audit")]
    [RequirePermission("Site_App_Ecommerce")]
    public async Task<ActionResult<IReadOnlyList<AdminAuditoriaDto>>> Audit(
        [FromQuery] int take = 100, CancellationToken ct = default)
    {
        if (!_tenant.IsResolved) return BadRequest(new { error = "Falta tenant." });
        return Ok(await _audit.ListAsync(take, ct));
    }

    private long? TryAdminId()
    {
        var sub = User.FindFirst("sub")?.Value ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return long.TryParse(sub, out var id) ? id : null;
    }
}

[ApiController]
[Route("api/storefront/content")]
[EnableRateLimiting("api")]
[AllowAnonymous]
public sealed class StorefrontContentController : ControllerBase
{
    private readonly IContentCmsService _cms;
    private readonly ITenantContext _tenant;

    public StorefrontContentController(IContentCmsService cms, ITenantContext tenant)
    {
        _cms = cms;
        _tenant = tenant;
    }

    [HttpGet("home")]
    public async Task<ActionResult<StorefrontHomeContentDto>> Home(CancellationToken ct)
    {
        if (!_tenant.IsResolved) return BadRequest(new { error = "Falta tenant." });
        return Ok(await _cms.GetPublicHomeAsync(ct));
    }
}
