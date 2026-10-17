using Datamex.ECommerce.Api.Authorization;
using Datamex.ECommerce.Application.Abstractions;
using Datamex.ECommerce.Application.Engagement;
using Datamex.ECommerce.Application.Reviews;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Datamex.ECommerce.Api.Controllers;

[ApiController]
[Route("api/admin")]
[EnableRateLimiting("api")]
[Authorize]
public sealed class AdminMarketplaceController : ControllerBase
{
    private readonly IReviewService _reviews;
    private readonly IEngagementService _engagement;
    private readonly ITenantContext _tenant;

    public AdminMarketplaceController(IReviewService reviews, IEngagementService engagement, ITenantContext tenant)
    {
        _reviews = reviews;
        _engagement = engagement;
        _tenant = tenant;
    }

    [HttpGet("resenas")]
    [RequirePermission("Site_App_Ecommerce_Resenas")]
    public async Task<ActionResult<IReadOnlyList<ResenaDto>>> Resenas(CancellationToken ct)
    {
        if (!_tenant.IsResolved) return BadRequest(new { error = "Falta tenant." });
        return Ok(await _reviews.AdminListPendientesAsync(ct));
    }

    [HttpPost("resenas/{id:long}/aprobar")]
    [RequirePermission("Site_App_Ecommerce_Resenas")]
    public async Task<IActionResult> Aprobar(long id, [FromQuery] bool aprobar = true, CancellationToken ct = default)
    {
        if (!_tenant.IsResolved) return BadRequest(new { error = "Falta tenant." });
        try
        {
            await _reviews.AprobarAsync(id, aprobar, ct);
            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpGet("cupones")]
    [RequirePermission("Site_App_Ecommerce_Cupones")]
    public async Task<ActionResult<IReadOnlyList<CuponDto>>> Cupones(CancellationToken ct)
    {
        if (!_tenant.IsResolved) return BadRequest(new { error = "Falta tenant." });
        return Ok(await _engagement.AdminListCuponesAsync(ct));
    }

    [HttpPost("cupones")]
    [RequirePermission("Site_App_Ecommerce_Cupones")]
    public async Task<ActionResult<CuponDto>> CrearCupon([FromBody] CrearCuponRequest request, CancellationToken ct)
    {
        if (!_tenant.IsResolved) return BadRequest(new { error = "Falta tenant." });
        try
        {
            return Ok(await _engagement.AdminCrearCuponAsync(request, ct));
        }
        catch (Exception ex) when (ex is InvalidOperationException or ArgumentException)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpPost("cupones/{id:long}/toggle")]
    [RequirePermission("Site_App_Ecommerce_Cupones")]
    public async Task<IActionResult> ToggleCupon(long id, [FromQuery] bool activo = true, CancellationToken ct = default)
    {
        if (!_tenant.IsResolved) return BadRequest(new { error = "Falta tenant." });
        try
        {
            await _engagement.AdminToggleCuponAsync(id, activo, ct);
            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpPost("pedidos/{idPedidoMovil:long}/tracking")]
    [RequirePermission("Site_App_Ecommerce_Pedidos")]
    public async Task<IActionResult> AddTracking(
        long idPedidoMovil, [FromBody] AdminTrackingBody body, CancellationToken ct)
    {
        if (!_tenant.IsResolved) return BadRequest(new { error = "Falta tenant." });
        try
        {
            await _engagement.AdminAddTrackingAsync(idPedidoMovil, body.Estado, body.Mensaje, body.CodigoTracking, ct);
            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpGet("devoluciones")]
    [RequirePermission("Site_App_Ecommerce_Devoluciones")]
    public async Task<ActionResult<IReadOnlyList<DevolucionDto>>> Devoluciones(CancellationToken ct)
    {
        if (!_tenant.IsResolved) return BadRequest(new { error = "Falta tenant." });
        return Ok(await _engagement.AdminDevolucionesAsync(ct));
    }

    [HttpPost("devoluciones/{id:long}/estado")]
    [RequirePermission("Site_App_Ecommerce_Devoluciones")]
    public async Task<IActionResult> UpdateDev(long id, [FromBody] AdminEstadoBody body, CancellationToken ct)
    {
        if (!_tenant.IsResolved) return BadRequest(new { error = "Falta tenant." });
        try
        {
            await _engagement.AdminUpdateDevolucionAsync(id, body.Estado, ct);
            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpGet("chat/conversaciones")]
    [RequirePermission("Site_App_Ecommerce_Chat")]
    public async Task<ActionResult<IReadOnlyList<ChatConversacionDto>>> Conversaciones(
        [FromQuery] string? estado, [FromQuery] string? q, CancellationToken ct = default)
    {
        if (!_tenant.IsResolved) return BadRequest(new { error = "Falta tenant." });
        return Ok(await _engagement.AdminListConversacionesAsync(estado, q, ct));
    }

    [HttpGet("chat/conversaciones/{id:long}")]
    [RequirePermission("Site_App_Ecommerce_Chat")]
    public async Task<ActionResult<IReadOnlyList<ChatMensajeDto>>> ConversacionMensajes(long id, CancellationToken ct)
    {
        if (!_tenant.IsResolved) return BadRequest(new { error = "Falta tenant." });
        try
        {
            return Ok(await _engagement.AdminGetConversacionMensajesAsync(id, ct));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpPost("chat/conversaciones/{id:long}/responder")]
    [RequirePermission("Site_App_Ecommerce_Chat")]
    public async Task<ActionResult<ChatMensajeDto>> ResponderConversacion(
        long id, [FromBody] AdminResponderChatRequest body, CancellationToken ct)
    {
        if (!_tenant.IsResolved) return BadRequest(new { error = "Falta tenant." });
        try
        {
            var adminId = _tenant.IDAdminUsuario;
            var nombre = User.Identity?.Name ?? "soporte";
            return Ok(await _engagement.AdminResponderConversacionAsync(
                id, body.Mensaje, adminId, nombre, body.Estado, ct));
        }
        catch (Exception ex) when (ex is InvalidOperationException or ArgumentException)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpPost("chat/conversaciones/{id:long}/upload")]
    [RequirePermission("Site_App_Ecommerce_Chat")]
    [RequestSizeLimit(25_000_000)]
    public async Task<ActionResult<ChatMensajeDto>> ResponderConFoto(
        long id, IFormFile file, [FromForm] string? mensaje, CancellationToken ct = default)
    {
        if (!_tenant.IsResolved) return BadRequest(new { error = "Falta tenant." });
        if (file is null || file.Length == 0)
            return BadRequest(new { error = "Archivo requerido." });
        try
        {
            await using var stream = file.OpenReadStream();
            return Ok(await _engagement.AdminResponderConAdjuntoAsync(
                id, stream, file.ContentType, file.FileName, mensaje,
                _tenant.IDAdminUsuario, User.Identity?.Name ?? "soporte", ct));
        }
        catch (Exception ex) when (ex is InvalidOperationException or ArgumentException)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpPost("chat/conversaciones/{id:long}/asignar")]
    [RequirePermission("Site_App_Ecommerce_Chat")]
    public async Task<IActionResult> Asignar(long id, [FromBody] AdminAsignarBody body, CancellationToken ct)
    {
        if (!_tenant.IsResolved) return BadRequest(new { error = "Falta tenant." });
        try
        {
            await _engagement.AdminAsignarConversacionAsync(
                id, body.IdAdmin ?? _tenant.IDAdminUsuario, body.NombreAdmin ?? User.Identity?.Name, ct);
            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpPost("chat/conversaciones/{id:long}/estado")]
    [RequirePermission("Site_App_Ecommerce_Chat")]
    public async Task<IActionResult> EstadoConv(long id, [FromBody] AdminEstadoBody body, CancellationToken ct)
    {
        if (!_tenant.IsResolved) return BadRequest(new { error = "Falta tenant." });
        try
        {
            await _engagement.AdminCambiarEstadoConversacionAsync(id, body.Estado, ct);
            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpGet("chat")]
    [RequirePermission("Site_App_Ecommerce_Chat")]
    public async Task<ActionResult<IReadOnlyList<ChatMensajeDto>>> Chat(CancellationToken ct)
    {
        if (!_tenant.IsResolved) return BadRequest(new { error = "Falta tenant." });
        return Ok(await _engagement.AdminChatsAsync(ct));
    }

    [HttpPost("chat/{idMensaje:long}/responder")]
    [RequirePermission("Site_App_Ecommerce_Chat")]
    public async Task<IActionResult> Responder(long idMensaje, [FromBody] AdminChatBody body, CancellationToken ct)
    {
        if (!_tenant.IsResolved) return BadRequest(new { error = "Falta tenant." });
        try
        {
            await _engagement.AdminResponderChatAsync(idMensaje, body.Mensaje, ct);
            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    public record AdminTrackingBody(string Estado, string? Mensaje, string? CodigoTracking);
    public record AdminEstadoBody(string Estado);
    public record AdminChatBody(string Mensaje);
    public record AdminAsignarBody(long? IdAdmin, string? NombreAdmin);
}
