using Datamex.ECommerce.Domain.Common;

namespace Datamex.ECommerce.Domain.Entities;

/// <summary>
/// Espejo de articuloembalajecodigobarra (ERP).
/// El barcode cuelga del embalaje, no del artículo.
/// </summary>
public class ArticuloEmbalajeCodigoBarra : EntidadConEmpresa
{
    public long IDArticuloEmbalaje { get; set; }
    public string CodigoBarra { get; set; } = string.Empty;
    /// <summary>Flag ERP ParaSupermercado.</summary>
    public bool ParaSupermercado { get; set; }

    public ArticuloEmbalaje? ArticuloEmbalaje { get; set; }
}

/// <summary>
/// Espejo simplificado de Chofer (ERP hereda Empleado; aquí es maestro e-com standalone).
/// Asignación a pedidos de delivery (paridad con PlanillaRuteo.IDChofer / ViajeMovil.IDChofer).
/// </summary>
public class ChoferEcommerce : EntidadConEmpresa
{
    public int NroChofer { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string? Documento { get; set; }
    public string? Telefono { get; set; }
    public string? Observacion { get; set; }
}
