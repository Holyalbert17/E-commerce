using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Datamex.ECommerce.Application.Abstractions;
using Datamex.ECommerce.Application.Admin;
using Datamex.ECommerce.Application.Options;
using Datamex.ECommerce.Infrastructure.Auth;
using Datamex.ECommerce.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace Datamex.ECommerce.Api.Controllers;

[ApiController]
[Route("api/admin/auth")]
public sealed class AdminAuthController : ControllerBase
{
    private readonly EcomDbContext _db;
    private readonly ITenantContext _tenant;
    private readonly IPermissionService _permissions;
    private readonly ECommerceOptions _options;

    public AdminAuthController(
        EcomDbContext db,
        ITenantContext tenant,
        IPermissionService permissions,
        IOptions<ECommerceOptions> options)
    {
        _db = db;
        _tenant = tenant;
        _permissions = permissions;
        _options = options.Value;
    }

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<ActionResult<AdminLoginResponse>> Login(
        [FromBody] AdminLoginRequest request,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Usuario) || string.IsNullOrWhiteSpace(request.Password))
            return BadRequest(new { error = "Usuario/Password requeridos." });

        if (!_tenant.IDEmpresa.HasValue)
            return BadRequest(new { error = "Tenant no resuelto." });

        var user = await _db.AdminUsuarios
            .AsNoTracking()
            .FirstOrDefaultAsync(u =>
                u.IDEmpresa == _tenant.IDEmpresa!.Value
                && u.Usuario == request.Usuario.Trim()
                && u.Activo,
                ct);

        if (user is null)
            return Unauthorized(new { error = "Credenciales inválidas." });

        if (!JwtAuthService.VerifyPassword(request.Password, user.PasswordHash))
            return Unauthorized(new { error = "Credenciales inválidas." });

        var expires = DateTime.UtcNow.AddMinutes(_options.JwtExpirationMinutes);
        var token = CreateToken(user.ID, user.IDEmpresa, user.Usuario, "Backoffice", expires);
        var perms = await _permissions.ListPermissionCodesAsync(user.IDEmpresa, user.ID, ct);
        var isSuper = perms.Any(p => string.Equals(p, "manager:all", StringComparison.OrdinalIgnoreCase));

        return new AdminLoginResponse(
            token,
            expires,
            user.IDEmpresa,
            user.ID,
            $"{user.Nombre ?? user.Usuario}".Trim(),
            "Backoffice",
            perms,
            isSuper);
    }

    [HttpGet("me")]
    [Authorize]
    public async Task<ActionResult<object>> Me(CancellationToken ct)
    {
        if (!_tenant.IDEmpresa.HasValue)
            return BadRequest(new { error = "Tenant no resuelto." });

        var sub = User.FindFirstValue(ClaimTypes.NameIdentifier)
                  ?? User.FindFirstValue(JwtRegisteredClaimNames.Sub);
        if (!long.TryParse(sub, out var userId))
            return Unauthorized();

        var user = await _db.AdminUsuarios.AsNoTracking()
            .FirstOrDefaultAsync(u => u.ID == userId && u.IDEmpresa == _tenant.IDEmpresa, ct);
        if (user is null)
            return Unauthorized();

        var perms = await _permissions.ListPermissionCodesAsync(user.IDEmpresa, user.ID, ct);
        var isSuper = perms.Any(p => string.Equals(p, "manager:all", StringComparison.OrdinalIgnoreCase));

        return Ok(new
        {
            id = user.ID,
            usuario = user.Usuario,
            nombre = user.Nombre ?? user.Usuario,
            idEmpresa = user.IDEmpresa,
            permissions = perms,
            isSuper
        });
    }

    private string CreateToken(long userId, long idEmpresa, string usuario, string canal, DateTime expires)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.JwtSigningKey));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, userId.ToString()),
            new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
            new Claim("IDEmpresa", idEmpresa.ToString()),
            new Claim(JwtRegisteredClaimNames.UniqueName, usuario),
            new Claim("canal", canal),
            new Claim("auth_ctx", "backoffice")
        };

        var jwt = new JwtSecurityToken(
            issuer: _options.JwtIssuer,
            audience: _options.JwtAudience,
            claims: claims,
            expires: expires,
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(jwt);
    }
}
