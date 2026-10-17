namespace Datamex.ECommerce.Application.Notifications;

public record OrderConfirmationEmail(
    string ToEmail,
    string CustomerName,
    long NroPedido,
    decimal MontoTotal,
    string? LugarEntrega,
    IReadOnlyList<(string Nombre, decimal Cantidad, decimal Monto)> Lines);

public record ActivationEmail(
    string ToEmail,
    string CustomerName,
    string ActivationUrl,
    string StoreName = "Datamex");

public record PasswordResetEmail(
    string ToEmail,
    string CustomerName,
    string ResetUrl,
    string StoreName = "Datamex");

public record MayoristaDecisionEmail(
    string ToEmail,
    string CustomerName,
    bool Aprobado,
    string? Mensaje);

public interface IEmailSender
{
    Task SendOrderConfirmationAsync(OrderConfirmationEmail message, CancellationToken ct = default);
    Task SendActivationAsync(ActivationEmail message, CancellationToken ct = default);
    Task SendPasswordResetAsync(PasswordResetEmail message, CancellationToken ct = default);
    Task SendMayoristaDecisionAsync(MayoristaDecisionEmail message, CancellationToken ct = default);
}
