using Datamex.ECommerce.Domain.Common;

namespace Datamex.ECommerce.Domain.Entities;

/// <summary>Espejo parcial de listaprecio.</summary>
public class ListaPrecio : EntidadConEmpresa
{
    public int NroListaPrecio { get; set; }
    public string Descripcion { get; set; } = string.Empty;
    public bool EsListaPrecioEcommerce { get; set; }
    public bool PrecioConImpuestos { get; set; } = true;
    public DateTime FechaRegistro { get; set; } = DateTime.UtcNow;
    public DateTime? FechaInicioVigencia { get; set; }
    public DateTime? FechaFinVigencia { get; set; }

    /// <summary>Extensión ecom: B2C | B2B | BOTH</summary>
    public string CanalEcommerce { get; set; } = "B2C";

    /// <summary>
    /// Espejo ERP ValoresEstadoListaPrecio:
    /// 0=ANULADO, 1=ABIERTO (pendiente), 2=AUTORIZADO, 3=CANCELADO.
    /// </summary>
    public int Estado { get; set; } = 1;

    /// <summary>Paridad ERP descuentos / umbral a nivel lista.</summary>
    public decimal CantidadMinimaMayorista { get; set; }
    public decimal PorDescuentoMinorista { get; set; }
    public decimal PorDescuentoMayorista { get; set; }

    public ICollection<DetalleListaPrecioCondicionVenta> DetallesCondicionVenta { get; set; } = new List<DetalleListaPrecioCondicionVenta>();
    public ICollection<DetalleListaPrecio> DetallesListaPrecio { get; set; } = new List<DetalleListaPrecio>();
    public ICollection<ListaPrecioCuenta> ListaPrecioCuentas { get; set; } = new List<ListaPrecioCuenta>();
}

/// <summary>
/// Espejo de dbo.listapreciocuenta — asignación (historial) lista ↔ cuenta.
/// PedidoMovil.IDListaPrecioCuenta apunta a esta fila (no a listaprecio.id).
/// </summary>
public class ListaPrecioCuenta : EntidadConEmpresa
{
    public long IDListaPrecio { get; set; }
    public long IDCuenta { get; set; }

    public ListaPrecio? ListaPrecio { get; set; }
    public Cuenta? Cuenta { get; set; }
}

/// <summary>Espejo de detallelistaprecio — precio simple (ERP lo usa al agregar al carrito).</summary>
public class DetalleListaPrecio : EntidadConEmpresa
{
    public long IDListaPrecio { get; set; }
    public long IDArticuloEmbalaje { get; set; }
    public decimal Precio { get; set; }

    public ListaPrecio? ListaPrecio { get; set; }
    public ArticuloEmbalaje? ArticuloEmbalaje { get; set; }
}

/// <summary>Espejo parcial de detallelistapreciocondicionventa — precio por embalaje en una lista.</summary>
public class DetalleListaPrecioCondicionVenta : EntidadConEmpresa
{
    public long IDListaPrecio { get; set; }
    public long IDArticuloEmbalaje { get; set; }
    public decimal PrecioContado { get; set; }
    public decimal PrecioCheque { get; set; }
    public decimal PrecioCredito { get; set; }
    public decimal CantidadMinimaMayorista { get; set; }
    public decimal PorDescuentoMinorista { get; set; }
    public decimal PorDescuentoMayorista { get; set; }
    public bool EsAutomatico { get; set; } = true;

    public ListaPrecio? ListaPrecio { get; set; }
    public ArticuloEmbalaje? ArticuloEmbalaje { get; set; }
}
