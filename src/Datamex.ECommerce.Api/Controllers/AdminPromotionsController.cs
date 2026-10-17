using Datamex.ECommerce.Api.Authorization;
using Datamex.ECommerce.Application.Abstractions;
using Datamex.ECommerce.Application.Admin;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Datamex.ECommerce.Api.Controllers;

[ApiController]
[Route("api/admin")]
[EnableRateLimiting("api")]
[Authorize]
public sealed class AdminPromotionsController : ControllerBase
{
    private readonly IAdminPromotionsService _promos;
    private readonly IAdminShippingAdminService _shipping;
    private readonly ITenantContext _tenant;

    public AdminPromotionsController(
        IAdminPromotionsService promos,
        IAdminShippingAdminService shipping,
        ITenantContext tenant)
    {
        _promos = promos;
        _shipping = shipping;
        _tenant = tenant;
    }

    [HttpGet("promotions")]
    [RequirePermission("Site_App_Ecommerce_GrupoArticulo")]
    public async Task<ActionResult<IReadOnlyList<AdminPromotionListItemDto>>> List(CancellationToken ct)
    {
        if (!_tenant.IsResolved) return BadRequest(new { error = "Falta tenant." });
        return Ok(await _promos.ListAsync(ct));
    }

    [HttpGet("promotions/{id:long}")]
    [RequirePermission("Site_App_Ecommerce_GrupoArticulo")]
    public async Task<ActionResult<AdminPromotionDetailDto>> Get(long id, CancellationToken ct)
    {
        if (!_tenant.IsResolved) return BadRequest(new { error = "Falta tenant." });
        var p = await _promos.GetAsync(id, ct);
        return p is null ? NotFound() : Ok(p);
    }

    [HttpPost("promotions")]
    [RequirePermission("Site_App_Ecommerce_GrupoArticulo")]
    public async Task<ActionResult<AdminPromotionDetailDto>> Create(
        [FromBody] UpsertPromotionRequest request, CancellationToken ct)
    {
        if (!_tenant.IsResolved) return BadRequest(new { error = "Falta tenant." });
        try
        {
            var created = await _promos.CreateAsync(request, ct);
            return CreatedAtAction(nameof(Get), new { id = created.Id }, created);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpPut("promotions/{id:long}")]
    [RequirePermission("Site_App_Ecommerce_GrupoArticulo")]
    public async Task<ActionResult<AdminPromotionDetailDto>> Update(
        long id, [FromBody] UpsertPromotionRequest request, CancellationToken ct)
    {
        if (!_tenant.IsResolved) return BadRequest(new { error = "Falta tenant." });
        try
        {
            return Ok(await _promos.UpdateAsync(id, request, ct));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpDelete("promotions/{id:long}")]
    [RequirePermission("Site_App_Ecommerce_GrupoArticulo")]
    public async Task<IActionResult> Delete(long id, CancellationToken ct)
    {
        if (!_tenant.IsResolved) return BadRequest(new { error = "Falta tenant." });
        try
        {
            await _promos.DeleteAsync(id, ct);
            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>Sube imagen de flyer/publicidad. Query opcional id= para asociar a una promo existente.</summary>
    [HttpPost("promotions/imagen/upload")]
    [RequirePermission("Site_App_Ecommerce_GrupoArticulo")]
    [RequestSizeLimit(25_000_000)]
    public async Task<ActionResult<PromoImageUploadResult>> UploadImagen(
        IFormFile file,
        [FromQuery] long? id,
        CancellationToken ct)
    {
        if (!_tenant.IsResolved) return BadRequest(new { error = "Falta tenant." });
        if (file is null || file.Length == 0)
            return BadRequest(new { error = "Archivo requerido (JPEG/PNG/WebP/GIF)." });

        try
        {
            await using var stream = file.OpenReadStream();
            return Ok(await _promos.UploadImageAsync(
                stream, file.ContentType, file.FileName, id, ct));
        }
        catch (Exception ex) when (ex is InvalidOperationException or ArgumentException)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpGet("shipping/tarifas")]
    [RequirePermission("Site_App_Ecommerce_TarifaDelivery")]
    public async Task<ActionResult<IReadOnlyList<AdminTarifaDeliveryDto>>> Tarifas(CancellationToken ct)
    {
        if (!_tenant.IsResolved) return BadRequest(new { error = "Falta tenant." });
        return Ok(await _shipping.ListTarifasAsync(ct));
    }

    [HttpPost("shipping/tarifas")]
    [RequirePermission("Site_App_Ecommerce_TarifaDelivery")]
    public async Task<ActionResult<AdminTarifaDeliveryDto>> CreateTarifa(
        [FromBody] CreateTarifaDeliveryRequest request, CancellationToken ct)
    {
        if (!_tenant.IsResolved) return BadRequest(new { error = "Falta tenant." });
        try
        {
            var created = await _shipping.CreateTarifaAsync(request, ct);
            return CreatedAtAction(nameof(UpdateTarifa), new { id = created.Id }, created);
        }
        catch (Exception ex) when (ex is InvalidOperationException or ArgumentException)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpPut("shipping/tarifas/{id:long}")]
    [RequirePermission("Site_App_Ecommerce_TarifaDelivery")]
    public async Task<ActionResult<AdminTarifaDeliveryDto>> UpdateTarifa(
        long id, [FromBody] UpdateTarifaDeliveryRequest request, CancellationToken ct)
    {
        if (!_tenant.IsResolved) return BadRequest(new { error = "Falta tenant." });
        try
        {
            return Ok(await _shipping.UpdateTarifaAsync(id, request, ct));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpPost("shipping/tarifas/autorizar")]
    [RequirePermission("Site_App_Ecommerce_TarifaDelivery_AutorizacionAnulacion")]
    public async Task<ActionResult<BulkEstadoResult>> AutorizarTarifas(
        [FromBody] TarifaIdsRequest request, CancellationToken ct)
    {
        if (!_tenant.IsResolved) return BadRequest(new { error = "Falta tenant." });
        try { return Ok(await _shipping.AutorizarTarifasAsync(request.Ids, ct)); }
        catch (Exception ex) when (ex is InvalidOperationException or ArgumentException)
        { return BadRequest(new { error = ex.Message }); }
    }

    [HttpPost("shipping/tarifas/anular")]
    [RequirePermission("Site_App_Ecommerce_TarifaDelivery_AutorizacionAnulacion")]
    public async Task<ActionResult<BulkEstadoResult>> AnularTarifas(
        [FromBody] TarifaIdsRequest request, CancellationToken ct)
    {
        if (!_tenant.IsResolved) return BadRequest(new { error = "Falta tenant." });
        try { return Ok(await _shipping.AnularTarifasAsync(request.Ids, ct)); }
        catch (Exception ex) when (ex is InvalidOperationException or ArgumentException)
        { return BadRequest(new { error = ex.Message }); }
    }

    [HttpPost("shipping/tarifas/cancelar")]
    [RequirePermission("Site_App_Ecommerce_TarifaDelivery_Cancelacion")]
    public async Task<ActionResult<BulkEstadoResult>> CancelarTarifas(
        [FromBody] TarifaIdsRequest request, CancellationToken ct)
    {
        if (!_tenant.IsResolved) return BadRequest(new { error = "Falta tenant." });
        try { return Ok(await _shipping.CancelarTarifasAsync(request.Ids, ct)); }
        catch (Exception ex) when (ex is InvalidOperationException or ArgumentException)
        { return BadRequest(new { error = ex.Message }); }
    }
}
