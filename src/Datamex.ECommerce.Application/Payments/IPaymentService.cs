using Datamex.ECommerce.Application.Checkout;

namespace Datamex.ECommerce.Application.Payments;

public record PaymentSessionDto(
    long IDCarrito,
    long NroPedido,
    long ShopProcessId,
    string? ProcessId,
    decimal MontoTotal,
    string Currency,
    bool RequierePagoOnline,
    bool PagoCompletado,
    string BancardMode,
    string? CheckoutScriptUrl,
    string Mensaje);

public interface IPaymentService
{
    Task<PaymentSessionDto?> GetSessionAsync(long nroPedido, string sessionId, CancellationToken ct = default);

    /// <summary>Webhook Bancard (POST Checkout) — cierra carrito + PedidoMovil.</summary>
    Task<CheckoutResultDto> ConfirmWebhookAsync(BancardWebhookPayload payload, CancellationToken ct = default);

    /// <summary>Simulación sandbox (solo Mode=Stub).</summary>
    Task<CheckoutResultDto> SimulateSuccessAsync(long shopProcessId, string sessionId, CancellationToken ct = default);

    Task RollbackAsync(long shopProcessId, CancellationToken ct = default);
}
