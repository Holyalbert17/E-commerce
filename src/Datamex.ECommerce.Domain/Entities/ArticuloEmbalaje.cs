using Datamex.ECommerce.Domain.Common;

namespace Datamex.ECommerce.Domain.Entities;

/// <summary>Espejo parcial de articuloembalaje — variantes de venta/empaque.</summary>
public class ArticuloEmbalaje : EntidadConEmpresa
{
    public long IDArticulo { get; set; }
    public long IDEmbalaje { get; set; }
    public double CantidadxEmbalaje { get; set; } = 1;
    public bool EmbalajeParaFacturacion { get; set; } = true;
    public bool EmbalajeParaControlStock { get; set; } = true;
    public string? CodigoBarras { get; set; }
    public string? DescripcionEmbalaje { get; set; }
    public decimal? PrecioDemo { get; set; }

    /// <summary>Código de unidad: UN, M, M2, KG, L, etc.</summary>
    public string CodigoUnidadMedida { get; set; } = "UN";

    /// <summary>Permite cantidades decimales (metro, m², kg).</summary>
    public bool PermiteFraccion { get; set; }

    /// <summary>Paso de venta (ej. 0.5 m).</summary>
    public decimal IncrementoVenta { get; set; } = 1;

    /// <summary>Cantidad mínima de venta en la unidad.</summary>
    public decimal CantidadMinimaVenta { get; set; } = 1;

    public Articulo? Articulo { get; set; }
    public ICollection<ArticuloEmbalajeCodigoBarra> CodigosBarra { get; set; } = new List<ArticuloEmbalajeCodigoBarra>();
}

/// <summary>Catálogo parametrizable de unidades de medida por tenant.</summary>
public class UnidadMedidaEcommerce : EntidadConEmpresa
{
    public string Codigo { get; set; } = "UN";
    public string Nombre { get; set; } = "Unidad";
    public string? Simbolo { get; set; } = "u";
    public bool PermiteDecimales { get; set; }
    public decimal IncrementoDefault { get; set; } = 1;
    public int Orden { get; set; }
}

/// <summary>Espejo simplificado de articuloimagen.</summary>
public class ArticuloImagen : EntidadConEmpresa
{
    public long IDArticulo { get; set; }
    public string Url { get; set; } = string.Empty;
    public string? Titulo { get; set; }
    public int Orden { get; set; }
    public bool EsPrincipal { get; set; }

    public Articulo? Articulo { get; set; }
}
