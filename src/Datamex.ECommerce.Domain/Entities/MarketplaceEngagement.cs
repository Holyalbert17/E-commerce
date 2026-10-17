using Datamex.ECommerce.Domain.Common;

namespace Datamex.ECommerce.Domain.Entities;

/// <summary>Reseña / calificación de producto post-compra (ligada a email de cuenta).</summary>
public class ResenaArticuloEcommerce : EntidadConEmpresa
{
    public long IDArticulo { get; set; }
    public long IDUsuarioEcommerce { get; set; }
    public long? IDCuenta { get; set; }
    public long? IDPedidoMovil { get; set; }
    public long? IDCarritoCompra { get; set; }
    public string Email { get; set; } = string.Empty;
    public string CanalEcommerce { get; set; } = "B2C";
    public int Puntuacion { get; set; }
    public string? Titulo { get; set; }
    public string? Comentario { get; set; }
    public bool Aprobado { get; set; }
    public DateTime FechaResena { get; set; } = DateTime.UtcNow;

    public Articulo? Articulo { get; set; }
    public UsuarioEcommerce? UsuarioEcommerce { get; set; }
}

/// <summary>Sección de favoritos del cliente (ej. "bebidas"), por cuenta + canal minorista/mayorista.</summary>
public class ListaFavoritoEcommerce : EntidadConEmpresa
{
    public long IDUsuarioEcommerce { get; set; }
    public long? IDCuenta { get; set; }
    public string Email { get; set; } = string.Empty;
    public string NombreLista { get; set; } = string.Empty;
    public string CanalEcommerce { get; set; } = "B2C";
    public int Orden { get; set; }
    public string? Color { get; set; }

    public UsuarioEcommerce? UsuarioEcommerce { get; set; }
    public ICollection<DetalleListaFavoritoEcommerce> Detalles { get; set; } = new List<DetalleListaFavoritoEcommerce>();
}

public class DetalleListaFavoritoEcommerce : EntidadConEmpresa
{
    public long IDListaFavoritoEcommerce { get; set; }
    public long IDArticulo { get; set; }
    public DateTime FechaAgregado { get; set; } = DateTime.UtcNow;
    public string? Nota { get; set; }

    public ListaFavoritoEcommerce? ListaFavoritoEcommerce { get; set; }
    public Articulo? Articulo { get; set; }
}

/// <summary>Interacciones de producto (vista, click, add_cart, compra, favorito).</summary>
public class InteraccionArticuloEcommerce : EntidadConEmpresa
{
    public long IDArticulo { get; set; }
    public long? IDUsuarioEcommerce { get; set; }
    public string? Email { get; set; }
    public string? IDSesion { get; set; }
    public string TipoInteraccion { get; set; } = "vista";
    public string CanalEcommerce { get; set; } = "B2C";
    public DateTime Momento { get; set; } = DateTime.UtcNow;
}

/// <summary>Cupón / código promocional.</summary>
public class CuponEcommerce : EntidadConEmpresa
{
    public string Codigo { get; set; } = string.Empty;
    public string Descripcion { get; set; } = string.Empty;
    public string TipoDescuento { get; set; } = "porcentaje"; // porcentaje | monto
    public decimal ValorDescuento { get; set; }
    public decimal MontoMinimo { get; set; }
    public decimal? MontoMaximoDescuento { get; set; }
    public string CanalEcommerce { get; set; } = "ALL"; // B2C | B2B | ALL
    public DateTime FechaInicio { get; set; }
    public DateTime FechaFin { get; set; }
    public int? UsosMaximos { get; set; }
    public int UsosActuales { get; set; }
    public int UsosPorUsuario { get; set; } = 1;
}

public class UsoCuponEcommerce : EntidadConEmpresa
{
    public long IDCuponEcommerce { get; set; }
    public long? IDUsuarioEcommerce { get; set; }
    public long? IDCarritoCompra { get; set; }
    public string? Email { get; set; }
    public decimal MontoDescontado { get; set; }
    public DateTime FechaUso { get; set; } = DateTime.UtcNow;

    public CuponEcommerce? CuponEcommerce { get; set; }
}

/// <summary>Seguir producto (alertas / “notificarme”).</summary>
public class ProductoSeguidoEcommerce : EntidadConEmpresa
{
    public long IDUsuarioEcommerce { get; set; }
    public long IDArticulo { get; set; }
    public string Email { get; set; } = string.Empty;
    public string CanalEcommerce { get; set; } = "B2C";
    public DateTime FechaSeguimiento { get; set; } = DateTime.UtcNow;
}

/// <summary>Tracking de pedido (estados postventa).</summary>
public class SeguimientoPedidoEcommerce : EntidadConEmpresa
{
    public long IDPedidoMovil { get; set; }
    public long? IDCarritoCompra { get; set; }
    public string Estado { get; set; } = "preparando"; // preparando|despachado|en_camino|entregado|cancelado
    public string? Mensaje { get; set; }
    public DateTime FechaEstado { get; set; } = DateTime.UtcNow;
    public string? CodigoTracking { get; set; }
}

/// <summary>Solicitud de devolución / RMA self-service.</summary>
public class DevolucionEcommerce : EntidadConEmpresa
{
    public long IDUsuarioEcommerce { get; set; }
    public long? IDCuenta { get; set; }
    public string Email { get; set; } = string.Empty;
    public long IDPedidoMovil { get; set; }
    public long? IDArticulo { get; set; }
    public string Motivo { get; set; } = string.Empty;
    public string? Detalle { get; set; }
    public string Estado { get; set; } = "solicitada"; // solicitada|aprobada|rechazada|completada
    public DateTime FechaSolicitud { get; set; } = DateTime.UtcNow;
    public string? UrlAdjunto { get; set; }
    public string? MimeType { get; set; }
    public string? NombreArchivo { get; set; }
    /// <summary>Resultado del write-back ERP (NC / anulación).</summary>
    public string? ErpMensaje { get; set; }
    public long? IdNotaCreditoErp { get; set; }
    public bool ErpProcesado { get; set; }
}

/// <summary>Conversación CRM (inbox vendedor ↔ cliente), estilo WhatsApp.</summary>
public class ChatConversacionEcommerce : EntidadConEmpresa
{
    public long IDUsuarioEcommerce { get; set; }
    public long? IDPedidoMovil { get; set; }
    public string Email { get; set; } = string.Empty;
    public string? NombreCliente { get; set; }
    /// <summary>consulta | reclamo | pedido | general</summary>
    public string Asunto { get; set; } = "consulta";
    /// <summary>abierta | en_curso | resuelta | cerrada</summary>
    public string Estado { get; set; } = "abierta";
    public long? IDAdminAsignado { get; set; }
    public string? NombreAdminAsignado { get; set; }
    public DateTime FechaInicio { get; set; } = DateTime.UtcNow;
    public DateTime FechaUltimoMensaje { get; set; } = DateTime.UtcNow;
    public string? PreviewUltimoMensaje { get; set; }
    public int NoLeidosCliente { get; set; }
    public int NoLeidosSoporte { get; set; }

    public UsuarioEcommerce? UsuarioEcommerce { get; set; }
    public ICollection<ChatSoporteMensajeEcommerce> Mensajes { get; set; } = new List<ChatSoporteMensajeEcommerce>();
}

/// <summary>Mensaje de chat / soporte in-app (texto o foto).</summary>
public class ChatSoporteMensajeEcommerce : EntidadConEmpresa
{
    public long? IDConversacion { get; set; }
    public long? IDUsuarioEcommerce { get; set; }
    public long? IDPedidoMovil { get; set; }
    public long? IDAdminUsuario { get; set; }
    public string Email { get; set; } = string.Empty;
    public string Origen { get; set; } = "cliente"; // cliente | soporte
    /// <summary>texto | imagen</summary>
    public string TipoMensaje { get; set; } = "texto";
    public string Mensaje { get; set; } = string.Empty;
    public string? UrlAdjunto { get; set; }
    public string? MimeType { get; set; }
    public string? NombreArchivo { get; set; }
    public DateTime FechaMensaje { get; set; } = DateTime.UtcNow;
    public bool Leido { get; set; }

    public ChatConversacionEcommerce? Conversacion { get; set; }
}
