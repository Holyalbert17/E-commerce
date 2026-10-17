namespace Datamex.ECommerce.Application.Admin;

public record AdminChoferDto(
    long Id,
    int NroChofer,
    string Nombre,
    string? Documento,
    string? Telefono,
    string? Observacion,
    bool Activo);

public record UpsertChoferRequest(
    string Nombre,
    int NroChofer = 0,
    string? Documento = null,
    string? Telefono = null,
    string? Observacion = null,
    bool? Activo = null);

public interface IAdminChoferService
{
    Task<IReadOnlyList<AdminChoferDto>> ListAsync(CancellationToken ct = default);
    Task<AdminChoferDto> UpsertAsync(long? id, UpsertChoferRequest request, CancellationToken ct = default);
    Task DeleteAsync(long id, CancellationToken ct = default);
    Task AsignarPedidoAsync(long idPedidoMovil, long? idChofer, CancellationToken ct = default);
}
