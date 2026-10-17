using Datamex.ECommerce.Domain.Common;

namespace Datamex.ECommerce.Domain.Entities;

/// <summary>Espejo de carritocompra.</summary>
public class CarritoCompra : EntidadConEmpresa
{
    public long NroPedidoCarritoCompra { get; set; }
    public long? IDCuenta { get; set; }
    public long? IDListaPrecio { get; set; }
    public long IDCondicionVenta { get; set; }
    public long IDVendedor { get; set; }
    public long IDDeposito { get; set; }
    public long IDCotizacionComercial { get; set; }
    public long IDAgenciaFacturacion { get; set; }
    public DateTime FechaPedido { get; set; } = DateTime.UtcNow;
    public decimal MontoCambio { get; set; }
    public int Plazo { get; set; }
    public decimal PorcentajeDescuento { get; set; }
    public decimal PorcentajeRecargo { get; set; }
    public int TipoEntrega { get; set; }
    public int TipoPago { get; set; }
    public DateTime FechaPrevistaEntrega { get; set; } = DateTime.UtcNow;
    public string? LugarEntrega { get; set; }
    public string? Observacion { get; set; }
    public decimal MontoTotal { get; set; }
    public decimal MontoIVA { get; set; }
    public decimal MontoExento { get; set; }
    public decimal MontoGravado { get; set; }
    public decimal MontoDescuento { get; set; }
    public decimal MontoRecargo { get; set; }
    public string RazonSocial { get; set; } = string.Empty;
    public string NumeroDocumento { get; set; } = string.Empty;
    public long? IDPedidoMovil { get; set; }
    public decimal? LatitudEntrega { get; set; }
    public decimal? LongitudEntrega { get; set; }
    public int Estado { get; set; }
    public bool Migrado { get; set; }
    public string? IDSesion { get; set; }
    public long? IDProcesoNegocio { get; set; }
    public string? CodigoCuponAplicado { get; set; }

    public PedidoMovil? PedidoMovil { get; set; }
    public ICollection<DetalleCarritoCompra> DetalleCarritoCompras { get; set; } = new List<DetalleCarritoCompra>();
}

/// <summary>Espejo de detallecarritocompra.</summary>
public class DetalleCarritoCompra : EntidadConEmpresa
{
    public long IDCarritoCompra { get; set; }
    public long IDArticulo { get; set; }
    public decimal CantidadxEmbalaje { get; set; }
    public long IDRegimenImpositivo { get; set; }
    public decimal PorcentajeDescuento { get; set; }
    public decimal CantidadBonificada { get; set; }
    public int CantidadEmbaladaBonificada { get; set; }
    public int CantidadFraccionadaBonificada { get; set; }
    public decimal Cantidad { get; set; }
    public int CantidadEmbalada { get; set; }
    public int CantidadFraccionada { get; set; }
    public decimal PorcentajeImpuesto { get; set; }
    public decimal PrecioBrutoConImpuesto { get; set; }
    public decimal PrecioBrutoSinImpuesto { get; set; }
    public decimal PrecioNetoConImpuesto { get; set; }
    public decimal PrecioNetoSinImpuesto { get; set; }
    public decimal MontoTotal { get; set; }
    public decimal MontoIVA { get; set; }

    public CarritoCompra? CarritoCompra { get; set; }
    public Articulo? Articulo { get; set; }
}
