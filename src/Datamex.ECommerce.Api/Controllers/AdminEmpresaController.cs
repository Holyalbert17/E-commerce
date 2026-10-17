using Datamex.ECommerce.Application.Abstractions;
using Datamex.ECommerce.Application.Admin;
using Datamex.ECommerce.Application.Branding;
using Datamex.ECommerce.Application.Sync;
using Datamex.ECommerce.Api.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Datamex.ECommerce.Api.Controllers;

[ApiController]
[Route("api/admin/empresa")]
[EnableRateLimiting("api")]
[Authorize]
public sealed class AdminEmpresaController : ControllerBase
{
    private readonly IAdminEmpresaService _empresa;
    private readonly IStorefrontBrandingService _branding;
    private readonly ITenantContext _tenant;

    public AdminEmpresaController(
        IAdminEmpresaService empresa,
        IStorefrontBrandingService branding,
        ITenantContext tenant)
    {
        _empresa = empresa;
        _branding = branding;
        _tenant = tenant;
    }

    [HttpGet("config")]
    [RequirePermission(
        "Site_App_Ecommerce_Empresa",
        "Site_App_Ecommerce_Empresa_Agencia",
        "Site_App_Ecommerce_Empresa_HorarioAtencion")]
    public async Task<ActionResult<AdminEmpresaConfigDto>> GetConfig(CancellationToken ct)
    {
        if (!_tenant.IsResolved) return BadRequest(new { error = "Falta tenant." });
        var cfg = await _empresa.GetConfigAsync(ct);
        return cfg is null ? NotFound() : Ok(cfg);
    }

    [HttpPut("config")]
    [RequirePermission("Site_App_Ecommerce_Empresa")]
    public async Task<ActionResult<AdminEmpresaConfigDto>> UpdateConfig(
        [FromBody] UpdateEmpresaConfigRequest request, CancellationToken ct)
    {
        if (!_tenant.IsResolved) return BadRequest(new { error = "Falta tenant." });
        try
        {
            return Ok(await _empresa.UpdateConfigAsync(request, ct));
        }
        catch (Exception ex) when (ex is InvalidOperationException or ArgumentException)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>Prueba la connection string ERP (guardada o draft en body).</summary>
    [HttpPost("config/test-erp")]
    [RequirePermission("Site_App_Ecommerce_Empresa")]
    public async Task<ActionResult<ErpConnectionTestResult>> TestErp(
        [FromBody] TestErpConnectionRequest? draft, CancellationToken ct)
    {
        if (!_tenant.IsResolved) return BadRequest(new { error = "Falta tenant." });
        var result = await _empresa.TestErpConnectionAsync(draft, ct);
        return Ok(result);
    }

    [HttpGet("config/erp-health")]
    [RequirePermission(
        "Site_App_Ecommerce_Empresa",
        "Site_App_Ecommerce_Empresa_Agencia",
        "Site_App_Ecommerce_Empresa_HorarioAtencion")]
    public async Task<ActionResult<AdminErpHealthDto>> ErpHealth(CancellationToken ct)
    {
        if (!_tenant.IsResolved) return BadRequest(new { error = "Falta tenant." });
        try
        {
            return Ok(await _empresa.GetErpHealthAsync(ct));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>Reintenta write-back de pedidos e-com pendientes (máx. 40).</summary>
    [HttpPost("config/erp-flush-pending")]
    [RequirePermission("Site_App_Ecommerce_Empresa", "Site_App_Ecommerce_Pedidos")]
    public async Task<ActionResult<MigrarPedidosResult>> FlushPendingOrders(CancellationToken ct)
    {
        if (!_tenant.IsResolved) return BadRequest(new { error = "Falta tenant." });
        try
        {
            return Ok(await _empresa.FlushPendingOrdersAsync(ct));
        }
        catch (Exception ex) when (ex is InvalidOperationException or ArgumentException)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpGet("branding")]
    [RequirePermission("Site_App_Ecommerce_Empresa")]
    public async Task<ActionResult<StorefrontBrandingDto>> GetBranding(CancellationToken ct)
    {
        if (!_tenant.IsResolved) return BadRequest(new { error = "Falta tenant." });
        var b = await _branding.GetPublicAsync(ct);
        return b is null ? NotFound() : Ok(b);
    }

    [HttpPut("branding")]
    [RequirePermission("Site_App_Ecommerce_Empresa")]
    public async Task<ActionResult<StorefrontBrandingDto>> UpdateBranding(
        [FromBody] UpdateBrandingRequest request, CancellationToken ct)
    {
        if (!_tenant.IsResolved) return BadRequest(new { error = "Falta tenant." });
        try
        {
            return Ok(await _branding.UpdateAsync(request, ct));
        }
        catch (Exception ex) when (ex is InvalidOperationException or ArgumentException)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpPost("branding/upload/{kind}")]
    [RequirePermission("Site_App_Ecommerce_Empresa")]
    [RequestSizeLimit(25_000_000)]
    public async Task<ActionResult<StorefrontBrandingDto>> UploadBrandingAsset(
        string kind,
        IFormFile file,
        CancellationToken ct)
    {
        if (!_tenant.IsResolved) return BadRequest(new { error = "Falta tenant." });
        if (file is null || file.Length == 0)
            return BadRequest(new { error = "Archivo requerido." });

        if (!TryParseKind(kind, out var assetKind))
            return BadRequest(new { error = "kind debe ser logo, favicon o banner." });

        try
        {
            await using var stream = file.OpenReadStream();
            return Ok(await _branding.UploadAssetAsync(
                assetKind, stream, file.ContentType, file.FileName, ct));
        }
        catch (Exception ex) when (ex is InvalidOperationException or ArgumentException)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    private static bool TryParseKind(string kind, out BrandingAssetKind assetKind)
    {
        assetKind = BrandingAssetKind.Logo;
        switch ((kind ?? "").Trim().ToLowerInvariant())
        {
            case "logo":
                assetKind = BrandingAssetKind.Logo;
                return true;
            case "favicon":
                assetKind = BrandingAssetKind.Favicon;
                return true;
            case "banner":
                assetKind = BrandingAssetKind.Banner;
                return true;
            default:
                return false;
        }
    }

    [HttpGet("depositos")]
    [RequirePermission("Site_App_Ecommerce_Empresa_Agencia")]
    public async Task<ActionResult<IReadOnlyList<AdminDepositoDto>>> ListDepositos(CancellationToken ct)
    {
        if (!_tenant.IsResolved) return BadRequest(new { error = "Falta tenant." });
        return Ok(await _empresa.ListDepositosAsync(ct));
    }

    [HttpGet("agencias")]
    [RequirePermission("Site_App_Ecommerce_Empresa_Agencia")]
    public async Task<ActionResult<IReadOnlyList<AdminAgenciaDto>>> ListAgencias(CancellationToken ct)
    {
        if (!_tenant.IsResolved) return BadRequest(new { error = "Falta tenant." });
        return Ok(await _empresa.ListAgenciasAsync(ct));
    }

    [HttpPost("agencias")]
    [RequirePermission("Site_App_Ecommerce_Empresa_Agencia")]
    public async Task<ActionResult<AdminAgenciaDto>> CreateAgencia(
        [FromBody] UpsertAgenciaRequest request, CancellationToken ct)
    {
        if (!_tenant.IsResolved) return BadRequest(new { error = "Falta tenant." });
        try
        {
            return Ok(await _empresa.CreateAgenciaAsync(request, ct));
        }
        catch (Exception ex) when (ex is InvalidOperationException or ArgumentException)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpPut("agencias/{id:long}")]
    [RequirePermission("Site_App_Ecommerce_Empresa_Agencia")]
    public async Task<ActionResult<AdminAgenciaDto>> UpdateAgencia(
        long id, [FromBody] UpsertAgenciaRequest request, CancellationToken ct)
    {
        if (!_tenant.IsResolved) return BadRequest(new { error = "Falta tenant." });
        try
        {
            return Ok(await _empresa.UpdateAgenciaAsync(id, request, ct));
        }
        catch (Exception ex) when (ex is InvalidOperationException or ArgumentException)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpDelete("agencias/{id:long}")]
    [RequirePermission("Site_App_Ecommerce_Empresa_Agencia")]
    public async Task<IActionResult> DeleteAgencia(long id, CancellationToken ct)
    {
        if (!_tenant.IsResolved) return BadRequest(new { error = "Falta tenant." });
        try
        {
            await _empresa.DeleteAgenciaAsync(id, ct);
            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpGet("horarios")]
    [RequirePermission("Site_App_Ecommerce_Empresa_HorarioAtencion")]
    public async Task<ActionResult<IReadOnlyList<AdminHorarioDto>>> ListHorarios(
        [FromQuery] long? idAgencia, CancellationToken ct)
    {
        if (!_tenant.IsResolved) return BadRequest(new { error = "Falta tenant." });
        return Ok(await _empresa.ListHorariosAsync(idAgencia, ct));
    }

    [HttpPost("horarios")]
    [RequirePermission("Site_App_Ecommerce_Empresa_HorarioAtencion")]
    public async Task<ActionResult<AdminHorarioDto>> CreateHorario(
        [FromBody] UpsertHorarioRequest request, CancellationToken ct)
    {
        if (!_tenant.IsResolved) return BadRequest(new { error = "Falta tenant." });
        try
        {
            return Ok(await _empresa.CreateHorarioAsync(request, ct));
        }
        catch (Exception ex) when (ex is InvalidOperationException or ArgumentException)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpPut("horarios/{id:long}")]
    [RequirePermission("Site_App_Ecommerce_Empresa_HorarioAtencion")]
    public async Task<ActionResult<AdminHorarioDto>> UpdateHorario(
        long id, [FromBody] UpsertHorarioRequest request, CancellationToken ct)
    {
        if (!_tenant.IsResolved) return BadRequest(new { error = "Falta tenant." });
        try
        {
            return Ok(await _empresa.UpdateHorarioAsync(id, request, ct));
        }
        catch (Exception ex) when (ex is InvalidOperationException or ArgumentException)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpDelete("horarios/{id:long}")]
    [RequirePermission("Site_App_Ecommerce_Empresa_HorarioAtencion")]
    public async Task<IActionResult> DeleteHorario(long id, CancellationToken ct)
    {
        if (!_tenant.IsResolved) return BadRequest(new { error = "Falta tenant." });
        try
        {
            await _empresa.DeleteHorarioAsync(id, ct);
            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }
}
