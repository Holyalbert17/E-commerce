namespace Datamex.ECommerce.Application.Invoices;

public record AccountInvoiceDto(
    long NroPedido,
    long? IDPedidoMovil,
    long? NroFactura,
    long? IDPedidoErp,
    DateTime FechaPedido,
    decimal MontoTotal,
    string? RazonSocial,
    bool Facturado,
    bool PuedeDescargarPdf,
    string TipoReporte,
    string? Mensaje);

public record InvoicePdfResult(
    byte[] Content,
    string FileName,
    string ContentType,
    long? NroFactura,
    long IDPedidoErp);

public interface IInvoiceService
{
    /// <summary>Facturas / pedidos facturados del comprador (historial).</summary>
    Task<IReadOnlyList<AccountInvoiceDto>> ListAsync(long idUsuarioEcommerce, CancellationToken ct = default);

    /// <summary>PDF factura vía Pentaho: original adquirente (mitad superior).</summary>
    Task<InvoicePdfResult> DownloadPdfAsync(
        long idUsuarioEcommerce, long nroPedido, CancellationToken ct = default);

    /// <summary>PDF nota de crédito (devolución): original adquirente a media hoja.</summary>
    Task<InvoicePdfResult> DownloadNotaCreditoPdfAsync(
        long idUsuarioEcommerce, long nroPedido, CancellationToken ct = default);

    /// <summary>PDF para backoffice: por ID PedidoMovil del tenant (sin validar comprador).</summary>
    Task<InvoicePdfResult> DownloadPdfByPedidoMovilAsync(
        long idPedidoMovil, CancellationToken ct = default);
}
