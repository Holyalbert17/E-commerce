using Datamex.ECommerce.Domain.Enums;

namespace Datamex.ECommerce.Application.Options;

public class ECommerceOptions
{
    public const string SectionName = "ECommerce";

    public IntegrationMode IntegrationMode { get; set; } = IntegrationMode.Standalone;
    public string JwtIssuer { get; set; } = "Datamex.ECommerce";
    public string JwtAudience { get; set; } = "Datamex.ECommerce.Clients";
    public string JwtSigningKey { get; set; } = "CHANGE_ME_DEV_ONLY_DATAMEX_ECOM_32+";
    public int JwtExpirationMinutes { get; set; } = 60;
    public string DefaultTenantHeader { get; set; } = "X-Tenant-ID";
    public string DefaultTenantSlugHeader { get; set; } = "X-Tenant-Slug";
    /// <summary>Slug operativo por defecto cuando no hay header/query (Casa Imperial).</summary>
    public string DefaultTenantSlug { get; set; } = "casa-imperial";
    /// <summary>Base URL del storefront para links de activación (ERP ActivarUrl).</summary>
    public string StorefrontBaseUrl { get; set; } = "http://localhost:4200";
    public decimal DefaultMapLat { get; set; } = -25.3106m;
    public decimal DefaultMapLng { get; set; } = -57.5901m;
}
