using Datamex.ECommerce.Domain.Common;

namespace Datamex.ECommerce.Domain.Entities;

/// <summary>Log mínimo de intentos Bancard (espejo reducido LogPagoTarjeta ERP).</summary>
public class LogPagoTarjeta : EntidadConEmpresa
{
    public long IDCarritoCompra { get; set; }
    public long ShopProcessId { get; set; }
    public string? ProcessId { get; set; }
    public decimal Monto { get; set; }
    public string Status { get; set; } = "pending";
    public string? AuthorizationCode { get; set; }
    public string? RawBody { get; set; }
    public DateTime Fecha { get; set; } = DateTime.UtcNow;
}
