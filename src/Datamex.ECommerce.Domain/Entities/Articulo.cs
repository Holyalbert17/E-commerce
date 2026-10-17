using Datamex.ECommerce.Domain.Common;

namespace Datamex.ECommerce.Domain.Entities;

/// <summary>Espejo parcial de articulo — campos usados por e-commerce.</summary>
public class Articulo : EntidadConEmpresa
{
    public int NroArticulo { get; set; }
    public string? SKU { get; set; }
    public string DescripcionArticulo { get; set; } = string.Empty;
    public string DescripcionArticuloAbreviada { get; set; } = string.Empty;
    public string DescripcionArticuloExtensa { get; set; } = string.Empty;
    public string? CodReferenciaProveedor { get; set; }
    public long IDMarca { get; set; }
    public string? NombreMarca { get; set; }
    public long? IDLinea { get; set; }
    public string? NombreLinea { get; set; }
    public long? IDTipoArticulo { get; set; }
    public string? NombreTipoArticulo { get; set; }
    public long? IDProveedor { get; set; }
    public string? NombreProveedor { get; set; }
    public long? IDPais { get; set; }
    public string? NombrePais { get; set; }
    public long? IDRegimenImpositivo { get; set; }
    public string? NombreRegimenImpositivo { get; set; }
    public bool ActivoEcommerce { get; set; }

    /// <summary>Slug SEO (DEFI catálogo). Generado/editable en admin.</summary>
    public string? SlugSeo { get; set; }

    /// <summary>borrador | activo | agotado | descontinuado | oculto</summary>
    public string EstadoEcommerce { get; set; } = "activo";

    public string? FichaTecnicaUrl { get; set; }
    public string? VideoUrl { get; set; }

    public ICollection<ArticuloEmbalaje> Embalajes { get; set; } = new List<ArticuloEmbalaje>();
    public ICollection<ArticuloImagen> Imagenes { get; set; } = new List<ArticuloImagen>();
}
