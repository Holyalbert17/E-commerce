using Datamex.ECommerce.Application.Abstractions;
using Datamex.ECommerce.Application.Audit;
using Datamex.ECommerce.Domain.Entities;
using Datamex.ECommerce.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Datamex.ECommerce.Infrastructure.Audit;

public sealed class AdminAuditService : IAdminAuditService
{
    private readonly EcomDbContext _db;
    private readonly ITenantContext _tenant;

    public AdminAuditService(EcomDbContext db, ITenantContext tenant)
    {
        _db = db;
        _tenant = tenant;
    }

    public async Task LogAsync(
        string accion,
        string entidad,
        string? entidadId,
        string? detalle,
        long? idAdminUsuario,
        string? usuario,
        string? ip,
        CancellationToken ct = default)
    {
        if (!_tenant.IsResolved || !_tenant.IDEmpresa.HasValue)
            return;

        _db.AdminAuditoriaLogs.Add(new AdminAuditoriaLog
        {
            IDEmpresa = _tenant.IDEmpresa.Value,
            IDAdminUsuario = idAdminUsuario,
            Usuario = Trunc(usuario ?? "sistema", 120),
            Accion = Trunc(accion, 80),
            Entidad = Trunc(entidad, 80),
            EntidadId = entidadId is null ? null : Trunc(entidadId, 80),
            Detalle = detalle is null ? null : Trunc(detalle, 1000),
            Fecha = DateTime.UtcNow,
            Ip = ip is null ? null : Trunc(ip, 64),
            Activo = true,
            UltModificacion = DateTime.UtcNow,
            UserName = usuario ?? "audit"
        });
        await _db.SaveChangesAsync(ct);
    }

    public async Task<IReadOnlyList<AdminAuditoriaDto>> ListAsync(int take = 100, CancellationToken ct = default)
    {
        var idEmpresa = RequireTenant();
        take = Math.Clamp(take, 1, 500);
        return await _db.AdminAuditoriaLogs.AsNoTracking()
            .Where(x => x.IDEmpresa == idEmpresa && x.Activo)
            .OrderByDescending(x => x.Fecha)
            .Take(take)
            .Select(x => new AdminAuditoriaDto(x.ID, x.Usuario, x.Accion, x.Entidad, x.EntidadId, x.Detalle, x.Fecha))
            .ToListAsync(ct);
    }

    private long RequireTenant()
    {
        if (!_tenant.IsResolved || !_tenant.IDEmpresa.HasValue)
            throw new InvalidOperationException("Tenant no resuelto.");
        return _tenant.IDEmpresa.Value;
    }

    private static string Trunc(string s, int max) => s.Length <= max ? s : s[..max];
}
