using Datamex.ECommerce.Domain.Common;

namespace Datamex.ECommerce.Domain.Entities;

/// <summary>
/// Espejo de <c>dbo.empresaconfiguracionecommerce</c>. ID = IDEmpresa (1:1).
/// Campos extra (slug, branding, mayorista, ModoIntegracion) son extensión e-com local.
/// </summary>
public class EmpresaConfiguracionEcommerce : EntidadSinEmpresa
{
    public bool RealizaDelivery { get; set; } = true;
    public bool RealizaPickup { get; set; } = true;
    public bool CobraEnLaEntrega { get; set; }
    public bool CobraAlHacerPedido { get; set; }
    public decimal MontoPedidoMinimo { get; set; }
    public bool ProcesaPedidosPorPlanilla { get; set; }
    public bool ProcesaPedidosERP { get; set; } = true;
    public long NroUltPedidoCarritoCompra { get; set; }
    public long? IDArticuloDelivery { get; set; }
    public bool ControlaStockEnCarrito { get; set; }

    /// <summary>
    /// Lectura continua de stock ERP → espejo local (ErpSyncHostedService).
    /// Activable/desactivable por tenant desde admin.
    /// </summary>
    public bool SyncStockContinuo { get; set; } = true;

    /// <summary>
    /// Intervalo en minutos para el pull de stock continuo (1–120). Default 5.
    /// Usá 1 para near real-time.
    /// </summary>
    public int SyncStockIntervaloMinutos { get; set; } = 5;

    /// <summary>
    /// Al generar/confirmar el pedido, hacer write-back inmediato a PedidoMovil ERP.
    /// Activable/desactivable por tenant desde admin.
    /// </summary>
    public bool MigrarPedidosAlGenerar { get; set; } = true;

    /// <summary>
    /// Permite INSERT/UPDATE real en el ERP de esta empresa (write-back).
    /// Independiente del kill-switch global Erp:AllowWriteBack.
    /// </summary>
    public bool ErpAllowWriteBack { get; set; }

    /// <summary>Host Postgres del ERP para esta empresa (override de ConnectionStrings:Erp).</summary>
    public string? ErpHost { get; set; }

    /// <summary>Puerto ERP (default 5432).</summary>
    public int? ErpPort { get; set; }

    /// <summary>Nombre de base ERP.</summary>
    public string? ErpDatabase { get; set; }

    /// <summary>Usuario DB ERP.</summary>
    public string? ErpUsername { get; set; }

    /// <summary>Password ERP cifrado (ErpSecretBox).</summary>
    public string? ErpPasswordProtected { get; set; }

    /// <summary>SSL Mode Npgsql (Prefer / Require / Disable). Null → Prefer.</summary>
    public string? ErpSslMode { get; set; }

    /// <summary>
    /// Modo por tenant: 0=Integrated, 1=Standalone, 2=External (extensión e-com).
    /// </summary>
    public int ModoIntegracion { get; set; } = 1;

    public string? ClavePublicaProcesadoraTarjeta { get; set; }
    public string? ClavePrivadaProcesadoraTarjeta { get; set; }
    public string? DireccionURLCheckout { get; set; }
    public long? CodigoComercio { get; set; }
    public long? CodigoSucursal { get; set; }

    /// <summary>Secuencia shop_process_id Bancard (espejo UltIDProcesoNegocio ERP).</summary>
    public long UltIDProcesoNegocio { get; set; }

    /// <summary>ERP <c>tiempoaproxentregapickup</c>.</summary>
    public TimeSpan? TiempoAproxEntregaPickup { get; set; }

    /// <summary>ERP <c>tiempoaproxentregadelivery</c>.</summary>
    public TimeSpan? TiempoAproxEntregaDelivery { get; set; }

    public string? DireccionURLFacebook { get; set; }
    public string? DireccionURLTwitter { get; set; }
    public string? DireccionURLInstagram { get; set; }
    public string? DireccionURLYoutube { get; set; }
    public string? DireccionURLWhatsapp { get; set; }

    /// <summary>ERP SMTP — host.</summary>
    public string? NombreServidorCorreoElectronico { get; set; }

    /// <summary>ERP SMTP — usuario.</summary>
    public string? NombreUsuarioCorreoElectronico { get; set; }

    /// <summary>ERP SMTP — password.</summary>
    public string? PasswordCorreoElectronico { get; set; }

    /// <summary>ERP SMTP — puerto.</summary>
    public long? PuertoCorreoElectronico { get; set; }

    public long IDDeposito { get; set; }

    public ICollection<EmpresaConfiguracionEcommerceRedSocial> RedesSociales { get; set; }
        = new List<EmpresaConfiguracionEcommerceRedSocial>();

    /// <summary>Extensión ecom: slug público de la vitrina.</summary>
    public string? SlugTienda { get; set; }

    /// <summary>Extensión ecom: dominio custom.</summary>
    public string? DominioCustom { get; set; }

    public decimal MontoUmbralMayorista { get; set; }
    public bool UsarListaPrecioCuentaErp { get; set; }
    public long? IDListaPrecioMayoristaDefault { get; set; }

    public string? LogoUrl { get; set; }
    public string? FaviconUrl { get; set; }
    public string? BannerUrl { get; set; }
    public string? ColorPrimario { get; set; }
    public string? ColorSecundario { get; set; }
    public string? ColorAcento { get; set; }
    public string? NombreTiendaDisplay { get; set; }
    public string? TaglineTienda { get; set; }
    public string? LogoMark { get; set; }
    public string? FooterDescripcion { get; set; }
    public string? DireccionLocal { get; set; }
    public string? CiudadLocal { get; set; }
    public string? TelefonoContacto { get; set; }
    public string? EmailContacto { get; set; }
    public string? HorarioAtencionTexto { get; set; }
    public decimal? LatitudLocal { get; set; }
    public decimal? LongitudLocal { get; set; }
    public bool MostrarMapaFooter { get; set; } = true;
    public string? DireccionURLTikTok { get; set; }

    /// <summary>HTML de FAQ público (extensión e-com).</summary>
    public string? ContenidoFaqHtml { get; set; }

    /// <summary>HTML de términos y condiciones.</summary>
    public string? ContenidoTerminosHtml { get; set; }

    /// <summary>HTML de “cómo comprar”.</summary>
    public string? ContenidoComoComprarHtml { get; set; }

    /// <summary>Delivery: permitir emitir factura.</summary>
    public bool DeliveryEmiteFactura { get; set; } = true;

    /// <summary>Delivery: permitir emitir ticket.</summary>
    public bool DeliveryEmiteTicket { get; set; }

    /// <summary>Pickup: permitir emitir factura.</summary>
    public bool PickupEmiteFactura { get; set; } = true;

    /// <summary>Pickup: permitir emitir ticket.</summary>
    public bool PickupEmiteTicket { get; set; }

    /// <summary>Crédito footer “Desarrollado por …”; null → UI muestra Datamex.</summary>
    public string? CreditoDesarrolladoPor { get; set; }

    public string? CreditoDesarrolladoPorUrl { get; set; }

    public Empresa? Empresa { get; set; }
}
