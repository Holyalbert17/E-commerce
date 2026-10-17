using Datamex.ECommerce.Application.Notifications;
using Microsoft.Extensions.Logging;

namespace Datamex.ECommerce.Infrastructure.Notifications;

/// <summary>
/// Stub alineado a mails del ERP: loguea (sin SMTP). Listo para reemplazar por SMTP real.
/// </summary>
public sealed class LoggingEmailSender : IEmailSender
{
    private readonly ILogger<LoggingEmailSender> _logger;

    public LoggingEmailSender(ILogger<LoggingEmailSender> logger) => _logger = logger;

    public Task SendOrderConfirmationAsync(OrderConfirmationEmail message, CancellationToken ct = default)
    {
        var lines = string.Join("; ", message.Lines.Select(l => $"{l.Cantidad:0.##}x {l.Nombre}={l.Monto:0}"));
        _logger.LogInformation(
            "EMAIL pedido confirmado → {To} | Pedido {Nro} | Total {Total} | Entrega {Entrega} | Items: {Items}",
            message.ToEmail, message.NroPedido, message.MontoTotal, message.LugarEntrega ?? "(pickup)", lines);
        return Task.CompletedTask;
    }

    public Task SendActivationAsync(ActivationEmail message, CancellationToken ct = default)
    {
        _logger.LogInformation(
            "EMAIL activación cuenta → {To} | {Name} | Tienda: {Store} | URL: {Url}",
            message.ToEmail, message.CustomerName, message.StoreName, message.ActivationUrl);
        return Task.CompletedTask;
    }

    public Task SendPasswordResetAsync(PasswordResetEmail message, CancellationToken ct = default)
    {
        _logger.LogInformation(
            "EMAIL reset password → {To} | {Name} | Tienda: {Store} | URL: {Url}",
            message.ToEmail, message.CustomerName, message.StoreName, message.ResetUrl);
        return Task.CompletedTask;
    }

    public Task SendMayoristaDecisionAsync(MayoristaDecisionEmail message, CancellationToken ct = default)
    {
        _logger.LogInformation(
            "EMAIL mayorista {Decision} → {To} | {Name} | {Msg}",
            message.Aprobado ? "APROBADO" : "RECHAZADO",
            message.ToEmail,
            message.CustomerName,
            message.Mensaje ?? "");
        return Task.CompletedTask;
    }
}
