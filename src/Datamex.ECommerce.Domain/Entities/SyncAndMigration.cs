using Datamex.ECommerce.Domain.Common;

namespace Datamex.ECommerce.Domain.Entities;

/// <summary>Watermark de sync ERP → Ecom por entidad y empresa.</summary>
public class SyncCursor : EntidadConEmpresa
{
    /// <summary>catalog | prices | stock | customers | order_status</summary>
    public string EntityKey { get; set; } = string.Empty;
    public DateTime? LastWatermarkUtc { get; set; }
    public long? LastId { get; set; }
    public DateTime? LastRunUtc { get; set; }
    public string? LastStatus { get; set; }
    public string? LastError { get; set; }
    public int RowsProcessed { get; set; }
    /// <summary>Filas leídas del ERP en la última corrida.</summary>
    public int RowsRead { get; set; }
    public int RowsInserted { get; set; }
    public int RowsUpdated { get; set; }
    public long DurationMs { get; set; }
}

/// <summary>Log de write-back PedidoMovil Ecom → ERP.</summary>
public class MigracionPedidoLog : EntidadConEmpresa
{
    public long IDPedidoMovil { get; set; }
    public long? IDCarritoCompra { get; set; }
    public long NroPedido { get; set; }
    /// <summary>pendiente | ok | error</summary>
    public string Estado { get; set; } = "pendiente";
    public string? MensajeError { get; set; }
    public string? DetalleTecnico { get; set; }
    public long? IDPedidoMovilErp { get; set; }
    public int Intentos { get; set; }
    public DateTime FechaIntento { get; set; } = DateTime.UtcNow;
    public string? UserNameOperador { get; set; }
}
