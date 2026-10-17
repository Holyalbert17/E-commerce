using Datamex.ECommerce.Application.Abstractions;
using System.Net;
using System.Net.Http;
using Datamex.ECommerce.Application.Account;
using Datamex.ECommerce.Application.Admin;
using Datamex.ECommerce.Application.Auth;
using Datamex.ECommerce.Application.Branding;
using Datamex.ECommerce.Application.Cart;
using Datamex.ECommerce.Application.Catalog;
using Datamex.ECommerce.Application.Checkout;
using Datamex.ECommerce.Application.Contacto;
using Datamex.ECommerce.Application.Engagement;
using Datamex.ECommerce.Application.Favorites;
using Datamex.ECommerce.Application.Geography;
using Datamex.ECommerce.Application.Invoices;
using Datamex.ECommerce.Application.Inventory;
using Datamex.ECommerce.Application.Media;
using Datamex.ECommerce.Application.Notifications;
using Datamex.ECommerce.Application.Options;
using Datamex.ECommerce.Application.Payments;
using Datamex.ECommerce.Application.Personalizacion;
using Datamex.ECommerce.Application.Pricing;
using Datamex.ECommerce.Application.Provisioning;
using Datamex.ECommerce.Application.Reviews;
using Datamex.ECommerce.Application.Search;
using Datamex.ECommerce.Application.Shipping;
using Datamex.ECommerce.Application.Sync;
using Datamex.ECommerce.Infrastructure.Account;
using Datamex.ECommerce.Infrastructure.Admin;
using Datamex.ECommerce.Infrastructure.Auth;
using Datamex.ECommerce.Infrastructure.Branding;
using Datamex.ECommerce.Infrastructure.Cart;
using Datamex.ECommerce.Infrastructure.Catalog;
using Datamex.ECommerce.Infrastructure.Checkout;
using Datamex.ECommerce.Infrastructure.Contacto;
using Datamex.ECommerce.Infrastructure.Engagement;
using Datamex.ECommerce.Infrastructure.Favorites;
using Datamex.ECommerce.Infrastructure.Geography;
using Datamex.ECommerce.Infrastructure.Inventory;
using Datamex.ECommerce.Application.Audit;
using Datamex.ECommerce.Application.Content;
using Datamex.ECommerce.Infrastructure.Audit;
using Datamex.ECommerce.Infrastructure.Content;
using Datamex.ECommerce.Infrastructure.Invoices;
using Datamex.ECommerce.Infrastructure.Media;
using Datamex.ECommerce.Infrastructure.Notifications;
using Datamex.ECommerce.Infrastructure.Payments;
using Datamex.ECommerce.Infrastructure.Personalizacion;
using Datamex.ECommerce.Infrastructure.Persistence;
using Datamex.ECommerce.Infrastructure.Pricing;
using Datamex.ECommerce.Infrastructure.Provisioning;
using Datamex.ECommerce.Infrastructure.Reviews;
using Datamex.ECommerce.Infrastructure.Search;
using Datamex.ECommerce.Infrastructure.Shipping;
using Datamex.ECommerce.Infrastructure.Sync;
using Datamex.ECommerce.Infrastructure.Tenancy;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Datamex.ECommerce.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddECommerceInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<ECommerceOptions>(configuration.GetSection(ECommerceOptions.SectionName));
        services.Configure<BancardOptions>(configuration.GetSection(BancardOptions.SectionName));
        services.Configure<MediaOptions>(configuration.GetSection(MediaOptions.SectionName));
        services.Configure<SmtpOptions>(configuration.GetSection(SmtpOptions.SectionName));
        services.Configure<PentahoOptions>(configuration.GetSection(PentahoOptions.SectionName));
        services.Configure<SearchOptions>(configuration.GetSection(SearchOptions.SectionName));
        services.AddMemoryCache();

        var redisCs = configuration["Search:RedisConnection"]
                      ?? configuration.GetConnectionString("Redis");
        if (!string.IsNullOrWhiteSpace(redisCs))
        {
            services.AddStackExchangeRedisCache(o => o.Configuration = redisCs);
        }
        else
        {
            services.AddDistributedMemoryCache();
        }

        services.AddHttpClient("opensearch", c =>
        {
            c.Timeout = TimeSpan.FromSeconds(8);
        });
        services.AddScoped<IProductSearchService, ProductSearchService>();
        services.AddHostedService<SearchIndexHostedService>();
        services.AddScoped<IContentCmsService, ContentCmsService>();
        services.AddScoped<IAdminAuditService, AdminAuditService>();

        services.AddDbContext<EcomDbContext>(options =>
        {
            var provider = configuration["Database:Provider"] ?? "Postgres";
            var cs = configuration.GetConnectionString("EcomDb")
                     ?? "Host=localhost;Port=5432;Database=datamex_ecom;Username=ecom;Password=ecom_dev_pass";

            if (string.Equals(provider, "Sqlite", StringComparison.OrdinalIgnoreCase)
                || cs.StartsWith("Data Source=", StringComparison.OrdinalIgnoreCase))
            {
                options.UseSqlite(cs);
            }
            else
            {
                options.UseNpgsql(cs, npgsql =>
                {
                    npgsql.MigrationsHistoryTable("__EFMigrationsHistory", "dbo");
                    // Postgres remoto (10.99.99.x) a veces corta: evita 500 esporádicos en listados admin.
                    npgsql.EnableRetryOnFailure(
                        maxRetryCount: 5,
                        maxRetryDelay: TimeSpan.FromSeconds(10),
                        errorCodesToAdd: null);
                });
            }
        });

        var bancardMode = configuration["Bancard:Mode"] ?? "Stub";
        if (string.Equals(bancardMode, "Live", StringComparison.OrdinalIgnoreCase))
        {
            services.AddHttpClient<IBancardClient, HttpBancardClient>();
        }
        else
        {
            services.AddScoped<IBancardClient, StubBancardClient>();
        }

        var useRemoteMedia = string.Equals(
            configuration["Media:UseRemoteSftp"], "true", StringComparison.OrdinalIgnoreCase)
            || string.Equals(configuration["Media:UseRemoteSftp"], "1", StringComparison.OrdinalIgnoreCase);
        if (useRemoteMedia)
        {
            // Local sirve /media vía API; SFTP replica al host de Postgres (205).
            services.AddScoped<LocalMediaStorage>();
            services.AddScoped<SftpMediaStorage>();
            services.AddScoped<IMediaStorage, MirroredMediaStorage>();
        }
        else
        {
            services.AddScoped<IMediaStorage, LocalMediaStorage>();
        }
        services.AddScoped<ITenantContext, TenantContext>();
        services.AddScoped<IAuthService, JwtAuthService>();
        services.AddScoped<ICatalogService, CatalogService>();
        services.AddScoped<IPricingService, PricingService>();
        services.AddScoped<IInventoryService, InventoryService>();
        services.AddScoped<ICartService, CartService>();
        services.AddScoped<ICheckoutService, CheckoutService>();
        services.AddScoped<IPaymentService, PaymentService>();
        services.AddScoped<IShippingService, ShippingService>();
        services.AddScoped<IAccountService, AccountService>();
        services.AddScoped<IMayoristaService, MayoristaService>();
        services.AddScoped<IGeographyService, GeographyService>();
        services.AddScoped<ITenantProvisioningService, TenantProvisioningService>();
        if (configuration.GetValue("Smtp:Enabled", false))
            services.AddScoped<IEmailSender, SmtpEmailSender>();
        else
            services.AddScoped<IEmailSender, LoggingEmailSender>();
        services.AddScoped<IAdminStockHelper, AdminStockHelper>();
        services.AddScoped<IAdminCatalogService, AdminCatalogService>();
        services.AddScoped<IAdminParametrosProductoService, AdminParametrosProductoService>();
        services.AddScoped<IAdminOrdersService, AdminOrdersService>();
        services.AddScoped<IAdminDashboardService, AdminDashboardService>();
        services.AddScoped<IAdminPromotionsService, AdminPromotionsService>();
        services.AddScoped<IAdminShippingAdminService, AdminShippingAdminService>();
        services.AddScoped<IAdminEmpresaService, AdminEmpresaService>();
        services.AddScoped<IAdminUnidadesService, AdminUnidadesService>();
        services.AddScoped<IStorefrontBrandingService, StorefrontBrandingService>();
        services.AddScoped<IPersonalizacionService, PersonalizacionService>();
        services.AddScoped<IAdminListaPrecioService, AdminListaPrecioService>();
        services.AddScoped<IAdminClienteService, AdminClienteService>();
        services.AddScoped<IAdminCarritoService, AdminCarritoService>();
        services.AddScoped<IAdminPlanillaService, AdminPlanillaService>();
        services.AddScoped<IAdminReportsService, AdminReportsService>();
        services.AddScoped<IAdminChoferService, AdminChoferService>();
        services.AddScoped<IPedidoErpMigrationService, PedidoErpMigrationService>();
        services.AddScoped<IErpPedidoLifecycleWriter, ErpPedidoLifecycleWriter>();
        services.AddSingleton<IPedidoErpWriteBackDispatcher, PedidoErpWriteBackDispatcher>();
        services.AddScoped<ICuentaErpWriter, CuentaErpWriter>();
        services.AddScoped<IRegistroClienteErpLookup, RegistroClienteErpLookup>();
        services.AddScoped<IErpConnectionResolver, ErpConnectionResolver>();
        services.AddScoped<IErpSyncService, ErpSyncService>();
        services.AddScoped<IInvoiceService, PentahoInvoiceService>();
        services.AddHttpClient("pentaho", (sp, client) =>
        {
            var opts = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<Datamex.ECommerce.Application.Options.PentahoOptions>>().Value;
            client.Timeout = TimeSpan.FromSeconds(Math.Clamp(opts.TimeoutSeconds, 10, 180));
            // Pentaho CE no responde bien a HTTP/2 en URL sin TLS (timeout/503).
            client.DefaultRequestVersion = HttpVersion.Version11;
            client.DefaultVersionPolicy = HttpVersionPolicy.RequestVersionExact;
        })
        .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
        {
            AutomaticDecompression = DecompressionMethods.All,
        });
        // Inbound ERP→ecom: solo ecom-etl (Python). Write-back de pedidos sigue en .NET.
        // services.AddHostedService<ErpSyncHostedService>();
        services.AddHostedService<PedidoErpWriteBackHostedService>();
        services.AddScoped<IReviewService, ReviewService>();
        services.AddScoped<IFavoriteService, FavoriteService>();
        services.AddScoped<IContactoService, ContactoService>();
        services.AddScoped<IEngagementService, EngagementService>();
        services.AddScoped<IChatRealtimeNotifier, NullChatRealtimeNotifier>();
        services.AddScoped<ITrackingRealtimeNotifier, NullChatRealtimeNotifier>();

        services.AddScoped<IPermissionService, PermissionService>();

        return services;
    }
}
