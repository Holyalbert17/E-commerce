using Datamex.ECommerce.Domain.Common;

namespace Datamex.ECommerce.Domain.Entities;

/// <summary>Tipos de bloque CMS (alineado a MODELO_PRELIMINAR / WIREFRAME_HOME).</summary>
public static class ContenidoBloqueTipo
{
    public const string TopInfoBar = "top_info_bar";
    public const string HeroBanner = "hero_banner";
    public const string HeroSlider = "hero_slider";
    public const string PromoBannerGrid = "promo_banner_grid";
    public const string FeaturedCategories = "featured_categories";
    public const string ProductCarousel = "product_carousel";
    public const string Newsletter = "newsletter";
    public const string FeatureList = "feature_list";
    public const string CouponStrip = "coupon_strip";
}

public static class ContenidoEstadoEditorial
{
    public const string Draft = "draft";
    public const string Published = "published";
    public const string Archived = "archived";
}

/// <summary>Bloque de contenido de página (home, etc.) — DEFI CMS.</summary>
public class ContenidoBloque : EntidadConEmpresa
{
    public string PageCode { get; set; } = "home";
    public string? PageZone { get; set; }
    public string BlockType { get; set; } = ContenidoBloqueTipo.HeroBanner;
    public string InternalName { get; set; } = string.Empty;
    public string? Title { get; set; }
    public string? Subtitle { get; set; }
    public string? Description { get; set; }
    public string? LayoutVariant { get; set; }
    public int DisplayOrder { get; set; }
    public string ContentStatus { get; set; } = ContenidoEstadoEditorial.Draft;
    public bool IsReadOnlyWhenIntegrated { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public string? SettingsJson { get; set; }
    public DateTime? PublishedAt { get; set; }
    public long? UpdatedByAdminUserId { get; set; }
    public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;

    public ICollection<ContenidoBloqueItem> Items { get; set; } = new List<ContenidoBloqueItem>();
}

public class ContenidoBloqueItem : EntidadConEmpresa
{
    public long IDContenidoBloque { get; set; }
    public long? ParentItemId { get; set; }
    public string ItemType { get; set; } = "default";
    public string? Title { get; set; }
    public string? Subtitle { get; set; }
    public string? Description { get; set; }
    public string? ImageUrl { get; set; }
    public string? MobileImageUrl { get; set; }
    public string? IconUrl { get; set; }
    public string? LinkUrl { get; set; }
    public string? LinkTarget { get; set; } = "_self";
    public string? BadgeText { get; set; }
    public string? BadgeType { get; set; }
    public string? PriceText { get; set; }
    public DateTime? CountdownEndAt { get; set; }
    public int DisplayOrder { get; set; }
    public string ContentStatus { get; set; } = ContenidoEstadoEditorial.Published;
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public string? SettingsJson { get; set; }

    public ContenidoBloque? Bloque { get; set; }
}

public static class NavegacionMenuTipo
{
    public const string Header = "header";
    public const string Topbar = "topbar";
    public const string Sidebar = "sidebar";
    public const string Footer = "footer";
}

public class NavegacionMenu : EntidadConEmpresa
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string MenuType { get; set; } = NavegacionMenuTipo.Header;
    public string? PageCode { get; set; } = "global";
    public string? PageZone { get; set; }
    public int DisplayOrder { get; set; }
    public string ContentStatus { get; set; } = ContenidoEstadoEditorial.Published;
    public string? SettingsJson { get; set; }

    public ICollection<NavegacionMenuItem> Items { get; set; } = new List<NavegacionMenuItem>();
}

public class NavegacionMenuItem : EntidadConEmpresa
{
    public long IDNavegacionMenu { get; set; }
    public long? ParentItemId { get; set; }
    public long? IDGrupoArticulo { get; set; }
    public string ItemType { get; set; } = "external_link";
    public string Label { get; set; } = string.Empty;
    public string? Url { get; set; }
    public string? Target { get; set; } = "_self";
    public string? IconUrl { get; set; }
    public string? BadgeText { get; set; }
    public int DisplayOrder { get; set; }
    public string ContentStatus { get; set; } = ContenidoEstadoEditorial.Published;

    public NavegacionMenu? Menu { get; set; }
}

/// <summary>Auditoría backoffice (DEFI §4.10).</summary>
public class AdminAuditoriaLog : EntidadConEmpresa
{
    public long? IDAdminUsuario { get; set; }
    public string Usuario { get; set; } = string.Empty;
    public string Accion { get; set; } = string.Empty;
    public string Entidad { get; set; } = string.Empty;
    public string? EntidadId { get; set; }
    public string? Detalle { get; set; }
    public DateTime Fecha { get; set; } = DateTime.UtcNow;
    public string? Ip { get; set; }
}

/// <summary>Movimiento de stock (DEFI inventario).</summary>
public class InventarioMovimiento : EntidadConEmpresa
{
    public long IDArticulo { get; set; }
    public long? IDEmbalaje { get; set; }
    public long? IDDeposito { get; set; }
    public long? IDPedidoMovil { get; set; }
    public string TipoMovimiento { get; set; } = string.Empty;
    public decimal Cantidad { get; set; }
    public decimal ExistenciaAntes { get; set; }
    public decimal ExistenciaDespues { get; set; }
    public string? Referencia { get; set; }
    public DateTime Fecha { get; set; } = DateTime.UtcNow;
}
