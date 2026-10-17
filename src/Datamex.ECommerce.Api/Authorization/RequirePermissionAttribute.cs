using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Datamex.ECommerce.Application.Admin;
using Datamex.ECommerce.Application.Abstractions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Datamex.ECommerce.Api.Authorization;

/// <summary>Exige al menos uno de los códigos (OR). <c>manager:all</c> lo resuelve PermissionService.</summary>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = true)]
public sealed class RequirePermissionAttribute : Attribute, IAsyncActionFilter
{
    private readonly string[] _permissionCodes;

    public RequirePermissionAttribute(params string[] permissionCodes)
    {
        _permissionCodes = permissionCodes?
            .Where(c => !string.IsNullOrWhiteSpace(c))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray() ?? Array.Empty<string>();
    }

    public async Task OnActionExecutionAsync(
        ActionExecutingContext context,
        ActionExecutionDelegate next)
    {
        var tenant = context.HttpContext.RequestServices.GetRequiredService<ITenantContext>();
        if (!tenant.IsResolved || !tenant.IDEmpresa.HasValue)
        {
            context.Result = new ForbidResult();
            return;
        }

        var permissionService = context.HttpContext.RequestServices.GetRequiredService<IPermissionService>();

        var sub = context.HttpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                  ?? context.HttpContext.User.FindFirst("sub")?.Value
                  ?? context.HttpContext.User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
        if (!long.TryParse(sub, out var idUsuario))
        {
            context.Result = new UnauthorizedObjectResult(new { error = "Sesión inválida. Volvé a iniciar sesión." });
            return;
        }

        if (_permissionCodes.Length == 0)
        {
            context.Result = new ObjectResult(new { error = "Sin permiso configurado." })
            {
                StatusCode = StatusCodes.Status403Forbidden
            };
            return;
        }

        var idEmpresa = tenant.IDEmpresa.Value;
        var ct = context.HttpContext.RequestAborted;
        var ok = false;
        string? missing = null;
        foreach (var code in _permissionCodes)
        {
            if (await permissionService.HasPermissionAsync(idEmpresa, idUsuario, code, ct))
            {
                ok = true;
                break;
            }
            missing ??= code;
        }

        if (!ok)
        {
            context.Result = new ObjectResult(new { error = $"Sin permiso: {missing}" })
            {
                StatusCode = StatusCodes.Status403Forbidden
            };
            return;
        }

        await next();
    }
}
