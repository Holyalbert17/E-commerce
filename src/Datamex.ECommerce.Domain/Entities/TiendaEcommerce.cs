using Datamex.ECommerce.Domain.Common;

namespace Datamex.ECommerce.Domain.Entities;

/// <summary>Espejo de tiendaecommerce — multi-tienda por empresa.</summary>
public class TiendaEcommerce : EntidadConEmpresa
{
    public long IDClasificacionArticulo { get; set; }
    public string TokenTienda { get; set; } = string.Empty;
    public long IDDepositoTienda { get; set; }

    /// <summary>Extensión: nombre comercial de la tienda.</summary>
    public string? Nombre { get; set; }

    /// <summary>Extensión: canales habilitados (flags).</summary>
    public bool HabilitaB2C { get; set; } = true;
    public bool HabilitaB2B { get; set; }
}
