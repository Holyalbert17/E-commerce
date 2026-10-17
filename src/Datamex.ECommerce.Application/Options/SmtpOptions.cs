namespace Datamex.ECommerce.Application.Options;

public class SmtpOptions
{
    public const string SectionName = "Smtp";

    public bool Enabled { get; set; }
    public string Host { get; set; } = "localhost";
    public int Port { get; set; } = 587;
    public string? User { get; set; }
    public string? Password { get; set; }
    public string From { get; set; } = "noreply@datamex.local";
    public bool EnableSsl { get; set; } = true;
}
