namespace Datamex.ECommerce.Application.Branding;

public record StorefrontBrandingDto(
    long IdEmpresa,
    string NombreTienda,
    string? Tagline,
    string? LogoUrl,
    string? FaviconUrl,
    string? BannerUrl,
    string? ColorPrimario,
    string? ColorSecundario,
    string? ColorAcento,
    string LogoMark,
    string? Facebook,
    string? Twitter,
    string? Instagram,
    string? Youtube,
    string? Whatsapp,
    string? Tiktok = null,
    string? FooterDescripcion = null,
    string? DireccionLocal = null,
    string? CiudadLocal = null,
    string? TelefonoContacto = null,
    string? EmailContacto = null,
    string? HorarioAtencion = null,
    decimal? Latitud = null,
    decimal? Longitud = null,
    bool MostrarMapa = true,
    bool RealizaDelivery = true,
    bool RealizaPickup = true,
    bool CobraEnLaEntrega = false,
    bool CobraAlHacerPedido = false,
    string? TiempoAproxEntregaPickup = null,
    string? TiempoAproxEntregaDelivery = null,
    string? ContenidoFaqHtml = null,
    string? ContenidoTerminosHtml = null,
    string? ContenidoComoComprarHtml = null,
    bool DeliveryEmiteFactura = true,
    bool DeliveryEmiteTicket = false,
    bool PickupEmiteFactura = true,
    bool PickupEmiteTicket = false,
    string? CreditoDesarrolladoPor = null,
    string? CreditoDesarrolladoPorUrl = null);

public record UpdateBrandingRequest(
    string? NombreTiendaDisplay = null,
    string? TaglineTienda = null,
    string? LogoMark = null,
    string? ColorPrimario = null,
    string? ColorSecundario = null,
    string? ColorAcento = null,
    string? DireccionURLFacebook = null,
    string? DireccionURLTwitter = null,
    string? DireccionURLInstagram = null,
    string? DireccionURLYoutube = null,
    string? DireccionURLWhatsapp = null,
    string? DireccionURLTikTok = null,
    string? FooterDescripcion = null,
    string? DireccionLocal = null,
    string? CiudadLocal = null,
    string? TelefonoContacto = null,
    string? EmailContacto = null,
    string? HorarioAtencionTexto = null,
    decimal? LatitudLocal = null,
    decimal? LongitudLocal = null,
    bool? MostrarMapaFooter = null,
    bool? ClearCoords = null,
    bool? ClearLogo = null,
    bool? ClearFavicon = null,
    bool? ClearBanner = null,
    string? ContenidoFaqHtml = null,
    string? ContenidoTerminosHtml = null,
    string? ContenidoComoComprarHtml = null,
    string? CreditoDesarrolladoPor = null,
    string? CreditoDesarrolladoPorUrl = null);

public enum BrandingAssetKind
{
    Logo = 0,
    Favicon = 1,
    Banner = 2
}

public interface IStorefrontBrandingService
{
    Task<StorefrontBrandingDto?> GetPublicAsync(CancellationToken ct = default);
    Task<StorefrontBrandingDto> UpdateAsync(UpdateBrandingRequest request, CancellationToken ct = default);
    Task<StorefrontBrandingDto> UploadAssetAsync(
        BrandingAssetKind kind,
        Stream content,
        string contentType,
        string originalFileName,
        CancellationToken ct = default);
}
