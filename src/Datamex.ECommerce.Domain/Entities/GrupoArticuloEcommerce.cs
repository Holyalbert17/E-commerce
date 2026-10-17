using Datamex.ECommerce.Domain.Common;

namespace Datamex.ECommerce.Domain.Entities;

/// <summary>Espejo de grupoarticuloecommerce — colecciones / vitrinas.</summary>
public class GrupoArticuloEcommerce : EntidadConEmpresa
{
    public int CodGrupoArticulo { get; set; }
    public string DescripcionGrupoArticulo { get; set; } = string.Empty;
    public DateTime FechaInicioVigencia { get; set; }
    public DateTime FechaFinVigencia { get; set; }
    public int TipoGrupo { get; set; }
    public string? Imagen { get; set; }
    public int Estado { get; set; }
    public int Orden { get; set; }

    /// <summary>Link al hacer clic en flyer/publicidad (ruta o URL absoluta).</summary>
    public string? UrlDestino { get; set; }

    /// <summary>Texto corto bajo el título del flyer.</summary>
    public string? Subtitulo { get; set; }

    public ICollection<DetalleGrupoArticuloEcommerce> DetalleGrupoArticuloEcommerces { get; set; } = new List<DetalleGrupoArticuloEcommerce>();
}

public class DetalleGrupoArticuloEcommerce : EntidadConEmpresa
{
    public long IDGrupoArticuloEcommerce { get; set; }
    public long IDArticulo { get; set; }

    public GrupoArticuloEcommerce? GrupoArticuloEcommerce { get; set; }
    public Articulo? Articulo { get; set; }
}
