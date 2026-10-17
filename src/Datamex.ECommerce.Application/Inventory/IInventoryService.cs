namespace Datamex.ECommerce.Application.Inventory;

public record DepositoDto(long Id, int Codigo, string Descripcion, bool DepositoEntrega);

/// <summary>
/// Existencia = TotalExistencia (como ProductoDTO.Existencia del ERP).
/// CantidadLibre = TotalExistencia − TotalReserva (disponible neto para badge/vitrina).
/// Validación de carrito ERP usa TotalExistencia (TieneArticulosSinStock).
/// ControlaStock = flag tenant (mutación local); la vitrina debe usar EnStock/CantidadLibre.
/// </summary>
public record StockItemDto(
    long IDArticulo,
    long IDDeposito,
    string? DepositoNombre,
    decimal TotalExistencia,
    decimal TotalReserva,
    decimal CantidadLibre,
    bool ControlaStock)
{
    public decimal CantidadDisponible => TotalExistencia;
    public decimal CantidadReservada => TotalReserva;
    /// <summary>Badge vitrina: hay unidades libres en el espejo ERP.</summary>
    public bool EnStock => CantidadLibre > 0;
}

public record StockCheckResult(bool Ok, decimal CantidadLibre, string? Mensaje);

public record StockLineMove(long IdArticulo, decimal Cantidad);

public record InventarioMovimientoDto(
    long Id,
    long IDArticulo,
    long? IDDeposito,
    string TipoMovimiento,
    decimal Cantidad,
    decimal ExistenciaAntes,
    decimal ExistenciaDespues,
    string? Referencia,
    DateTime Fecha,
    string? UserName);

public interface IInventoryService
{
    Task<DepositoDto?> GetEcommerceDepositoAsync(CancellationToken ct = default);
    Task<StockItemDto?> GetStockAsync(long articuloId, long? depositoId = null, CancellationToken ct = default);
    Task<IReadOnlyDictionary<long, StockItemDto>> GetStockBatchAsync(
        IEnumerable<long> articuloIds, long? depositoId = null, CancellationToken ct = default);
    Task<StockCheckResult> CheckAvailabilityAsync(
        long articuloId, decimal cantidad, long? depositoId = null, CancellationToken ct = default);

    /// <summary>Reserva blanda (TotalReserva +=). Opcional; el ecom ERP no la usa en carrito.</summary>
    Task ReserveAsync(long articuloId, decimal cantidad, long depositoId, CancellationToken ct = default);
    Task ReleaseReservationAsync(long articuloId, decimal cantidad, long depositoId, CancellationToken ct = default);

    /// <summary>
    /// Paridad ERP PedidoMovil (CU reserve): TotalExistencia −= qty, TotalPedidoVenta += qty.
    /// Solo Standalone + ControlaStockEnCarrito. Integrated: no llamar (el ERP mueve al facturar).
    /// </summary>
    Task ApplyPedidoStockAsync(
        long depositoId,
        IReadOnlyList<StockLineMove> lines,
        long? skipArticuloId = null,
        CancellationToken ct = default);

    /// <summary>
    /// Reverso de ApplyPedidoStockAsync (cancelación / rollback PedidoMovil no migrado).
    /// </summary>
    Task RestorePedidoStockAsync(
        long depositoId,
        IReadOnlyList<StockLineMove> lines,
        long? skipArticuloId = null,
        CancellationToken ct = default);

    Task<IReadOnlyList<InventarioMovimientoDto>> ListMovimientosAsync(
        long? articuloId = null, int take = 100, CancellationToken ct = default);
}
