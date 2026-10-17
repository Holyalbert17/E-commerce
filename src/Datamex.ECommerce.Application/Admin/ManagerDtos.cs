namespace Datamex.ECommerce.Application.Admin;

public record AdminLoginRequest(string Usuario, string Password);

/// <summary>Login backoffice con permisos para filtrar nav en el SPA.</summary>
public record AdminLoginResponse(
    string AccessToken,
    DateTime ExpiresAtUtc,
    long IDEmpresa,
    long IDUsuarioEcommerce,
    string Nombre,
    string CanalPreferido,
    IReadOnlyList<string> Permissions,
    bool IsSuper);

public record AdminCreateUserRequest(string Usuario, string Password, string? Nombre = null, string? Email = null);

public record AdminUpdateUserRequest(
    string? Nombre = null,
    string? Email = null,
    string? Password = null,
    bool? Activo = null);

public record AdminCreateRoleRequest(string CodigoRol, string NombreRol);

public record AdminUpdateRoleRequest(string? NombreRol = null, bool? Activo = null);

public record AdminFuncionalidadDto(
    long Id,
    string CodigoPermission,
    string Descripcion,
    long? IDFuncionalidadPadre = null,
    int Orden = 0);

/// <summary>Nodo FancyTree-compatible (TreeEmpresaUsuarioOperacion).</summary>
public record AdminPermissionTreeNodeDto(
    string Key,
    string CodigoPermission,
    string Title,
    bool Folder,
    bool Selected,
    bool Unselectable,
    string? Source,
    IReadOnlyList<AdminPermissionTreeNodeDto> Children);

public record AdminUserPermissionTreeDto(
    long IDUsuario,
    string Usuario,
    IReadOnlyList<string> RoleCodes,
    IReadOnlyList<AdminPermissionTreeNodeDto> Tree);

public record SaveUserPermissionsRequest(IReadOnlyList<long> IDFuncionalidades);

public record AdminRoleDto(
    long Id,
    long IDEmpresa,
    string CodigoRol,
    string NombreRol,
    bool Activo,
    DateTime UltModificacion,
    int PermisosCount,
    IReadOnlyList<string> PermissionCodes);

public record AdminRoleDetailDto(
    long Id,
    long IDEmpresa,
    string CodigoRol,
    string NombreRol,
    bool Activo,
    DateTime UltModificacion,
    IReadOnlyList<string> PermissionCodes);

public record AdminUserDto(
    long Id,
    long IDEmpresa,
    string Usuario,
    string? Nombre,
    string? Email,
    bool Activo,
    DateTime UltModificacion,
    IReadOnlyList<AdminUserRoleDto> Roles);

public record AdminUserRoleDto(long IdRol, string CodigoRol, string NombreRol);

public record AssignRoleRequest(long IDRol);

public record AssignPermissionsRequest(long IDRol, IReadOnlyList<string> PermissionCodes);

public record CopyRolePermissionsRequest(long IDRolOrigen);
