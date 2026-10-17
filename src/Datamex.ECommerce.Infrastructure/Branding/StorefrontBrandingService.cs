using System.Text.RegularExpressions;
using Datamex.ECommerce.Application.Abstractions;
using Datamex.ECommerce.Application.Branding;
using Datamex.ECommerce.Application.Media;
using Datamex.ECommerce.Domain.Entities;
using Datamex.ECommerce.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Datamex.ECommerce.Infrastructure.Branding;

public sealed class StorefrontBrandingService : IStorefrontBrandingService
{
    private static readonly Regex HexColor = new(@"^#([0-9A-Fa-f]{6}|[0-9A-Fa-f]{3})$", RegexOptions.Compiled);

    private readonly EcomDbContext _db;
    private readonly ITenantContext _tenant;
    private readonly IMediaStorage _media;

    public StorefrontBrandingService(EcomDbContext db, ITenantContext tenant, IMediaStorage media)
    {
        _db = db;
        _tenant = tenant;
        _media = media;
    }

    private long EmpresaId => _tenant.IDEmpresa
        ?? throw new InvalidOperationException("Tenant no resuelto.");

    public async Task<StorefrontBrandingDto?> GetPublicAsync(CancellationToken ct = default)
    {
        var idEmpresa = EmpresaId;
        var emp = await _db.Empresas.AsNoTracking().FirstOrDefaultAsync(e => e.ID == idEmpresa, ct);
        if (emp is null) return null;
        var cfg = await _db.EmpresaConfiguracionesEcommerce.AsNoTracking()
            .FirstOrDefaultAsync(c => c.ID == idEmpresa, ct);
        if (cfg is null) return null;
        return Map(emp, cfg);
    }

    public async Task<StorefrontBrandingDto> UpdateAsync(UpdateBrandingRequest request, CancellationToken ct = default)
    {
        var idEmpresa = EmpresaId;
        var emp = await _db.Empresas.FirstOrDefaultAsync(e => e.ID == idEmpresa, ct)
            ?? throw new InvalidOperationException("Empresa no encontrada.");
        var cfg = await _db.EmpresaConfiguracionesEcommerce.FirstOrDefaultAsync(c => c.ID == idEmpresa, ct)
            ?? throw new InvalidOperationException("Configuración e-commerce no encontrada.");

        if (request.NombreTiendaDisplay is not null)
            cfg.NombreTiendaDisplay = NullIfEmpty(request.NombreTiendaDisplay);
        if (request.TaglineTienda is not null)
            cfg.TaglineTienda = NullIfEmpty(request.TaglineTienda);
        if (request.LogoMark is not null)
            cfg.LogoMark = NormalizeMark(request.LogoMark);
        if (request.ColorPrimario is not null)
            cfg.ColorPrimario = NormalizeColor(request.ColorPrimario, "ColorPrimario");
        if (request.ColorSecundario is not null)
            cfg.ColorSecundario = NormalizeColor(request.ColorSecundario, "ColorSecundario");
        if (request.ColorAcento is not null)
            cfg.ColorAcento = NormalizeColor(request.ColorAcento, "ColorAcento");

        if (request.DireccionURLFacebook is not null) cfg.DireccionURLFacebook = NullIfEmpty(request.DireccionURLFacebook);
        if (request.DireccionURLTwitter is not null) cfg.DireccionURLTwitter = NullIfEmpty(request.DireccionURLTwitter);
        if (request.DireccionURLInstagram is not null) cfg.DireccionURLInstagram = NullIfEmpty(request.DireccionURLInstagram);
        if (request.DireccionURLYoutube is not null) cfg.DireccionURLYoutube = NullIfEmpty(request.DireccionURLYoutube);
        if (request.DireccionURLWhatsapp is not null) cfg.DireccionURLWhatsapp = NullIfEmpty(request.DireccionURLWhatsapp);
        if (request.DireccionURLTikTok is not null) cfg.DireccionURLTikTok = NullIfEmpty(request.DireccionURLTikTok);

        if (request.FooterDescripcion is not null) cfg.FooterDescripcion = NullIfEmpty(request.FooterDescripcion);
        if (request.DireccionLocal is not null) cfg.DireccionLocal = NullIfEmpty(request.DireccionLocal);
        if (request.CiudadLocal is not null) cfg.CiudadLocal = NullIfEmpty(request.CiudadLocal);
        if (request.TelefonoContacto is not null) cfg.TelefonoContacto = NullIfEmpty(request.TelefonoContacto);
        if (request.EmailContacto is not null) cfg.EmailContacto = NullIfEmpty(request.EmailContacto);
        if (request.HorarioAtencionTexto is not null) cfg.HorarioAtencionTexto = NullIfEmpty(request.HorarioAtencionTexto);
        if (request.MostrarMapaFooter.HasValue) cfg.MostrarMapaFooter = request.MostrarMapaFooter.Value;

        if (request.ClearCoords == true)
        {
            cfg.LatitudLocal = null;
            cfg.LongitudLocal = null;
        }
        else
        {
            if (request.LatitudLocal.HasValue)
            {
                if (request.LatitudLocal.Value is < -90 or > 90)
                    throw new ArgumentException("Latitud inválida (−90 a 90).");
                cfg.LatitudLocal = request.LatitudLocal.Value;
            }
            if (request.LongitudLocal.HasValue)
            {
                if (request.LongitudLocal.Value is < -180 or > 180)
                    throw new ArgumentException("Longitud inválida (−180 a 180).");
                cfg.LongitudLocal = request.LongitudLocal.Value;
            }
        }

        if (request.ClearLogo == true)
        {
            await _media.DeleteIfOwnedAsync(cfg.LogoUrl, ct);
            cfg.LogoUrl = null;
        }
        if (request.ClearFavicon == true)
        {
            await _media.DeleteIfOwnedAsync(cfg.FaviconUrl, ct);
            cfg.FaviconUrl = null;
        }
        if (request.ClearBanner == true)
        {
            await _media.DeleteIfOwnedAsync(cfg.BannerUrl, ct);
            cfg.BannerUrl = null;
        }

        if (request.ContenidoFaqHtml is not null) cfg.ContenidoFaqHtml = NullIfEmpty(request.ContenidoFaqHtml);
        if (request.ContenidoTerminosHtml is not null) cfg.ContenidoTerminosHtml = NullIfEmpty(request.ContenidoTerminosHtml);
        if (request.ContenidoComoComprarHtml is not null) cfg.ContenidoComoComprarHtml = NullIfEmpty(request.ContenidoComoComprarHtml);
        if (request.CreditoDesarrolladoPor is not null) cfg.CreditoDesarrolladoPor = NullIfEmpty(request.CreditoDesarrolladoPor);
        if (request.CreditoDesarrolladoPorUrl is not null) cfg.CreditoDesarrolladoPorUrl = NullIfEmpty(request.CreditoDesarrolladoPorUrl);

        cfg.UltModificacion = DateTime.UtcNow;
        cfg.UserName = "admin";
        await _db.SaveChangesAsync(ct);
        return Map(emp, cfg);
    }

    public async Task<StorefrontBrandingDto> UploadAssetAsync(
        BrandingAssetKind kind,
        Stream content,
        string contentType,
        string originalFileName,
        CancellationToken ct = default)
    {
        var idEmpresa = EmpresaId;
        var emp = await _db.Empresas.FirstOrDefaultAsync(e => e.ID == idEmpresa, ct)
            ?? throw new InvalidOperationException("Empresa no encontrada.");
        var cfg = await _db.EmpresaConfiguracionesEcommerce.FirstOrDefaultAsync(c => c.ID == idEmpresa, ct)
            ?? throw new InvalidOperationException("Configuración e-commerce no encontrada.");

        var kindKey = kind switch
        {
            BrandingAssetKind.Logo => "logo",
            BrandingAssetKind.Favicon => "favicon",
            BrandingAssetKind.Banner => "banner",
            _ => "asset"
        };

        var stored = await _media.SaveBrandingAssetAsync(
            idEmpresa, kindKey, content, contentType, originalFileName, ct);

        switch (kind)
        {
            case BrandingAssetKind.Logo:
                await _media.DeleteIfOwnedAsync(cfg.LogoUrl, ct);
                cfg.LogoUrl = stored.PublicUrl;
                break;
            case BrandingAssetKind.Favicon:
                await _media.DeleteIfOwnedAsync(cfg.FaviconUrl, ct);
                cfg.FaviconUrl = stored.PublicUrl;
                break;
            case BrandingAssetKind.Banner:
                await _media.DeleteIfOwnedAsync(cfg.BannerUrl, ct);
                cfg.BannerUrl = stored.PublicUrl;
                break;
        }

        cfg.UltModificacion = DateTime.UtcNow;
        cfg.UserName = "admin";
        await _db.SaveChangesAsync(ct);
        return Map(emp, cfg);
    }

    private static StorefrontBrandingDto Map(Empresa emp, EmpresaConfiguracionEcommerce cfg)
    {
        var nombre = FirstNonEmpty(cfg.NombreTiendaDisplay, emp.NombreFantasia, emp.RazonSocial) ?? "Tienda";
        var mark = FirstNonEmpty(cfg.LogoMark, TakeInitials(nombre)) ?? "D";
        return new StorefrontBrandingDto(
            emp.ID,
            nombre,
            cfg.TaglineTienda,
            cfg.LogoUrl,
            cfg.FaviconUrl,
            cfg.BannerUrl,
            cfg.ColorPrimario,
            cfg.ColorSecundario,
            cfg.ColorAcento,
            mark,
            cfg.DireccionURLFacebook,
            cfg.DireccionURLTwitter,
            cfg.DireccionURLInstagram,
            cfg.DireccionURLYoutube,
            cfg.DireccionURLWhatsapp,
            cfg.DireccionURLTikTok,
            cfg.FooterDescripcion,
            cfg.DireccionLocal,
            cfg.CiudadLocal,
            cfg.TelefonoContacto,
            cfg.EmailContacto,
            cfg.HorarioAtencionTexto,
            cfg.LatitudLocal,
            cfg.LongitudLocal,
            cfg.MostrarMapaFooter,
            cfg.RealizaDelivery,
            cfg.RealizaPickup,
            cfg.CobraEnLaEntrega,
            cfg.CobraAlHacerPedido,
            FmtOpt(cfg.TiempoAproxEntregaPickup),
            FmtOpt(cfg.TiempoAproxEntregaDelivery),
            cfg.ContenidoFaqHtml,
            cfg.ContenidoTerminosHtml,
            cfg.ContenidoComoComprarHtml,
            cfg.DeliveryEmiteFactura,
            cfg.DeliveryEmiteTicket,
            cfg.PickupEmiteFactura,
            cfg.PickupEmiteTicket,
            cfg.CreditoDesarrolladoPor,
            cfg.CreditoDesarrolladoPorUrl);
    }

    private static string? FmtOpt(TimeSpan? t) => t is null ? null : t.Value.ToString(@"hh\:mm");

    private static string? NormalizeColor(string? value, string field)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        var v = value.Trim();
        if (!v.StartsWith('#')) v = "#" + v;
        if (!HexColor.IsMatch(v))
            throw new ArgumentException($"{field} inválido. Usá formato #RRGGBB.");
        if (v.Length == 4)
        {
            v = $"#{v[1]}{v[1]}{v[2]}{v[2]}{v[3]}{v[3]}";
        }
        return v.ToUpperInvariant();
    }

    private static string? NormalizeMark(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        var t = value.Trim();
        return t.Length <= 2 ? t.ToUpperInvariant() : t[..2].ToUpperInvariant();
    }

    private static string? TakeInitials(string nombre)
    {
        var parts = nombre.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (parts.Length == 0) return "D";
        if (parts.Length == 1) return parts[0][..Math.Min(2, parts[0].Length)].ToUpperInvariant();
        return string.Concat(parts[0][0], parts[1][0]).ToUpperInvariant();
    }

    private static string? FirstNonEmpty(params string?[] values) =>
        values.FirstOrDefault(v => !string.IsNullOrWhiteSpace(v))?.Trim();

    private static string? NullIfEmpty(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
