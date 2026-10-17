namespace Datamex.ECommerce.Application.Sync;

/// <summary>
/// Anulación / devolución de pedidos e-com ya escritos en Postgres ERP.
/// </summary>
public interface IErpPedidoLifecycleWriter
{
    /// <summary>
    /// Anula el pedidomovil en ERP (pickup / pre-factura) y reintegra stock ERP.
    /// No-op si no hay CS / AllowWriteBack / pedido no migrado.
    /// </summary>
    Task<ErpLifecycleResult> AnularPedidoAsync(
        long idEmpresa, long idPedidoMovilLocal, string? motivo, CancellationToken ct = default);

    /// <summary>
    /// Devolución automática: sin factura → anula; con factura → nota de crédito DEVOLUCION + stock.
    /// </summary>
    Task<ErpLifecycleResult> DevolverPedidoAsync(
        long idEmpresa, long idPedidoMovilLocal, string? motivo, CancellationToken ct = default);

    /// <summary>
    /// Trae nrofactura / id pedido ERP al espejo local.
    /// </summary>
    Task<ErpFacturaSyncResult> SincronizarFacturaAsync(
        long idEmpresa, long idPedidoMovilLocal, CancellationToken ct = default);
}

public record ErpLifecycleResult(
    bool Ok,
    bool Skipped,
    string Mensaje,
    long? IdPedidoMovilErp = null,
    long? IdNotaCreditoErp = null,
    long? NroFactura = null);

public record ErpFacturaSyncResult(
    bool Ok,
    string Mensaje,
    long? NroFactura = null,
    long? IdPedidoErp = null,
    bool EsFiscalErp = false);

/// <summary>Serie local e-com (Admin AsignarFactura) vs serie fiscal ERP típica.</summary>
public static class FacturaSerie
{
    public const long LocalMin = 1_000_000L;

    public static bool EsFiscalErp(long? nroFactura) =>
        nroFactura is > 0 and < LocalMin;

    /// <summary>Pedido con N° asignado en ERP (factura emitida o pedido facturado en salón).</summary>
    public static bool EsFacturado(long? nroFactura) =>
        nroFactura is > 0;

    /// <summary>Pedido con comprobante imprimible vía Pentaho (ERP asignó N°).</summary>
    public static bool EsImprimibleErp(long? nroFactura) =>
        EsFacturado(nroFactura);
}
