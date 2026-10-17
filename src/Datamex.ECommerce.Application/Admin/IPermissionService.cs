namespace Datamex.ECommerce.Application.Admin;

public interface IPermissionService
{
    Task<bool> HasPermissionAsync(long idEmpresa, long idUsuario, string permissionCode, CancellationToken ct = default);
    Task<IReadOnlyList<string>> ListPermissionCodesAsync(long idEmpresa, long idUsuario, CancellationToken ct = default);
}
