namespace Datamex.ECommerce.Application.Audit;

public record AdminAuditoriaDto(
    long Id,
    string Usuario,
    string Accion,
    string Entidad,
    string? EntidadId,
    string? Detalle,
    DateTime Fecha);

public interface IAdminAuditService
{
    Task LogAsync(
        string accion,
        string entidad,
        string? entidadId,
        string? detalle,
        long? idAdminUsuario,
        string? usuario,
        string? ip,
        CancellationToken ct = default);

    Task<IReadOnlyList<AdminAuditoriaDto>> ListAsync(int take = 100, CancellationToken ct = default);
}
