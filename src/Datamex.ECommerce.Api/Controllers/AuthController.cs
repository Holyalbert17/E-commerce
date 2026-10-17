using Datamex.ECommerce.Application.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Datamex.ECommerce.Api.Controllers;

[ApiController]
[Route("api/auth")]
[EnableRateLimiting("auth")]
public sealed class AuthController : ControllerBase
{
    private readonly IAuthService _auth;

    public AuthController(IAuthService auth) => _auth = auth;

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<ActionResult<TokenResponse>> Login([FromBody] LoginRequest request, CancellationToken ct)
    {
        try
        {
            var result = await _auth.LoginAsync(request, ct);
            if (result is null)
                return Unauthorized(new { error = "Usuario o contraseña incorrectos." });

            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return Unauthorized(new { error = ex.Message });
        }
    }

    /// <summary>Registro espejo SignUpDTO ERP. Devuelve activationUrlDev (también en logs).</summary>
    [HttpPost("register")]
    [AllowAnonymous]
    public async Task<ActionResult<SignUpResult>> Register([FromBody] SignUpRequest request, CancellationToken ct)
    {
        try
        {
            return Ok(await _auth.RegisterAsync(request, ct));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (Microsoft.EntityFrameworkCore.DbUpdateException)
        {
            return Conflict(new
            {
                error = "No se pudo crear el usuario (conflicto de ID o dato duplicado). Reintentá; si persiste, avisar a soporte."
            });
        }
    }

    [HttpPost("activate")]
    [AllowAnonymous]
    public async Task<ActionResult<TokenResponse>> Activate([FromBody] ActivateRequest request, CancellationToken ct)
    {
        try
        {
            var result = await _auth.ActivateAsync(request, ct);
            if (result is null)
                return BadRequest(new { error = "Código de activación inválido o ya usado." });
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception)
        {
            return StatusCode(500, new
            {
                error = "No se pudo activar la cuenta. Reintentá; si persiste, pedí un reenvío del enlace."
            });
        }
    }

    [HttpPost("resend-activation")]
    [AllowAnonymous]
    public async Task<ActionResult<ResendActivationResult>> ResendActivation(
        [FromBody] ResendActivationRequest request, CancellationToken ct)
    {
        return Ok(await _auth.ResendActivationAsync(request, ct));
    }

    [HttpGet("activate")]
    [AllowAnonymous]
    public async Task<ActionResult<TokenResponse>> ActivateGet(
        [FromQuery] string c, [FromQuery] string k, [FromQuery] string? tenant, CancellationToken ct)
    {
        try
        {
            var result = await _auth.ActivateAsync(new ActivateRequest(c, k, tenant), ct);
            if (result is null)
                return BadRequest(new { error = "Código de activación inválido o ya usado." });
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception)
        {
            return StatusCode(500, new
            {
                error = "No se pudo activar la cuenta. Reintentá; si persiste, pedí un reenvío del enlace."
            });
        }
    }

    [HttpPost("forgot-password")]
    [AllowAnonymous]
    public async Task<ActionResult<ForgotPasswordResult>> ForgotPassword(
        [FromBody] ForgotPasswordRequest request, CancellationToken ct)
    {
        return Ok(await _auth.ForgotPasswordAsync(request, ct));
    }

    [HttpPost("reset-password")]
    [AllowAnonymous]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest request, CancellationToken ct)
    {
        try
        {
            await _auth.ResetPasswordAsync(request, ct);
            return Ok(new { mensaje = "Contraseña actualizada. Ya podés iniciar sesión." });
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

    [HttpPost("change-password")]
    [Authorize]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request, CancellationToken ct)
    {
        var sub = User.FindFirst("sub")?.Value ?? User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (!long.TryParse(sub, out var userId))
            return Unauthorized(new { error = "Usuario no autenticado." });

        try
        {
            await _auth.ChangePasswordAsync(userId, request, ct);
            return Ok(new { mensaje = "Contraseña actualizada." });
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
}
