using Datamex.ECommerce.Domain.Common;

namespace Datamex.ECommerce.Domain.Entities;

/// <summary>Origen pedido móvil — espejo ValoresOrigenPedidoMovil.ECOMMERCE = 2.</summary>
public static class OrigenPedidoMovil
{
    public const int Movil = 0;
    public const int EDI = 1;
    public const int Ecommerce = 2;
}

/// <summary>Estado pedido móvil — espejo ValoresEstadoPedidoMovil.</summary>
public static class EstadoPedidoMovil
{
    public const int Abierto = 0;
    public const int Cerrado = 1;
    public const int Anulado = 6;
}

/// <summary>Estado carrito — espejo ValoresEstadoComprobante.</summary>
public static class EstadoCarrito
{
    public const int Abierto = 0;
    public const int Cerrado = 1;
    public const int Anulado = 2;
}

/// <summary>Tipo factura — espejo ValoresTipoFactura ERP (PedidoMovil).</summary>
public static class TipoFacturaErp
{
    public const int Reparto = 0; // delivery
    public const int Salon = 1;   // pickup
}

/// <summary>Tipo pedido — espejo ValoresTipoPedido ERP.</summary>
public static class TipoPedidoErp
{
    public const int Venta = 1;
}

/// <summary>Tipo entrega ecom — espejo CarritoCompra.ValoresTipoEntregaEcommerce.</summary>
public static class TipoEntregaEcommerce
{
    public const int Delivery = 0;
    public const int Pickup = 1;
}

/// <summary>Tipo pago ecom — espejo CarritoCompra.ValoresTipoPagoEcommerce.</summary>
public static class TipoPagoEcommerce
{
    public const int Entrega = 0;
    public const int Pedido = 1;
}

/// <summary>Espejo de pedidomovil (destino del checkout ecom).</summary>
public class PedidoMovil : EntidadConEmpresa
{
    public long NroPedido { get; set; }
    public long? NroFactura { get; set; }
    public long IDCuenta { get; set; }
    public long IDVendedor { get; set; }
    public long IDDeposito { get; set; }
    public double IndiceComision { get; set; }
    public long IDCotizacionComercial { get; set; }
    public long IDCondicionVenta { get; set; }
    public long? IDListaPrecioCuenta { get; set; }
    public long IDUsuario { get; set; } = 1;
    public DateTime FechaPedido { get; set; } = DateTime.UtcNow;
    public decimal MontoCambio { get; set; } = 1;
    public DateTime FechaEmision { get; set; } = DateTime.UtcNow;
    public int Plazo { get; set; }
    public double PorcentajeDescuento { get; set; }
    public int TipoFactura { get; set; }
    public int? TipoPedido { get; set; } = TipoPedidoErp.Venta;
    public int TipoPrecio { get; set; }
    public DateTime? FechaPrevistaEntrega { get; set; }
    public int Estado { get; set; } = EstadoPedidoMovil.Cerrado;
    public long NroPedidoDispositivoMovil { get; set; }
    /// <summary>false = pendiente de ETL/migración a Pedido ERP.</summary>
    public bool Migrado { get; set; }
    public DateTime Momento { get; set; } = DateTime.UtcNow;
    public string? Host { get; set; }
    public string? Observacion { get; set; }
    public decimal? Latitud { get; set; }
    public decimal? Longitud { get; set; }
    public decimal? Precision { get; set; }
    public string? NroOrdenCompra { get; set; }
    /// <summary>ValoresOrigenPedidoMovil.ECOMMERCE = 2.</summary>
    public int OrigenPedido { get; set; } = OrigenPedidoMovil.Ecommerce;
    public string? MotivoPendiente { get; set; }

    /// <summary>Chofer asignado al reparto (espejo IDChofer de planilla/viaje ERP).</summary>
    public long? IDChofer { get; set; }

    /// <summary>Dirección de entrega textual (copia de carrito.LugarEntrega).</summary>
    public string? DireccionEntrega { get; set; }

    /// <summary>Token opaco de solo lectura para compartir seguimiento sin sesión.</summary>
    public string? TokenTrackingPublico { get; set; }

    public ChoferEcommerce? Chofer { get; set; }
    public ICollection<DetallePedidoMovil> DetallePedidoMoviles { get; set; } = new List<DetallePedidoMovil>();
    public ICollection<CarritoCompra> CarritoCompras { get; set; } = new List<CarritoCompra>();
}

/// <summary>Espejo de detallepedidomovil (EntidadSinEmpresa en ERP).</summary>
public class DetallePedidoMovil : EntidadSinEmpresa
{
    public long IDPedidoMovil { get; set; }
    public long IDArticulo { get; set; }
    public long IDDetalleListaPrecio { get; set; } = 1;
    public long IDUsuarioTraspaso { get; set; } = 1;
    public long IDUsuario { get; set; } = 1;
    public string? CodigoBarra { get; set; }
    public double MontoCambio { get; set; } = 1;
    public double PorcentajeDescuento { get; set; }
    public double PrecioBrutoConImpuesto { get; set; }
    public double PrecioBrutoSinImpuesto { get; set; }
    public double PrecioNetoConImpuesto { get; set; }
    public double PrecioNetoSinImpuesto { get; set; }
    public int CantidadEmbalada { get; set; }
    public int CantidadFraccionada { get; set; }
    public DateTime Traspaso { get; set; } = DateTime.UtcNow;
    public DateTime Momento { get; set; } = DateTime.UtcNow;
    public string? Host { get; set; }
    public string? Observacion { get; set; }
    public int? NroOrden { get; set; }
    public bool DiferenciaPrecioSupermercado { get; set; }
    public decimal? PrecioLista { get; set; }

    public PedidoMovil? PedidoMovil { get; set; }
    public Articulo? Articulo { get; set; }
}
