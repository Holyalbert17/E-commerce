using Datamex.ECommerce.Application.Admin;
using Datamex.ECommerce.Application.Abstractions;
using Datamex.ECommerce.Domain.Entities;
using Datamex.ECommerce.Infrastructure.Admin;
using Datamex.ECommerce.Infrastructure.Auth;
using Datamex.ECommerce.Infrastructure.Persistence;
using Datamex.ECommerce.Api.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Datamex.ECommerce.Api.Controllers;

/// <summary>
/// Manager backoffice alineado a ERP Seguridad (EmpresaRol / EmpresaUsuario / Operaciones).
/// </summary>
[ApiController]
[Route("api/admin/manager")]
[Authorize]
public sealed class ManagerController : ControllerBase
{
    private readonly EcomDbContext _db;
    private readonly ITenantContext _tenant;

    public ManagerController(EcomDbContext db, ITenantContext tenant)
    {
        _db = db;
        _tenant = tenant;
    }

    private long? EmpresaId => _tenant.IDEmpresa;

    [HttpGet("functionalidades")]
    [RequirePermission("manager:funcionalidades")]
    public async Task<ActionResult<IReadOnlyList<AdminFuncionalidadDto>>> Functionalities(CancellationToken ct)
    {
        if (!EmpresaId.HasValue) return BadRequest(new { error = "Tenant no resuelto." });

        var list = await _db.AdminFuncionalidades
            .AsNoTracking()
            .Where(f => f.IDEmpresa == EmpresaId.Value && f.Activo)
            .OrderBy(f => f.CodigoPermission)
            .Select(f => new AdminFuncionalidadDto(
                f.ID, f.CodigoPermission, f.Descripcion ?? string.Empty, f.IDFuncionalidadPadre, f.Orden))
            .ToListAsync(ct);

        return list;
    }

    [HttpGet("roles")]
    [RequirePermission("manager:roles")]
    public async Task<ActionResult<IReadOnlyList<AdminRoleDto>>> Roles(
        [FromQuery] bool includeInactive = true,
        CancellationToken ct = default)
    {
        if (!EmpresaId.HasValue) return BadRequest(new { error = "Tenant no resuelto." });
        var idEmpresa = EmpresaId.Value;

        var q = _db.AdminRoles.AsNoTracking().Where(r => r.IDEmpresa == idEmpresa);
        if (!includeInactive) q = q.Where(r => r.Activo);

        var roles = await q.OrderBy(r => r.CodigoRol).ToListAsync(ct);
        var roleIds = roles.Select(r => r.ID).ToList();

        var maps = await (
            from m in _db.AdminRolFuncionalidades.AsNoTracking()
            join f in _db.AdminFuncionalidades.AsNoTracking() on m.IDFuncionalidad equals f.ID
            where m.IDEmpresa == idEmpresa && m.Activo && roleIds.Contains(m.IDRol)
            select new { m.IDRol, f.CodigoPermission }
        ).ToListAsync(ct);

        var byRole = maps.GroupBy(x => x.IDRol)
            .ToDictionary(g => g.Key, g => (IReadOnlyList<string>)g.Select(x => x.CodigoPermission).OrderBy(c => c).ToList());

        return roles.Select(r =>
        {
            byRole.TryGetValue(r.ID, out var codes);
            codes ??= Array.Empty<string>();
            return new AdminRoleDto(
                r.ID, r.IDEmpresa, r.CodigoRol, r.NombreRol, r.Activo, r.UltModificacion,
                codes.Count, codes);
        }).ToList();
    }

    [HttpGet("roles/{idRol:long}")]
    [RequirePermission("manager:roles")]
    public async Task<ActionResult<AdminRoleDetailDto>> Role(long idRol, CancellationToken ct)
    {
        if (!EmpresaId.HasValue) return BadRequest(new { error = "Tenant no resuelto." });
        var idEmpresa = EmpresaId.Value;

        var r = await _db.AdminRoles.AsNoTracking()
            .FirstOrDefaultAsync(x => x.ID == idRol && x.IDEmpresa == idEmpresa, ct);
        if (r is null) return NotFound();

        var codes = await (
            from m in _db.AdminRolFuncionalidades.AsNoTracking()
            join f in _db.AdminFuncionalidades.AsNoTracking() on m.IDFuncionalidad equals f.ID
            where m.IDEmpresa == idEmpresa && m.IDRol == idRol && m.Activo
            orderby f.CodigoPermission
            select f.CodigoPermission
        ).ToListAsync(ct);

        return new AdminRoleDetailDto(
            r.ID, r.IDEmpresa, r.CodigoRol, r.NombreRol, r.Activo, r.UltModificacion, codes);
    }

    [HttpPost("roles")]
    [RequirePermission("manager:roles")]
    public async Task<ActionResult<AdminRoleDto>> CreateRole(
        [FromBody] AdminCreateRoleRequest request,
        CancellationToken ct)
    {
        if (!EmpresaId.HasValue) return BadRequest(new { error = "Tenant no resuelto." });
        var idEmpresa = EmpresaId.Value;

        var codigo = request.CodigoRol?.Trim() ?? string.Empty;
        var nombre = request.NombreRol?.Trim() ?? codigo;
        if (string.IsNullOrWhiteSpace(codigo))
            return BadRequest(new { error = "CodigoRol requerido." });

        var exists = await _db.AdminRoles.AnyAsync(r => r.IDEmpresa == idEmpresa && r.CodigoRol == codigo, ct);
        if (exists)
            return Conflict(new { error = $"Rol '{codigo}' ya existe." });

        var role = new AdminRol
        {
            IDEmpresa = idEmpresa,
            CodigoRol = codigo,
            NombreRol = nombre,
            Activo = true,
            UltModificacion = DateTime.UtcNow,
            UserName = User?.Identity?.Name
        };
        _db.AdminRoles.Add(role);
        await _db.SaveChangesAsync(ct);

        return new AdminRoleDto(role.ID, role.IDEmpresa, role.CodigoRol, role.NombreRol,
            role.Activo, role.UltModificacion, 0, Array.Empty<string>());
    }

    [HttpPut("roles/{idRol:long}")]
    [RequirePermission("manager:roles")]
    public async Task<ActionResult<AdminRoleDetailDto>> UpdateRole(
        long idRol, [FromBody] AdminUpdateRoleRequest request, CancellationToken ct)
    {
        if (!EmpresaId.HasValue) return BadRequest(new { error = "Tenant no resuelto." });
        var idEmpresa = EmpresaId.Value;

        var role = await _db.AdminRoles.FirstOrDefaultAsync(r => r.ID == idRol && r.IDEmpresa == idEmpresa, ct);
        if (role is null) return NotFound(new { error = "Rol no encontrado." });

        if (!string.IsNullOrWhiteSpace(request.NombreRol))
            role.NombreRol = request.NombreRol.Trim();
        if (request.Activo.HasValue)
            role.Activo = request.Activo.Value;

        role.UltModificacion = DateTime.UtcNow;
        role.UserName = User?.Identity?.Name;
        await _db.SaveChangesAsync(ct);

        return await Role(idRol, ct);
    }

    [HttpDelete("roles/{idRol:long}")]
    [RequirePermission("manager:roles")]
    public async Task<IActionResult> DeleteRole(long idRol, CancellationToken ct)
    {
        if (!EmpresaId.HasValue) return BadRequest(new { error = "Tenant no resuelto." });
        var idEmpresa = EmpresaId.Value;

        var role = await _db.AdminRoles.FirstOrDefaultAsync(r => r.ID == idRol && r.IDEmpresa == idEmpresa, ct);
        if (role is null) return NotFound(new { error = "Rol no encontrado." });

        if (string.Equals(role.CodigoRol, "SUPER", StringComparison.OrdinalIgnoreCase))
            return BadRequest(new { error = "No se puede eliminar el rol SUPER." });

        role.Activo = false;
        role.UltModificacion = DateTime.UtcNow;
        role.UserName = User?.Identity?.Name;
        await _db.SaveChangesAsync(ct);
        return NoContent();
    }

    /// <summary>Espejo SaveTree: reemplaza el set completo de permisos del rol.</summary>
    [HttpPut("roles/{idRol:long}/functionalidades")]
    [RequirePermission("manager:roles")]
    public async Task<ActionResult<AdminRoleDetailDto>> SaveRolePermissions(
        long idRol, [FromBody] AssignPermissionsRequest request, CancellationToken ct)
    {
        if (!EmpresaId.HasValue) return BadRequest(new { error = "Tenant no resuelto." });
        var idEmpresa = EmpresaId.Value;

        var role = await _db.AdminRoles.FirstOrDefaultAsync(r => r.ID == idRol && r.IDEmpresa == idEmpresa, ct);
        if (role is null) return NotFound(new { error = "Rol no encontrado." });

        var codes = (request.PermissionCodes ?? Array.Empty<string>())
            .Select(c => c?.Trim())
            .Where(c => !string.IsNullOrWhiteSpace(c))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Cast<string>()
            .ToArray();

        var funcIds = new List<long>();
        foreach (var code in codes)
        {
            var func = await _db.AdminFuncionalidades
                .FirstOrDefaultAsync(f => f.IDEmpresa == idEmpresa && f.CodigoPermission == code, ct);
            if (func is null)
            {
                func = new AdminFuncionalidad
                {
                    IDEmpresa = idEmpresa,
                    CodigoPermission = code,
                    Descripcion = code,
                    Activo = true,
                    UltModificacion = DateTime.UtcNow,
                    UserName = User?.Identity?.Name
                };
                _db.AdminFuncionalidades.Add(func);
                await _db.SaveChangesAsync(ct);
            }
            else if (!func.Activo)
            {
                func.Activo = true;
                func.UltModificacion = DateTime.UtcNow;
            }
            funcIds.Add(func.ID);
        }

        var existing = await _db.AdminRolFuncionalidades
            .Where(m => m.IDEmpresa == idEmpresa && m.IDRol == idRol)
            .ToListAsync(ct);

        foreach (var m in existing)
        {
            if (funcIds.Contains(m.IDFuncionalidad))
            {
                m.Activo = true;
                m.UltModificacion = DateTime.UtcNow;
            }
            else
            {
                m.Activo = false;
                m.UltModificacion = DateTime.UtcNow;
            }
        }

        var existingFuncIds = existing.Select(m => m.IDFuncionalidad).ToHashSet();
        foreach (var fid in funcIds.Where(id => !existingFuncIds.Contains(id)))
        {
            _db.AdminRolFuncionalidades.Add(new AdminRolFuncionalidad
            {
                IDEmpresa = idEmpresa,
                IDRol = idRol,
                IDFuncionalidad = fid,
                Activo = true,
                UltModificacion = DateTime.UtcNow,
                UserName = User?.Identity?.Name
            });
        }

        await _db.SaveChangesAsync(ct);
        return await Role(idRol, ct);
    }

    /// <summary>Compatibilidad con cliente anterior (append). Preferir PUT SaveTree.</summary>
    [HttpPost("roles/{idRol:long}/functionalidades")]
    [RequirePermission("manager:roles")]
    public Task<ActionResult<AdminRoleDetailDto>> AssignPermissionsToRole(
        long idRol, [FromBody] AssignPermissionsRequest request, CancellationToken ct)
        => SaveRolePermissions(idRol, request, ct);

    /// <summary>Espejo CopyEmpresaRolOperacion.</summary>
    [HttpPost("roles/{idRolDestino:long}/copiar-permisos")]
    [RequirePermission("manager:roles")]
    public async Task<ActionResult<AdminRoleDetailDto>> CopyRolePermissions(
        long idRolDestino, [FromBody] CopyRolePermissionsRequest request, CancellationToken ct)
    {
        if (!EmpresaId.HasValue) return BadRequest(new { error = "Tenant no resuelto." });
        var idEmpresa = EmpresaId.Value;

        if (request.IDRolOrigen == idRolDestino)
            return BadRequest(new { error = "Origen y destino deben ser distintos." });

        var destOk = await _db.AdminRoles.AnyAsync(r => r.ID == idRolDestino && r.IDEmpresa == idEmpresa, ct);
        if (!destOk) return NotFound(new { error = "Rol destino no encontrado." });

        var codes = await (
            from m in _db.AdminRolFuncionalidades.AsNoTracking()
            join f in _db.AdminFuncionalidades.AsNoTracking() on m.IDFuncionalidad equals f.ID
            where m.IDEmpresa == idEmpresa && m.IDRol == request.IDRolOrigen && m.Activo
            select f.CodigoPermission
        ).ToListAsync(ct);

        return await SaveRolePermissions(idRolDestino, new AssignPermissionsRequest(idRolDestino, codes), ct);
    }

    [HttpGet("usuarios")]
    [RequirePermission("manager:usuarios")]
    public async Task<ActionResult<IReadOnlyList<AdminUserDto>>> Users(
        [FromQuery] bool includeInactive = true,
        CancellationToken ct = default)
    {
        if (!EmpresaId.HasValue) return BadRequest(new { error = "Tenant no resuelto." });
        var idEmpresa = EmpresaId.Value;

        var q = _db.AdminUsuarios.AsNoTracking().Where(u => u.IDEmpresa == idEmpresa);
        if (!includeInactive) q = q.Where(u => u.Activo);

        var users = await q.OrderBy(u => u.Usuario).ToListAsync(ct);
        var userIds = users.Select(u => u.ID).ToList();

        var roleMaps = await (
            from ur in _db.AdminUsuariosRoles.AsNoTracking()
            join r in _db.AdminRoles.AsNoTracking() on ur.IDRol equals r.ID
            where ur.IDEmpresa == idEmpresa && ur.Activo && userIds.Contains(ur.IDUsuario)
            select new { ur.IDUsuario, Role = new AdminUserRoleDto(r.ID, r.CodigoRol, r.NombreRol) }
        ).ToListAsync(ct);

        var byUser = roleMaps.GroupBy(x => x.IDUsuario)
            .ToDictionary(g => g.Key, g => (IReadOnlyList<AdminUserRoleDto>)g.Select(x => x.Role).ToList());

        return users.Select(u =>
        {
            byUser.TryGetValue(u.ID, out var roles);
            return new AdminUserDto(
                u.ID, u.IDEmpresa, u.Usuario, u.Nombre, u.Email, u.Activo, u.UltModificacion,
                roles ?? Array.Empty<AdminUserRoleDto>());
        }).ToList();
    }

    [HttpPost("usuarios")]
    [RequirePermission("manager:usuarios")]
    public async Task<ActionResult<AdminUserDto>> CreateUser(
        [FromBody] AdminCreateUserRequest request,
        CancellationToken ct)
    {
        if (!EmpresaId.HasValue) return BadRequest(new { error = "Tenant no resuelto." });
        var idEmpresa = EmpresaId.Value;

        var usuario = request.Usuario?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(usuario))
            return BadRequest(new { error = "Usuario requerido." });
        if (string.IsNullOrWhiteSpace(request.Password))
            return BadRequest(new { error = "Password requerida." });

        var exists = await _db.AdminUsuarios.AnyAsync(u => u.IDEmpresa == idEmpresa && u.Usuario == usuario, ct);
        if (exists)
            return Conflict(new { error = $"Usuario '{usuario}' ya existe." });

        var user = new AdminUsuario
        {
            IDEmpresa = idEmpresa,
            Usuario = usuario,
            PasswordHash = JwtAuthService.HashPassword(request.Password),
            Nombre = request.Nombre?.Trim(),
            Email = request.Email?.Trim(),
            Activo = true,
            UltModificacion = DateTime.UtcNow,
            UserName = User?.Identity?.Name
        };

        _db.AdminUsuarios.Add(user);
        await _db.SaveChangesAsync(ct);

        return new AdminUserDto(user.ID, user.IDEmpresa, user.Usuario, user.Nombre, user.Email,
            user.Activo, user.UltModificacion, Array.Empty<AdminUserRoleDto>());
    }

    [HttpPut("usuarios/{idUsuario:long}")]
    [RequirePermission("manager:usuarios")]
    public async Task<ActionResult<AdminUserDto>> UpdateUser(
        long idUsuario, [FromBody] AdminUpdateUserRequest request, CancellationToken ct)
    {
        if (!EmpresaId.HasValue) return BadRequest(new { error = "Tenant no resuelto." });
        var idEmpresa = EmpresaId.Value;

        var user = await _db.AdminUsuarios.FirstOrDefaultAsync(u => u.ID == idUsuario && u.IDEmpresa == idEmpresa, ct);
        if (user is null) return NotFound(new { error = "Usuario no encontrado." });

        if (request.Nombre is not null) user.Nombre = string.IsNullOrWhiteSpace(request.Nombre) ? null : request.Nombre.Trim();
        if (request.Email is not null) user.Email = string.IsNullOrWhiteSpace(request.Email) ? null : request.Email.Trim();
        if (!string.IsNullOrWhiteSpace(request.Password))
            user.PasswordHash = JwtAuthService.HashPassword(request.Password);
        if (request.Activo.HasValue) user.Activo = request.Activo.Value;

        user.UltModificacion = DateTime.UtcNow;
        user.UserName = User?.Identity?.Name;
        await _db.SaveChangesAsync(ct);

        var list = await Users(includeInactive: true, ct);
        var dto = list.Value?.FirstOrDefault(u => u.Id == idUsuario);
        return dto is null ? NotFound() : Ok(dto);
    }

    [HttpDelete("usuarios/{idUsuario:long}")]
    [RequirePermission("manager:usuarios")]
    public async Task<IActionResult> DeleteUser(long idUsuario, CancellationToken ct)
    {
        if (!EmpresaId.HasValue) return BadRequest(new { error = "Tenant no resuelto." });
        var idEmpresa = EmpresaId.Value;

        var user = await _db.AdminUsuarios.FirstOrDefaultAsync(u => u.ID == idUsuario && u.IDEmpresa == idEmpresa, ct);
        if (user is null) return NotFound(new { error = "Usuario no encontrado." });

        if (string.Equals(user.Usuario, "admin", StringComparison.OrdinalIgnoreCase))
            return BadRequest(new { error = "No se puede desactivar el usuario admin." });

        user.Activo = false;
        user.UltModificacion = DateTime.UtcNow;
        user.UserName = User?.Identity?.Name;
        await _db.SaveChangesAsync(ct);
        return NoContent();
    }

    [HttpPost("usuarios/{idUsuario:long}/roles")]
    [RequirePermission("manager:usuarios")]
    public async Task<IActionResult> AssignRolesToUser(
        long idUsuario, [FromBody] AssignRoleRequest request, CancellationToken ct)
    {
        if (!EmpresaId.HasValue) return BadRequest(new { error = "Tenant no resuelto." });
        var idEmpresa = EmpresaId.Value;

        var userExists = await _db.AdminUsuarios.AnyAsync(u => u.ID == idUsuario && u.IDEmpresa == idEmpresa, ct);
        if (!userExists) return NotFound(new { error = "Usuario no encontrado." });

        var roleExists = await _db.AdminRoles.AnyAsync(r => r.ID == request.IDRol && r.IDEmpresa == idEmpresa && r.Activo, ct);
        if (!roleExists) return NotFound(new { error = "Rol no encontrado." });

        var mapping = await _db.AdminUsuariosRoles
            .FirstOrDefaultAsync(m => m.IDEmpresa == idEmpresa && m.IDUsuario == idUsuario && m.IDRol == request.IDRol, ct);

        if (mapping is null)
        {
            _db.AdminUsuariosRoles.Add(new AdminUsuarioRol
            {
                IDEmpresa = idEmpresa,
                IDUsuario = idUsuario,
                IDRol = request.IDRol,
                Activo = true,
                UltModificacion = DateTime.UtcNow,
                UserName = User?.Identity?.Name
            });
        }
        else
        {
            mapping.Activo = true;
            mapping.UltModificacion = DateTime.UtcNow;
            mapping.UserName = User?.Identity?.Name;
        }

        await _db.SaveChangesAsync(ct);
        return Ok();
    }

    [HttpDelete("usuarios/{idUsuario:long}/roles/{idRol:long}")]
    [RequirePermission("manager:usuarios")]
    public async Task<IActionResult> UnassignRoleFromUser(long idUsuario, long idRol, CancellationToken ct)
    {
        if (!EmpresaId.HasValue) return BadRequest(new { error = "Tenant no resuelto." });
        var idEmpresa = EmpresaId.Value;

        var mapping = await _db.AdminUsuariosRoles
            .FirstOrDefaultAsync(m => m.IDEmpresa == idEmpresa && m.IDUsuario == idUsuario && m.IDRol == idRol, ct);
        if (mapping is null) return NotFound(new { error = "Asignación no encontrada." });

        mapping.Activo = false;
        mapping.UltModificacion = DateTime.UtcNow;
        mapping.UserName = User?.Identity?.Name;
        await _db.SaveChangesAsync(ct);
        return NoContent();
    }

    /// <summary>
    /// Espejo GetDataTreeEditPermisosRol — árbol de operaciones del usuario.
    /// Grants de rol = Source R (verde / no editable); directos = Source P.
    /// </summary>
    [HttpGet("usuarios/{idUsuario:long}/operaciones-tree")]
    [RequirePermission("manager:usuarios")]
    public async Task<ActionResult<AdminUserPermissionTreeDto>> GetUserPermissionTree(
        long idUsuario, CancellationToken ct)
    {
        if (!EmpresaId.HasValue) return BadRequest(new { error = "Tenant no resuelto." });
        var idEmpresa = EmpresaId.Value;

        var user = await _db.AdminUsuarios.AsNoTracking()
            .FirstOrDefaultAsync(u => u.ID == idUsuario && u.IDEmpresa == idEmpresa, ct);
        if (user is null) return NotFound(new { error = "Usuario no encontrado." });

        var catalog = await _db.AdminFuncionalidades
            .Where(f => f.IDEmpresa == idEmpresa && f.Activo)
            .ToListAsync(ct);

        // Re-cablear padres si el catálogo quedó plano (tenants viejos).
        if (catalog.Count > 0 && catalog.All(f => f.IDFuncionalidadPadre is null))
        {
            var tracked = await _db.AdminFuncionalidades
                .Where(f => f.IDEmpresa == idEmpresa && f.Activo)
                .ToListAsync(ct);
            AdminPermissionTreeBuilder.WireParentsByCodePrefix(tracked);
            await _db.SaveChangesAsync(ct);
            catalog = tracked;
        }

        var roleCodes = await (
            from ur in _db.AdminUsuariosRoles.AsNoTracking()
            join r in _db.AdminRoles.AsNoTracking() on ur.IDRol equals r.ID
            where ur.IDEmpresa == idEmpresa && ur.IDUsuario == idUsuario && ur.Activo
            select r.CodigoRol
        ).ToListAsync(ct);

        var fromRole = (await (
            from ur in _db.AdminUsuariosRoles.AsNoTracking()
            join rf in _db.AdminRolFuncionalidades.AsNoTracking() on ur.IDRol equals rf.IDRol
            join f in _db.AdminFuncionalidades.AsNoTracking() on rf.IDFuncionalidad equals f.ID
            where ur.IDEmpresa == idEmpresa && ur.IDUsuario == idUsuario && ur.Activo && rf.Activo && f.Activo
            select f.CodigoPermission
        ).ToListAsync(ct)).ToHashSet(StringComparer.OrdinalIgnoreCase);

        // manager:all (SUPER) = todas las operaciones vía rol, igual ERP con rol full.
        if (fromRole.Contains("manager:all"))
        {
            foreach (var f in catalog)
                fromRole.Add(f.CodigoPermission);
        }

        var fromDirect = (await (
            from uf in _db.AdminUsuarioFuncionalidades.AsNoTracking()
            join f in _db.AdminFuncionalidades.AsNoTracking() on uf.IDFuncionalidad equals f.ID
            where uf.IDEmpresa == idEmpresa && uf.IDUsuario == idUsuario && uf.Activo && f.Activo
            select f.CodigoPermission
        ).ToListAsync(ct)).ToHashSet(StringComparer.OrdinalIgnoreCase);

        var tree = AdminPermissionTreeBuilder.Build(catalog, fromDirect, fromRole);
        return new AdminUserPermissionTreeDto(user.ID, user.Usuario, roleCodes, tree);
    }

    /// <summary>
    /// Espejo SaveTreeEmpresaUsuarioOperaciones — reemplaza grants directos (no toca rol).
    /// </summary>
    [HttpPut("usuarios/{idUsuario:long}/operaciones")]
    [RequirePermission("manager:usuarios")]
    public async Task<ActionResult<AdminUserPermissionTreeDto>> SaveUserPermissions(
        long idUsuario, [FromBody] SaveUserPermissionsRequest request, CancellationToken ct)
    {
        if (!EmpresaId.HasValue) return BadRequest(new { error = "Tenant no resuelto." });
        var idEmpresa = EmpresaId.Value;

        var userOk = await _db.AdminUsuarios.AnyAsync(u => u.ID == idUsuario && u.IDEmpresa == idEmpresa, ct);
        if (!userOk) return NotFound(new { error = "Usuario no encontrado." });

        var byId = await _db.AdminFuncionalidades
            .Where(f => f.IDEmpresa == idEmpresa && f.Activo)
            .ToDictionaryAsync(f => f.ID, ct);

        var selected = (request.IDFuncionalidades ?? Array.Empty<long>())
            .Where(id => byId.ContainsKey(id))
            .Distinct()
            .ToList();

        // No persistir grants que ya vienen solo de rol (ERP: unselectable R).
        var fromRoleCodes = await (
            from ur in _db.AdminUsuariosRoles.AsNoTracking()
            join rf in _db.AdminRolFuncionalidades.AsNoTracking() on ur.IDRol equals rf.IDRol
            join f in _db.AdminFuncionalidades.AsNoTracking() on rf.IDFuncionalidad equals f.ID
            where ur.IDEmpresa == idEmpresa && ur.IDUsuario == idUsuario && ur.Activo && rf.Activo && f.Activo
            select f.CodigoPermission
        ).Distinct().ToListAsync(ct);

        HashSet<long> roleSet;
        if (fromRoleCodes.Any(c => string.Equals(c, "manager:all", StringComparison.OrdinalIgnoreCase)))
        {
            roleSet = byId.Keys.ToHashSet();
        }
        else
        {
            var fromRoleIds = await (
                from ur in _db.AdminUsuariosRoles.AsNoTracking()
                join rf in _db.AdminRolFuncionalidades.AsNoTracking() on ur.IDRol equals rf.IDRol
                where ur.IDEmpresa == idEmpresa && ur.IDUsuario == idUsuario && ur.Activo && rf.Activo
                select rf.IDFuncionalidad
            ).Distinct().ToListAsync(ct);
            roleSet = fromRoleIds.ToHashSet();
        }
        selected = selected.Where(id => !roleSet.Contains(id)).ToList();

        var expanded = AdminPermissionTreeBuilder.ExpandWithAncestors(selected, byId);
        expanded.RemoveWhere(roleSet.Contains);

        var existing = await _db.AdminUsuarioFuncionalidades
            .Where(m => m.IDEmpresa == idEmpresa && m.IDUsuario == idUsuario)
            .ToListAsync(ct);

        foreach (var m in existing)
        {
            if (expanded.Contains(m.IDFuncionalidad))
            {
                m.Activo = true;
                m.UltModificacion = DateTime.UtcNow;
                m.UserName = User?.Identity?.Name;
                expanded.Remove(m.IDFuncionalidad);
            }
            else
            {
                m.Activo = false;
                m.UltModificacion = DateTime.UtcNow;
                m.UserName = User?.Identity?.Name;
            }
        }

        foreach (var fid in expanded)
        {
            _db.AdminUsuarioFuncionalidades.Add(new AdminUsuarioFuncionalidad
            {
                IDEmpresa = idEmpresa,
                IDUsuario = idUsuario,
                IDFuncionalidad = fid,
                Activo = true,
                UltModificacion = DateTime.UtcNow,
                UserName = User?.Identity?.Name
            });
        }

        await _db.SaveChangesAsync(ct);
        return await GetUserPermissionTree(idUsuario, ct);
    }

    /// <summary>Árbol de catálogo para roles (misma estructura FancyTree).</summary>
    [HttpGet("operaciones-tree")]
    [RequirePermission("manager:roles")]
    public async Task<ActionResult<IReadOnlyList<AdminPermissionTreeNodeDto>>> GetCatalogTree(
        CancellationToken ct)
    {
        if (!EmpresaId.HasValue) return BadRequest(new { error = "Tenant no resuelto." });
        var idEmpresa = EmpresaId.Value;

        var catalog = await _db.AdminFuncionalidades
            .Where(f => f.IDEmpresa == idEmpresa && f.Activo)
            .ToListAsync(ct);
        if (catalog.Count > 0 && catalog.All(f => f.IDFuncionalidadPadre is null))
        {
            AdminPermissionTreeBuilder.WireParentsByCodePrefix(catalog);
            await _db.SaveChangesAsync(ct);
        }

        var empty = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        return Ok(AdminPermissionTreeBuilder.Build(catalog, empty, empty));
    }
}
