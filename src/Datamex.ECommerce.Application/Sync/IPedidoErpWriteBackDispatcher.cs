namespace Datamex.ECommerce.Application.Sync;

/// <summary>
/// Write-back de PedidoMovil al ERP (paridad CarritoCompraService.FinalizarCarrito).
/// Preferir <see cref="FlushPedidoAsync"/> en checkout/pago para tiempo real;
/// <see cref="SchedulePedido"/> queda como reintento en background.
/// </summary>
public interface IPedidoErpWriteBackDispatcher
{
    /// <summary>Migración síncrona (visible en ERP al confirmar). Devuelve null si write-back deshabilitado.</summary>
    Task<MigrarPedidosResult?> FlushPedidoAsync(long idEmpresa, long idPedidoMovil, CancellationToken ct = default);

    /// <summary>Encola migración en background (fallback / reintento).</summary>
    void SchedulePedido(long idEmpresa, long idPedidoMovil);
}
