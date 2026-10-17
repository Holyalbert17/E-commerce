using System.Net;
using System.Net.Mail;
using System.Text;
using System.Text.RegularExpressions;
using Datamex.ECommerce.Application.Abstractions;
using Datamex.ECommerce.Application.Contacto;
using Datamex.ECommerce.Domain.Entities;
using Datamex.ECommerce.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Datamex.ECommerce.Infrastructure.Contacto;

public sealed class ContactoService : IContactoService
{
    private static readonly Regex EmailRx = new(
        @"^[^@\s]+@[^@\s]+\.[^@\s]+$", RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private readonly EcomDbContext _db;
    private readonly ITenantContext _tenant;
    private readonly ILogger<ContactoService> _logger;

    public ContactoService(EcomDbContext db, ITenantContext tenant, ILogger<ContactoService> logger)
    {
        _db = db;
        _tenant = tenant;
        _logger = logger;
    }

    public async Task<ContactoResultDto> EnviarAsync(ContactoRequest request, CancellationToken ct = default)
    {
        var idEmpresa = RequireTenant();
        var nombre = (request.Nombre ?? "").Trim();
        var email = (request.Email ?? "").Trim();
        var mensaje = (request.Mensaje ?? "").Trim();
        var telefono = string.IsNullOrWhiteSpace(request.Telefono) ? null : request.Telefono.Trim();

        if (nombre.Length < 2)
            throw new ArgumentException("Nombre es obligatorio.");
        if (!EmailRx.IsMatch(email))
            throw new ArgumentException("Email inválido.");
        if (mensaje.Length < 5)
            throw new ArgumentException("Mensaje es obligatorio (mín. 5 caracteres).");

        var row = new ContactoMensaje
        {
            IDEmpresa = idEmpresa,
            Nombre = Truncate(nombre, 120)!,
            Email = Truncate(email, 256)!,
            Telefono = Truncate(telefono, 40),
            Mensaje = Truncate(mensaje, 4000)!,
            FechaUtc = DateTime.UtcNow,
            Leido = false,
            Activo = true,
            UltModificacion = DateTime.UtcNow,
            UserName = email
        };
        _db.ContactoMensajes.Add(row);
        await _db.SaveChangesAsync(ct);

        var cfg = await _db.EmpresaConfiguracionesEcommerce.AsNoTracking()
            .FirstOrDefaultAsync(c => c.ID == idEmpresa, ct);
        var emailed = await TrySendSmtpAsync(cfg, row, ct);

        var msg = emailed
            ? "Mensaje enviado. Te contactaremos pronto."
            : "Mensaje recibido. Te contactaremos pronto.";
        return new ContactoResultDto(true, msg, row.ID);
    }

    public async Task<IReadOnlyList<ContactoMensajeDto>> ListAdminAsync(
        bool? leido = null, CancellationToken ct = default)
    {
        var idEmpresa = RequireTenant();
        var q = _db.ContactoMensajes.AsNoTracking()
            .Where(x => x.IDEmpresa == idEmpresa && x.Activo);
        if (leido.HasValue)
            q = q.Where(x => x.Leido == leido.Value);

        return await q.OrderByDescending(x => x.FechaUtc)
            .Take(200)
            .Select(x => new ContactoMensajeDto(
                x.ID, x.Nombre, x.Email, x.Telefono, x.Mensaje, x.FechaUtc, x.Leido))
            .ToListAsync(ct);
    }

    public async Task MarcarLeidoAsync(long id, bool leido = true, CancellationToken ct = default)
    {
        var idEmpresa = RequireTenant();
        var row = await _db.ContactoMensajes
            .FirstOrDefaultAsync(x => x.ID == id && x.IDEmpresa == idEmpresa, ct)
            ?? throw new InvalidOperationException("Mensaje no encontrado.");
        row.Leido = leido;
        row.UltModificacion = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);
    }

    private async Task<bool> TrySendSmtpAsync(
        EmpresaConfiguracionEcommerce? cfg, ContactoMensaje row, CancellationToken ct)
    {
        if (cfg is null
            || string.IsNullOrWhiteSpace(cfg.NombreServidorCorreoElectronico)
            || cfg.PuertoCorreoElectronico is null or <= 0)
        {
            _logger.LogInformation(
                "CONTACTO recibido (sin SMTP tenant) empresa={Empresa} de={Email} nombre={Nombre}: {Mensaje}",
                row.IDEmpresa, row.Email, row.Nombre, row.Mensaje);
            return false;
        }

        var to = FirstNonEmpty(cfg.EmailContacto)
                 ?? await _db.Empresas.AsNoTracking()
                     .Where(e => e.ID == row.IDEmpresa)
                     .Select(e => e.EMail)
                     .FirstOrDefaultAsync(ct);
        if (string.IsNullOrWhiteSpace(to))
        {
            _logger.LogInformation(
                "CONTACTO recibido (sin destinatario) empresa={Empresa} de={Email}: {Mensaje}",
                row.IDEmpresa, row.Email, row.Mensaje);
            return false;
        }

        try
        {
            var port = (int)cfg.PuertoCorreoElectronico.Value;
            using var client = new SmtpClient(cfg.NombreServidorCorreoElectronico, port)
            {
                EnableSsl = port is 465 or 587,
                DeliveryMethod = SmtpDeliveryMethod.Network,
            };
            if (!string.IsNullOrWhiteSpace(cfg.NombreUsuarioCorreoElectronico))
            {
                client.Credentials = new NetworkCredential(
                    cfg.NombreUsuarioCorreoElectronico,
                    cfg.PasswordCorreoElectronico ?? "");
            }

            var from = string.IsNullOrWhiteSpace(cfg.NombreUsuarioCorreoElectronico)
                ? to
                : cfg.NombreUsuarioCorreoElectronico;
            var body = $"""
                Nuevo mensaje de contacto

                Nombre: {row.Nombre}
                Email: {row.Email}
                Teléfono: {row.Telefono ?? "-"}

                {row.Mensaje}
                """;
            using var mail = new MailMessage(from, to, $"Contacto web — {row.Nombre}", body)
            {
                BodyEncoding = Encoding.UTF8,
                ReplyToList = { new MailAddress(row.Email, row.Nombre) }
            };
            await client.SendMailAsync(mail, ct);
            _logger.LogInformation("CONTACTO email enviado → {To} de {From}", to, row.Email);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "CONTACTO SMTP falló; mensaje {Id} queda guardado", row.ID);
            return false;
        }
    }

    private long RequireTenant()
    {
        if (!_tenant.IsResolved || !_tenant.IDEmpresa.HasValue)
            throw new InvalidOperationException("Tenant no resuelto.");
        return _tenant.IDEmpresa.Value;
    }

    private static string? FirstNonEmpty(params string?[] values) =>
        values.FirstOrDefault(v => !string.IsNullOrWhiteSpace(v))?.Trim();

    private static string? Truncate(string? v, int max) =>
        string.IsNullOrWhiteSpace(v) ? null : (v.Trim().Length <= max ? v.Trim() : v.Trim()[..max]);
}
