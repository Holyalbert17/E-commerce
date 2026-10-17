using Datamex.ECommerce.Application.Abstractions;
using Datamex.ECommerce.Application.Engagement;
using Datamex.ECommerce.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;

namespace Datamex.ECommerce.Api.Controllers;

[ApiController]
[Route("api/engagement")]
[EnableRateLimiting("api")]
public sealed class EngagementController : ControllerBase
{
    private readonly IEngagementService _engagement;
    private readonly ITenantContext _tenant;
    private readonly EcomDbContext _db;

    public EngagementController(IEngagementService engagement, ITenantContext tenant, EcomDbContext db)
    {
        _engagement = engagement;
        _tenant = tenant;
        _db = db;
    }

    [HttpPost("track")]
    public async Task<IActionResult> Track([FromBody] TrackInteraccionRequest request, CancellationToken ct)
    {
        if (!_tenant.IsResolved) return BadRequest(new { error = "Falta tenant." });
        string? email = null;
        if (_tenant.IDUsuarioEcommerce.HasValue)
        {
            email = await _db.UsuariosEcommerce.AsNoTracking()
                .Where(u => u.ID == _tenant.IDUsuarioEcommerce.Value)
                .Select(u => u.Email)
                .FirstOrDefaultAsync(ct);
        }
        await _engagement.TrackAsync(request, _tenant.IDUsuarioEcommerce, email, ct);
        return NoContent();
    }

    [HttpGet("recomendaciones")]
    public async Task<ActionResult<IReadOnlyList<RecomendacionDto>>> Recomendaciones(
        [FromQuery] long? idArticulo, [FromQuery] string canal = "B2C", [FromQuery] int take = 8, CancellationToken ct = default)
    {
        if (!_tenant.IsResolved) return BadRequest(new { error = "Falta tenant." });
        return Ok(await _engagement.GetRecomendacionesAsync(idArticulo, canal, take, ct));
    }

    [HttpPost("cupones/aplicar")]
    public async Task<ActionResult<AplicarCuponResult>> AplicarCupon([FromBody] AplicarCuponBody body, CancellationToken ct)
    {
        if (!_tenant.IsResolved) return BadRequest(new { error = "Falta tenant." });
        var session = Request.Headers["X-Cart-Session"].FirstOrDefault();
        if (string.IsNullOrWhiteSpace(session))
            return BadRequest(new { error = "Falta X-Cart-Session." });
        try
        {
            return Ok(await _engagement.AplicarCuponAsync(session, body.Codigo, body.Canal ?? "B2C", _tenant.IDUsuarioEcommerce, ct));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpDelete("cupones")]
    public async Task<IActionResult> QuitarCupon(CancellationToken ct)
    {
        if (!_tenant.IsResolved) return BadRequest(new { error = "Falta tenant." });
        var session = Request.Headers["X-Cart-Session"].FirstOrDefault();
        if (string.IsNullOrWhiteSpace(session))
            return BadRequest(new { error = "Falta X-Cart-Session." });
        await _engagement.QuitarCuponAsync(session, ct);
        return NoContent();
    }

    [Authorize]
    [HttpPost("seguir/{idArticulo:long}")]
    public async Task<IActionResult> Seguir(long idArticulo, CancellationToken ct)
    {
        if (!_tenant.IsResolved) return BadRequest(new { error = "Falta tenant." });
        if (!_tenant.IDUsuarioEcommerce.HasValue) return Unauthorized();
        await _engagement.SeguirProductoAsync(_tenant.IDUsuarioEcommerce.Value, idArticulo, ct);
        return NoContent();
    }

    [Authorize]
    [HttpDelete("seguir/{idArticulo:long}")]
    public async Task<IActionResult> DejarSeguir(long idArticulo, CancellationToken ct)
    {
        if (!_tenant.IsResolved) return BadRequest(new { error = "Falta tenant." });
        if (!_tenant.IDUsuarioEcommerce.HasValue) return Unauthorized();
        await _engagement.DejarDeSeguirAsync(_tenant.IDUsuarioEcommerce.Value, idArticulo, ct);
        return NoContent();
    }

    [Authorize]
    [HttpGet("seguidos")]
    public async Task<ActionResult<IReadOnlyList<SeguirProductoDto>>> Seguidos(CancellationToken ct)
    {
        if (!_tenant.IsResolved) return BadRequest(new { error = "Falta tenant." });
        if (!_tenant.IDUsuarioEcommerce.HasValue) return Unauthorized();
        return Ok(await _engagement.ListSeguidosAsync(_tenant.IDUsuarioEcommerce.Value, ct));
    }

    [Authorize]
    [HttpGet("seguidos/contains/{idArticulo:long}")]
    public async Task<ActionResult<object>> SeguidosContains(long idArticulo, CancellationToken ct)
    {
        if (!_tenant.IsResolved) return BadRequest(new { error = "Falta tenant." });
        if (!_tenant.IDUsuarioEcommerce.HasValue) return Unauthorized();
        var ok = await _engagement.EstaSeguidoAsync(_tenant.IDUsuarioEcommerce.Value, idArticulo, ct);
        return Ok(new { seguido = ok });
    }

    [Authorize]
    [HttpGet("tracking/{idPedidoMovil:long}")]
    public async Task<ActionResult<IReadOnlyList<SeguimientoDto>>> Tracking(long idPedidoMovil, CancellationToken ct)
    {
        if (!_tenant.IsResolved) return BadRequest(new { error = "Falta tenant." });
        return Ok(await _engagement.GetTrackingAsync(idPedidoMovil, ct));
    }

    [Authorize]
    [HttpPost("tracking/{idPedidoMovil:long}/share")]
    public async Task<ActionResult<object>> ShareTracking(long idPedidoMovil, CancellationToken ct)
    {
        if (!_tenant.IsResolved) return BadRequest(new { error = "Falta tenant." });
        if (!_tenant.IDUsuarioEcommerce.HasValue) return Unauthorized();
        try
        {
            var token = await _engagement.EnsureTrackingShareTokenAsync(
                _tenant.IDUsuarioEcommerce.Value, idPedidoMovil, ct);
            return Ok(new { token, path = $"/seguimiento/{token}" });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [AllowAnonymous]
    [HttpGet("tracking/publico/{token}")]
    public async Task<ActionResult<TrackingPublicoDto>> TrackingPublico(string token, CancellationToken ct)
    {
        if (!_tenant.IsResolved) return BadRequest(new { error = "Falta tenant." });
        var dto = await _engagement.GetTrackingByShareTokenAsync(token, ct);
        return dto is null ? NotFound(new { error = "Enlace de seguimiento inválido." }) : Ok(dto);
    }

    [Authorize]
    [HttpPost("devoluciones")]
    public async Task<ActionResult<DevolucionDto>> Devolucion([FromBody] CrearDevolucionRequest request, CancellationToken ct)
    {
        if (!_tenant.IsResolved) return BadRequest(new { error = "Falta tenant." });
        if (!_tenant.IDUsuarioEcommerce.HasValue) return Unauthorized();
        try
        {
            return Ok(await _engagement.CrearDevolucionAsync(_tenant.IDUsuarioEcommerce.Value, request, ct));
        }
        catch (Exception ex) when (ex is InvalidOperationException or ArgumentException)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [Authorize]
    [HttpPost("devoluciones/upload")]
    [RequestSizeLimit(25_000_000)]
    public async Task<ActionResult<DevolucionDto>> DevolucionConFoto(
        IFormFile? file,
        [FromForm] long idPedidoMovil,
        [FromForm] string motivo,
        [FromForm] string? detalle,
        [FromForm] long? idArticulo,
        CancellationToken ct = default)
    {
        if (!_tenant.IsResolved) return BadRequest(new { error = "Falta tenant." });
        if (!_tenant.IDUsuarioEcommerce.HasValue) return Unauthorized();
        if (string.IsNullOrWhiteSpace(motivo))
            return BadRequest(new { error = "El motivo es obligatorio." });
        try
        {
            var req = new CrearDevolucionRequest(idPedidoMovil, idArticulo, motivo.Trim(), detalle);
            if (file is null || file.Length == 0)
                return Ok(await _engagement.CrearDevolucionAsync(_tenant.IDUsuarioEcommerce.Value, req, ct));

            await using var stream = file.OpenReadStream();
            return Ok(await _engagement.CrearDevolucionConAdjuntoAsync(
                _tenant.IDUsuarioEcommerce.Value, req, stream, file.ContentType, file.FileName, ct));
        }
        catch (Exception ex) when (ex is InvalidOperationException or ArgumentException)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [Authorize]
    [HttpGet("devoluciones")]
    public async Task<ActionResult<IReadOnlyList<DevolucionDto>>> MisDevoluciones(CancellationToken ct)
    {
        if (!_tenant.IsResolved) return BadRequest(new { error = "Falta tenant." });
        if (!_tenant.IDUsuarioEcommerce.HasValue) return Unauthorized();
        return Ok(await _engagement.MisDevolucionesAsync(_tenant.IDUsuarioEcommerce.Value, ct));
    }

    [Authorize]
    [HttpGet("chat/conversaciones")]
    public async Task<ActionResult<IReadOnlyList<ChatConversacionDto>>> MisConversaciones(CancellationToken ct)
    {
        if (!_tenant.IsResolved) return BadRequest(new { error = "Falta tenant." });
        if (!_tenant.IDUsuarioEcommerce.HasValue) return Unauthorized();
        return Ok(await _engagement.MisConversacionesAsync(_tenant.IDUsuarioEcommerce.Value, ct));
    }

    [Authorize]
    [HttpGet("chat")]
    public async Task<ActionResult<IReadOnlyList<ChatMensajeDto>>> Chat(
        [FromQuery] long? idPedidoMovil, [FromQuery] long? idConversacion, CancellationToken ct = default)
    {
        if (!_tenant.IsResolved) return BadRequest(new { error = "Falta tenant." });
        if (!_tenant.IDUsuarioEcommerce.HasValue) return Unauthorized();
        return Ok(await _engagement.MisChatsAsync(_tenant.IDUsuarioEcommerce.Value, idPedidoMovil, idConversacion, ct));
    }

    [Authorize]
    [HttpPost("chat")]
    public async Task<ActionResult<ChatMensajeDto>> EnviarChat([FromBody] EnviarChatRequest request, CancellationToken ct)
    {
        if (!_tenant.IsResolved) return BadRequest(new { error = "Falta tenant." });
        if (!_tenant.IDUsuarioEcommerce.HasValue) return Unauthorized();
        try
        {
            return Ok(await _engagement.EnviarChatAsync(_tenant.IDUsuarioEcommerce.Value, request, ct));
        }
        catch (Exception ex) when (ex is ArgumentException or InvalidOperationException)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [Authorize]
    [HttpPost("chat/upload")]
    [RequestSizeLimit(25_000_000)]
    public async Task<ActionResult<ChatMensajeDto>> EnviarChatFoto(
        IFormFile file,
        [FromForm] string? mensaje,
        [FromForm] string? asunto,
        [FromForm] long? idConversacion,
        [FromForm] long? idPedidoMovil,
        [FromForm] bool forzarNueva = false,
        CancellationToken ct = default)
    {
        if (!_tenant.IsResolved) return BadRequest(new { error = "Falta tenant." });
        if (!_tenant.IDUsuarioEcommerce.HasValue) return Unauthorized();
        if (file is null || file.Length == 0)
            return BadRequest(new { error = "Archivo requerido." });
        try
        {
            await using var stream = file.OpenReadStream();
            return Ok(await _engagement.EnviarChatConAdjuntoAsync(
                _tenant.IDUsuarioEcommerce.Value, stream, file.ContentType, file.FileName,
                mensaje, asunto ?? "consulta", idConversacion, idPedidoMovil, forzarNueva, ct));
        }
        catch (Exception ex) when (ex is ArgumentException or InvalidOperationException)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [Authorize]
    [HttpPost("chat/conversaciones/{id:long}/leidos")]
    public async Task<IActionResult> MarcarLeidos(long id, CancellationToken ct)
    {
        if (!_tenant.IsResolved) return BadRequest(new { error = "Falta tenant." });
        if (!_tenant.IDUsuarioEcommerce.HasValue) return Unauthorized();
        try
        {
            await _engagement.MarcarLeidosClienteAsync(_tenant.IDUsuarioEcommerce.Value, id, ct);
            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [Authorize]
    [HttpPost("chat/conversaciones/{id:long}/cerrar")]
    public async Task<IActionResult> CerrarConversacion(long id, CancellationToken ct)
    {
        if (!_tenant.IsResolved) return BadRequest(new { error = "Falta tenant." });
        if (!_tenant.IDUsuarioEcommerce.HasValue) return Unauthorized();
        try
        {
            await _engagement.CerrarConversacionClienteAsync(_tenant.IDUsuarioEcommerce.Value, id, ct);
            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    public record AplicarCuponBody(string Codigo, string? Canal = "B2C");
}
