using Datamex.ECommerce.Application.Catalog;

namespace Datamex.ECommerce.Application.Admin;

public record AdminCarritoListItemDto(
    long Id,
    long NroPedidoCarritoCompra,
    DateTime FechaPedido,
    string RazonSocial,
    string NumeroDocumento,
    decimal MontoTotal,
    int TipoEntrega,
    int TipoPago,
    int Estado,
    bool Migrado,
    long? IDPedidoMovil,
    long? IDCuenta,
    int ItemsCount,
    string? LugarEntrega);

public record AdminCarritoLineDto(
    long Id,
    long IDArticulo,
    string? Sku,
    string Nombre,
    int CantidadEmbalada,
    decimal PrecioUnitario,
    decimal MontoLinea);

public record AdminCarritoTrazabilidadStepDto(
    string Codigo,
    string Etiqueta,
    bool Completado,
    DateTime? Fecha,
    string? Detalle);

public record AdminCarritoDetailDto(
    long Id,
    long NroPedidoCarritoCompra,
    DateTime FechaPedido,
    DateTime FechaPrevistaEntrega,
    string RazonSocial,
    string NumeroDocumento,
    string? Observacion,
    string? LugarEntrega,
    int TipoEntrega,
    int TipoPago,
    int Estado,
    bool Migrado,
    long? IDPedidoMovil,
    long? IDCuenta,
    long? IDListaPrecio,
    long IDDeposito,
    decimal MontoTotal,
    decimal MontoIVA,
    decimal MontoDescuento,
    decimal? LatitudEntrega,
    decimal? LongitudEntrega,
    DateTime UltModificacion,
    IReadOnlyList<AdminCarritoLineDto> Items,
    IReadOnlyList<AdminCarritoTrazabilidadStepDto> Trazabilidad);

public record AdminCarritoExportRowDto(
    long Id,
    long NroPedidoCarritoCompra,
    DateTime FechaPedido,
    string RazonSocial,
    string NumeroDocumento,
    decimal MontoTotal,
    int TipoEntrega,
    int TipoPago,
    int Estado,
    long? IDPedidoMovil,
    string? LugarEntrega,
    string? Observacion);

public record MarkMigradosRequest(IReadOnlyList<long> Ids);
public record MarkMigradosResult(int UpdatedCount);

public interface IAdminCarritoService
{
    Task<PagedResult<AdminCarritoListItemDto>> SearchAsync(
        string? search,
        int? estado,
        bool? migrado,
        DateTime? fechaDesde,
        DateTime? fechaHasta,
        int page,
        int pageSize,
        CancellationToken ct = default);

    Task<AdminCarritoDetailDto?> GetAsync(long id, CancellationToken ct = default);

    Task<MarkMigradosResult> MarkMigradosAsync(IReadOnlyList<long> ids, CancellationToken ct = default);

    Task<IReadOnlyList<AdminCarritoExportRowDto>> GetPendingExportAsync(
        IReadOnlyList<long>? ids, CancellationToken ct = default);

    Task<byte[]> ExportAbandonadosCsvAsync(int hoursOld = 24, CancellationToken ct = default);
}
