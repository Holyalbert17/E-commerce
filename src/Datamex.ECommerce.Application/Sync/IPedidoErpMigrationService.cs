namespace Datamex.ECommerce.Application.Sync;

public record MigracionPedidoLogDto(
    long Id,
    long IDPedidoMovil,
    long? IDCarritoCompra,
    long NroPedido,
    string Estado,
    string? MensajeError,
    string? DetalleTecnico,
    long? IDPedidoMovilErp,
    int Intentos,
    DateTime FechaIntento,
    string? UserNameOperador);

public record MigrarPedidosResult(
    int Procesados,
    int Exitosos,
    int Errores,
    IReadOnlyList<MigracionPedidoLogDto> Logs);

public record MigrarPedidosRequest(IReadOnlyList<long> Ids);

public interface IPedidoErpMigrationService
{
    Task<MigrarPedidosResult> MigrarAsync(IReadOnlyList<long> ids, CancellationToken ct = default);
    Task<IReadOnlyList<MigracionPedidoLogDto>> ListLogsAsync(
        long? idPedidoMovil = null,
        int limit = 100,
        CancellationToken ct = default);
}
