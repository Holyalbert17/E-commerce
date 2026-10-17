namespace Datamex.ECommerce.Application.Checkout;

public record CheckoutRequest(
    bool Delivery = true,
    bool PagarEnLaEntrega = true,
    string RazonSocial = "",
    string NumeroDocumento = "",
    string Nombre = "",
    string Apellido = "",
    string Email = "",
    string NroTelefono = "",
    string? LugarEntrega = null,
    string? Observacion = null,
    decimal? Latitud = null,
    decimal? Longitud = null,
    string? TipoComprobante = null);

public record CheckoutLineDto(
    long IDArticulo,
    string? Sku,
    string Nombre,
    decimal Cantidad,
    decimal PrecioUnitario,
    decimal MontoTotal);

public record CheckoutResultDto(
    long IDCarrito,
    long NroPedido,
    long? IDPedidoMovil,
    int EstadoCarrito,
    int TipoEntrega,
    int TipoPago,
    decimal MontoTotal,
    DateTime FechaPrevistaEntrega,
    string RazonSocial,
    string? LugarEntrega,
    bool RequierePagoOnline,
    string Mensaje,
    IReadOnlyList<CheckoutLineDto> Items,
    string? ProcessId = null,
    string? BancardMode = null,
    long? NroFactura = null,
    bool Facturado = false,
    bool PuedeDescargarFactura = false);

public interface ICheckoutService
{
    Task<CheckoutResultDto> CheckoutAsync(string sessionId, CheckoutRequest request, CancellationToken ct = default);
    Task<CheckoutResultDto?> GetOrderAsync(long nroPedido, string sessionId, CancellationToken ct = default);
}
