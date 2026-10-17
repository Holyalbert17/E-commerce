using Datamex.ECommerce.Domain.Common;

namespace Datamex.ECommerce.Domain.Entities;

/// <summary>
/// Configuración flexible por clave JSON para marketing (layout, temas, textos).
/// Versionada + vigencia opcional por campaña.
/// </summary>
public class ConfiguracionPersonalizacion : EntidadConEmpresa
{
    /// <summary>Ej: home.layout | tema.activo | checkout.textos</summary>
    public string Clave { get; set; } = string.Empty;
    public string ValorJson { get; set; } = "{}";
    public int Version { get; set; } = 1;
    public DateTime? FechaInicioVigencia { get; set; }
    public DateTime? FechaFinVigencia { get; set; }
    public string? CreadoPor { get; set; }
    public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;
    public string? Nota { get; set; }
}
