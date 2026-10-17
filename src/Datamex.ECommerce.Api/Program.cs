using System.IO.Compression;
using System.Text;
using System.Threading.RateLimiting;
using Datamex.ECommerce.Api.Hubs;
using Datamex.ECommerce.Api.Middleware;
using Datamex.ECommerce.Application.Engagement;
using Datamex.ECommerce.Application.Options;
using Datamex.ECommerce.Infrastructure;
using Datamex.ECommerce.Infrastructure.Seed;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.Extensions.FileProviders;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddResponseCompression(o =>
{
    o.EnableForHttps = true;
    o.Providers.Add<BrotliCompressionProvider>();
    o.Providers.Add<GzipCompressionProvider>();
    o.MimeTypes = ResponseCompressionDefaults.MimeTypes.Concat(
        ["application/json", "application/javascript", "text/csv"]);
});
builder.Services.Configure<BrotliCompressionProviderOptions>(o => o.Level = CompressionLevel.Fastest);
builder.Services.Configure<GzipCompressionProviderOptions>(o => o.Level = CompressionLevel.Fastest);

builder.Services.AddECommerceInfrastructure(builder.Configuration);
builder.Services.AddSignalR();
builder.Services.AddSingleton<IChatRealtimeNotifier, SignalRChatRealtimeNotifier>();
builder.Services.AddSingleton<ITrackingRealtimeNotifier>(sp =>
    (ITrackingRealtimeNotifier)sp.GetRequiredService<IChatRealtimeNotifier>());
builder.Services.AddControllers();
builder.Services.Configure<FormOptions>(o =>
{
    o.MultipartBodyLengthLimit = 25_000_000;
});
builder.WebHost.ConfigureKestrel(o => o.Limits.MaxRequestBodySize = 25_000_000);
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Datamex E-Commerce API",
        Version = "v1",
        Description = "API rápida multi-tenant (IDEmpresa) con integración ERP Datamex."
    });
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Bearer. Ejemplo: Bearer {token}",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT"
    });
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
            },
            Array.Empty<string>()
        }
    });
});

builder.Services.AddHealthChecks()
    .AddDbContextCheck<Datamex.ECommerce.Infrastructure.Persistence.EcomDbContext>("database");

var ecomOpts = builder.Configuration.GetSection(ECommerceOptions.SectionName).Get<ECommerceOptions>()
               ?? new ECommerceOptions();

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = ecomOpts.JwtIssuer,
            ValidAudience = ecomOpts.JwtAudience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(ecomOpts.JwtSigningKey))
        };
    });
builder.Services.AddAuthorization();

builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    // Chat/CRM hacen poll; en local storefront+admin comparten IP → subimos el techo.
    var apiLimit = builder.Environment.IsDevelopment() ? 600 : 180;
    options.AddPolicy("api", httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = apiLimit,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0
            }));
    options.AddPolicy("auth", httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 40,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0
            }));
    options.AddPolicy("contacto", httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 10,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0
            }));
});

var corsOrigins = builder.Configuration.GetSection("ECommerce:CorsOrigins").Get<string[]>()
                  ?? new[]
                  {
                      "http://localhost:4200",
                      "http://localhost:4201",
                      "http://127.0.0.1:4200",
                      "http://127.0.0.1:4201",
                  };
builder.Services.AddCors(options =>
{
    options.AddPolicy("AngularDev", policy =>
        policy.WithOrigins(corsOrigins)
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials());
});

// JWT en query string para WebSockets SignalR
builder.Services.PostConfigure<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme, options =>
{
    var existing = options.Events?.OnMessageReceived;
    options.Events ??= new JwtBearerEvents();
    options.Events.OnMessageReceived = async ctx =>
    {
        if (existing is not null)
            await existing(ctx);
        var accessToken = ctx.Request.Query["access_token"];
        var path = ctx.HttpContext.Request.Path;
        if (!string.IsNullOrEmpty(accessToken)
            && (path.StartsWithSegments("/hubs/chat")
                || path.Value?.Contains("/factura.pdf", StringComparison.OrdinalIgnoreCase) == true
                || path.Value?.Contains("/nota-credito.pdf", StringComparison.OrdinalIgnoreCase) == true))
            ctx.Token = accessToken;
    };
});

var app = builder.Build();

// Swagger siempre disponible en local (sin depender solo de IsDevelopment),
// para builds desde artifacts/ y cuando ASPNETCORE_ENVIRONMENT no está seteado.
var enableSwagger = app.Environment.IsDevelopment()
                    || app.Environment.IsEnvironment("Local")
                    || string.Equals(
                        app.Configuration["Swagger:Enabled"],
                        "true",
                        StringComparison.OrdinalIgnoreCase)
                    || !app.Environment.IsProduction();

if (enableSwagger)
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Datamex E-Commerce API v1");
        c.RoutePrefix = "swagger";
    });
    app.MapGet("/", () => Results.Redirect("/swagger/index.html")).ExcludeFromDescription();
}

app.UseCors("AngularDev");
app.UseResponseCompression();
app.UseRateLimiter();

var mediaOpts = app.Configuration.GetSection(MediaOptions.SectionName).Get<MediaOptions>() ?? new MediaOptions();
var mediaRoot = Path.IsPathRooted(mediaOpts.RootPath)
    ? mediaOpts.RootPath
    : Path.GetFullPath(Path.Combine(app.Environment.ContentRootPath, mediaOpts.RootPath));
Directory.CreateDirectory(mediaRoot);
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(mediaRoot),
    RequestPath = mediaOpts.RequestPath.TrimEnd('/').StartsWith('/')
        ? mediaOpts.RequestPath.TrimEnd('/')
        : "/" + mediaOpts.RequestPath.Trim('/'),
    OnPrepareResponse = ctx =>
    {
        // Cache agresivo para assets inmutables (nombre GUID)
        ctx.Context.Response.Headers.CacheControl = "public,max-age=31536000,immutable";
    }
});

app.UseAuthentication();
app.UseMiddleware<TenantResolutionMiddleware>();
app.UseAuthorization();
app.MapControllers();
app.MapHub<ChatHub>("/hubs/chat");
app.MapHealthChecks("/health");

try
{
    await DemoSeed.EnsureSeedAsync(app.Services);
}
catch (Exception ex)
{
    var seedLog = app.Services.GetRequiredService<ILoggerFactory>().CreateLogger("DemoSeed");
    seedLog.LogError(ex, "Seed falló al arrancar; la API continúa (revisar geo/demo).");
}

app.Run();

public partial class Program;
