using System.Security.Claims;
using Datamex.ECommerce.Application.Abstractions;
using Datamex.ECommerce.Application.Options;
using Datamex.ECommerce.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Datamex.ECommerce.Api.Middleware;

/// <summary>
/// Resuelve IDEmpresa desde: claim JWT → header X-Tenant-ID → header X-Tenant-Slug → query tenant.
/// </summary>
public sealed class TenantResolutionMiddleware
{
    private readonly RequestDelegate _next;

    public TenantResolutionMiddleware(RequestDelegate next) => _next = next;

    public async Task InvokeAsync(
        HttpContext context,
        ITenantContext tenant,
        EcomDbContext db,
        IOptions<ECommerceOptions> options)
    {
        var opts = options.Value;

        // 1) JWT claim
        var claimEmpresa = context.User.FindFirst("IDEmpresa")?.Value
                           ?? context.User.FindFirst("idempresa")?.Value;
        if (long.TryParse(claimEmpresa, out var fromClaim))
        {
            tenant.Set(fromClaim);
            SetUserFromJwt(context, tenant);
            await _next(context);
            return;
        }

        // 2) Header numérico
        if (context.Request.Headers.TryGetValue(opts.DefaultTenantHeader, out var headerId)
            && long.TryParse(headerId.FirstOrDefault(), out var fromHeader))
        {
            tenant.Set(fromHeader);
            SetUserFromJwt(context, tenant);
            await _next(context);
            return;
        }

        // 3) Slug (header → query → DefaultTenantSlug). Legacy `demo` → Casa Imperial.
        var rawSlug = context.Request.Headers[opts.DefaultTenantSlugHeader].FirstOrDefault()
                   ?? context.Request.Query["tenant"].FirstOrDefault();
        var slugFromClient = !string.IsNullOrWhiteSpace(rawSlug);
        var slug = string.IsNullOrWhiteSpace(rawSlug) ? opts.DefaultTenantSlug : rawSlug.Trim();
        if (string.Equals(slug, "demo", StringComparison.OrdinalIgnoreCase))
            slug = string.IsNullOrWhiteSpace(opts.DefaultTenantSlug) ? "casa-imperial" : opts.DefaultTenantSlug;

        if (!string.IsNullOrWhiteSpace(slug))
        {
            var id = await db.EmpresaConfiguracionesEcommerce
                .AsNoTracking()
                .Where(c => c.SlugTienda == slug && c.Activo)
                .Select(c => (long?)c.ID)
                .FirstOrDefaultAsync(context.RequestAborted);

            if (id.HasValue)
            {
                tenant.Set(id.Value, slug);
                SetUserFromJwt(context, tenant);
                await _next(context);
                return;
            }

            // Solo 404 si el cliente pidió un slug explícito (ya no demo) y no existe.
            if (slugFromClient && !string.Equals(rawSlug, "demo", StringComparison.OrdinalIgnoreCase))
            {
                context.Response.StatusCode = StatusCodes.Status404NotFound;
                await context.Response.WriteAsJsonAsync(new { error = $"Tenant slug '{slug}' no encontrado." });
                return;
            }

            // demo u otro legacy sin fila: intentar default operativo.
            if (!string.Equals(slug, opts.DefaultTenantSlug, StringComparison.OrdinalIgnoreCase)
                && !string.IsNullOrWhiteSpace(opts.DefaultTenantSlug))
            {
                var fallbackId = await db.EmpresaConfiguracionesEcommerce
                    .AsNoTracking()
                    .Where(c => c.SlugTienda == opts.DefaultTenantSlug && c.Activo)
                    .Select(c => (long?)c.ID)
                    .FirstOrDefaultAsync(context.RequestAborted);
                if (fallbackId.HasValue)
                {
                    tenant.Set(fallbackId.Value, opts.DefaultTenantSlug);
                    SetUserFromJwt(context, tenant);
                    await _next(context);
                    return;
                }
            }
        }

        SetUserFromJwt(context, tenant);
        await _next(context);
    }

    private static void SetUserFromJwt(HttpContext context, ITenantContext tenant)
    {
        var sub = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                  ?? context.User.FindFirst("sub")?.Value;
        if (!long.TryParse(sub, out var userId))
            return;

        var canal = context.User.FindFirst("canal")?.Value;
        var authCtx = context.User.FindFirst("auth_ctx")?.Value;
        if (string.Equals(canal, "Backoffice", StringComparison.OrdinalIgnoreCase)
            || string.Equals(authCtx, "backoffice", StringComparison.OrdinalIgnoreCase))
        {
            tenant.SetAdminUser(userId);
        }
        else
        {
            tenant.SetUser(userId);
        }
    }
}
