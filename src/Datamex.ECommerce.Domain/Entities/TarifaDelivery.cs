using Datamex.ECommerce.Domain.Common;

namespace Datamex.ECommerce.Domain.Entities;

/// <summary>
/// Espejo de tarifadelivery (ERP). En standalone usamos nombres denormalizados
/// para no depender aún de ciudad/barrio completos; IDs se mantienen para ETL.
/// </summary>
public class TarifaDelivery : EntidadConEmpresa
{
    public long IDDivisionPolitica { get; set; } = 1;
    public long IDCiudad { get; set; }
    public long IDBarrio { get; set; }
    public DateTime FechaInicioVigencia { get; set; }
    public DateTime FechaFinVigencia { get; set; }
    public decimal ImporteDelivery { get; set; }
    /// <summary>
    /// ValoresEstadoVarios: 0=ABIERTO (pendiente), 1=CERRADO (autorizado),
    /// 2=ANULADO, 3=CANCELADO.
    /// </summary>
    public int Estado { get; set; }
    public long? IDAgencia { get; set; }

    /// <summary>Cache UI; la fuente de verdad son ciudad/barrio maestros.</summary>
    public string NombreCiudad { get; set; } = string.Empty;
    public string NombreBarrio { get; set; } = string.Empty;

    public Ciudad? Ciudad { get; set; }
    public Barrio? Barrio { get; set; }
    public DivisionPolitica? DivisionPolitica { get; set; }
}
