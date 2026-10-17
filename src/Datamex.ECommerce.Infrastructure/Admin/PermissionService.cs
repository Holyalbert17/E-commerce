using Datamex.ECommerce.Application.Admin;
using Datamex.ECommerce.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Datamex.ECommerce.Infrastructure.Admin;

public sealed class PermissionService : IPermissionService
{
    private const string SuperPermission = "manager:all";

    private readonly EcomDbContext _db;

    public PermissionService(EcomDbContext db) => _db = db;

    public async Task<bool> HasPermissionAsync(
        long idEmpresa,
        long idUsuario,
        string permissionCode,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(permissionCode))
            return false;

        var codes = await ListPermissionCodesAsync(idEmpresa, idUsuario, ct);
        if (codes.Any(c => string.Equals(c, SuperPermission, StringComparison.OrdinalIgnoreCase)))
            return true;

        return codes.Any(c => string.Equals(c, permissionCode, StringComparison.OrdinalIgnoreCase));
    }

    public async Task<IReadOnlyList<string>> ListPermissionCodesAsync(
        long idEmpresa, long idUsuario, CancellationToken ct = default)
    {
        // Roles → operaciones (empresaroloperacion)
        var fromRoles = await _db.AdminUsuariosRoles
            .AsNoTracking()
            .Where(ur => ur.IDEmpresa == idEmpresa && ur.IDUsuario == idUsuario && ur.Activo)
            .Join(_db.AdminRolFuncionalidades.AsNoTracking().Where(rf => rf.Activo),
                ur => ur.IDRol,
                rf => rf.IDRol,
                (ur, rf) => rf)
            .Join(_db.AdminFuncionalidades.AsNoTracking().Where(f => f.Activo && f.IDEmpresa == idEmpresa),
                rf => rf.IDFuncionalidad,
                f => f.ID,
                (rf, f) => f.CodigoPermission)
            .ToListAsync(ct);

        // Grants directos (empresausuariooperacion)
        var fromUser = await _db.AdminUsuarioFuncionalidades
            .AsNoTracking()
            .Where(uf => uf.IDEmpresa == idEmpresa && uf.IDUsuario == idUsuario && uf.Activo)
            .Join(_db.AdminFuncionalidades.AsNoTracking().Where(f => f.Activo && f.IDEmpresa == idEmpresa),
                uf => uf.IDFuncionalidad,
                f => f.ID,
                (uf, f) => f.CodigoPermission)
            .ToListAsync(ct);

        return fromRoles
            .Concat(fromUser)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(c => c)
            .ToList();
    }
}
