using System.Net;
using System.Net.Mail;
using System.Text;
using Datamex.ECommerce.Application.Notifications;
using Datamex.ECommerce.Application.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Datamex.ECommerce.Infrastructure.Notifications;

public sealed class SmtpEmailSender : IEmailSender
{
    private readonly SmtpOptions _options;
    private readonly ILogger<SmtpEmailSender> _logger;

    public SmtpEmailSender(IOptions<SmtpOptions> options, ILogger<SmtpEmailSender> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    public Task SendOrderConfirmationAsync(OrderConfirmationEmail message, CancellationToken ct = default)
    {
        var lines = string.Join("\n", message.Lines.Select(l => $"- {l.Cantidad:0.##}x {l.Nombre}: {l.Monto:0} Gs"));
        var body = $"""
            Hola {message.CustomerName},

            Confirmamos tu pedido N° {message.NroPedido}.
            Total: {message.MontoTotal:0} Gs
            Entrega: {message.LugarEntrega ?? "Retiro en sucursal"}

            Detalle:
            {lines}
            """;
        return SendAsync(message.ToEmail, $"Pedido N° {message.NroPedido} confirmado", body, ct);
    }

    public Task SendActivationAsync(ActivationEmail message, CancellationToken ct = default)
    {
        var store = string.IsNullOrWhiteSpace(message.StoreName) ? "Datamex" : message.StoreName.Trim();
        var body = $"""
            Hola {message.CustomerName},

            Activá tu cuenta en {store} con el siguiente enlace:
            {message.ActivationUrl}
            """;
        return SendAsync(message.ToEmail, $"Activá tu cuenta en {store}", body, ct);
    }

    public Task SendPasswordResetAsync(PasswordResetEmail message, CancellationToken ct = default)
    {
        var store = string.IsNullOrWhiteSpace(message.StoreName) ? "Datamex" : message.StoreName.Trim();
        var body = $"""
            Hola {message.CustomerName},

            Restablecé tu contraseña de {store}:
            {message.ResetUrl}
            """;
        return SendAsync(message.ToEmail, $"Restablecer contraseña — {store}", body, ct);
    }

    public Task SendMayoristaDecisionAsync(MayoristaDecisionEmail message, CancellationToken ct = default)
    {
        var subject = message.Aprobado ? "Cuenta mayorista aprobada" : "Solicitud mayorista";
        var body = $"""
            Hola {message.CustomerName},

            {(message.Aprobado ? "Tu solicitud mayorista fue aprobada." : "Tu solicitud mayorista no fue aprobada en este momento.")}
            {message.Mensaje ?? ""}
            """;
        return SendAsync(message.ToEmail, subject, body, ct);
    }

    private async Task SendAsync(string to, string subject, string body, CancellationToken ct)
    {
        if (!_options.Enabled)
        {
            _logger.LogInformation("EMAIL (SMTP disabled) → {To} | {Subject}", to, subject);
            return;
        }

        using var client = new SmtpClient(_options.Host, _options.Port)
        {
            EnableSsl = _options.EnableSsl,
            DeliveryMethod = SmtpDeliveryMethod.Network,
        };
        if (!string.IsNullOrWhiteSpace(_options.User))
            client.Credentials = new NetworkCredential(_options.User, _options.Password ?? "");

        using var mail = new MailMessage(_options.From, to, subject, body) { BodyEncoding = Encoding.UTF8 };
        await client.SendMailAsync(mail, ct);
        _logger.LogInformation("EMAIL enviado → {To} | {Subject}", to, subject);
    }
}
