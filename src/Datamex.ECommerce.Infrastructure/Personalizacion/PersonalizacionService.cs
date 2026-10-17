using System.Text.Json;
using Datamex.ECommerce.Application.Abstractions;
using Datamex.ECommerce.Application.Personalizacion;
using Datamex.ECommerce.Domain.Entities;
using Datamex.ECommerce.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Datamex.ECommerce.Infrastructure.Personalizacion;

public sealed class PersonalizacionService : IPersonalizacionService
{
    private readonly EcomDbContext _db;
    private readonly ITenantContext _tenant;

    public PersonalizacionService(EcomDbContext db, ITenantContext tenant)
    {
        _db = db;
        _tenant = tenant;
    }

    private long EmpresaId => _tenant.IDEmpresa
        ?? throw new InvalidOperationException("Tenant no resuelto.");

    public IReadOnlyDictionary<string, string> GetDefaultSchemas() => new Dictionary<string, string>
    {
        [PersonalizacionClaves.HomeLayout] = DefaultHomeLayout(),
        [PersonalizacionClaves.TemaActivo] = DefaultTema(),
        [PersonalizacionClaves.CheckoutTextos] = DefaultCheckoutTextos(),
        [PersonalizacionClaves.CarritoTextos] = DefaultCarritoTextos(),
        [PersonalizacionClaves.WidgetsToggles] = DefaultWidgets()
    };

    public async Task<PersonalizacionItemDto?> GetActivaAsync(string clave, CancellationToken ct = default)
    {
        var idEmpresa = EmpresaId;
        clave = NormalizeClave(clave);
        var now = DateTime.UtcNow;
        var row = await ResolveActivaAsync(idEmpresa, clave, now, ct);
        return row is null ? MapDefault(clave) : Map(row);
    }

    public async Task<IReadOnlyList<PersonalizacionItemDto>> ListVersionesAsync(string clave, CancellationToken ct = default)
    {
        var idEmpresa = EmpresaId;
        clave = NormalizeClave(clave);
        var rows = await _db.ConfiguracionesPersonalizacion.AsNoTracking()
            .Where(c => c.IDEmpresa == idEmpresa && c.Clave == clave)
            .OrderByDescending(c => c.Version)
            .Take(50)
            .ToListAsync(ct);
        return rows.Select(Map).ToList();
    }

    public async Task<PersonalizacionItemDto> UpsertAsync(
        string clave, UpsertPersonalizacionRequest request, string? userName, CancellationToken ct = default)
    {
        var idEmpresa = EmpresaId;
        clave = NormalizeClave(clave);
        ValidateJson(clave, request.ValorJson);

        var nextVersion = ((await _db.ConfiguracionesPersonalizacion.AsNoTracking()
            .Where(c => c.IDEmpresa == idEmpresa && c.Clave == clave)
            .MaxAsync(c => (int?)c.Version, ct)) ?? 0) + 1;

        if (request.FechaInicioVigencia is null && request.FechaFinVigencia is null)
        {
            var prev = await _db.ConfiguracionesPersonalizacion
                .Where(c => c.IDEmpresa == idEmpresa && c.Clave == clave
                            && c.Activo
                            && c.FechaInicioVigencia == null
                            && c.FechaFinVigencia == null)
                .ToListAsync(ct);
            foreach (var p in prev)
            {
                p.Activo = false;
                p.UltModificacion = DateTime.UtcNow;
            }
        }

        var row = new ConfiguracionPersonalizacion
        {
            IDEmpresa = idEmpresa,
            Clave = clave,
            ValorJson = CompactJson(request.ValorJson),
            Version = nextVersion,
            FechaInicioVigencia = request.FechaInicioVigencia,
            FechaFinVigencia = request.FechaFinVigencia,
            Activo = request.Activo ?? true,
            CreadoPor = userName ?? "admin",
            FechaCreacion = DateTime.UtcNow,
            UltModificacion = DateTime.UtcNow,
            UserName = userName ?? "admin",
            Nota = string.IsNullOrWhiteSpace(request.Nota) ? null : request.Nota.Trim()
        };
        _db.ConfiguracionesPersonalizacion.Add(row);
        await _db.SaveChangesAsync(ct);
        return Map(row);
    }

    public async Task<PersonalizacionItemDto> ProgramarAsync(
        string clave, ProgramarPersonalizacionRequest request, string? userName, CancellationToken ct = default)
    {
        if (request.FechaFinVigencia <= request.FechaInicioVigencia)
            throw new ArgumentException("Fecha fin debe ser posterior a inicio.");
        return await UpsertAsync(clave, new UpsertPersonalizacionRequest(
            request.ValorJson,
            request.FechaInicioVigencia,
            request.FechaFinVigencia,
            request.Nota,
            true), userName, ct);
    }

    public async Task<StorefrontPersonalizacionDto> GetPublicasAsync(CancellationToken ct = default)
    {
        var idEmpresa = EmpresaId;
        var now = DateTime.UtcNow;
        var dict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var clave in PersonalizacionClaves.Publicas)
        {
            var row = await ResolveActivaAsync(idEmpresa, clave, now, ct);
            dict[clave] = row?.ValorJson ?? GetDefaultSchemas()[clave];
        }
        return new StorefrontPersonalizacionDto(dict);
    }

    private async Task<ConfiguracionPersonalizacion?> ResolveActivaAsync(
        long idEmpresa, string clave, DateTime now, CancellationToken ct)
    {
        var scheduled = await _db.ConfiguracionesPersonalizacion.AsNoTracking()
            .Where(c => c.IDEmpresa == idEmpresa && c.Clave == clave && c.Activo
                        && c.FechaInicioVigencia != null && c.FechaFinVigencia != null
                        && c.FechaInicioVigencia <= now && c.FechaFinVigencia >= now)
            .OrderByDescending(c => c.Version)
            .FirstOrDefaultAsync(ct);
        if (scheduled is not null) return scheduled;

        return await _db.ConfiguracionesPersonalizacion.AsNoTracking()
            .Where(c => c.IDEmpresa == idEmpresa && c.Clave == clave && c.Activo
                        && c.FechaInicioVigencia == null && c.FechaFinVigencia == null)
            .OrderByDescending(c => c.Version)
            .FirstOrDefaultAsync(ct);
    }

    private void ValidateJson(string clave, string json)
    {
        if (string.IsNullOrWhiteSpace(json))
            throw new ArgumentException("ValorJson requerido.");
        JsonDocument doc;
        try
        {
            doc = JsonDocument.Parse(json);
        }
        catch (JsonException ex)
        {
            throw new ArgumentException($"JSON inválido: {ex.Message}");
        }

        using (doc)
        {
            if (doc.RootElement.ValueKind != JsonValueKind.Object)
                throw new ArgumentException("ValorJson debe ser un objeto JSON.");

            var root = doc.RootElement;
            switch (clave)
            {
                case PersonalizacionClaves.HomeLayout:
                    if (!root.TryGetProperty("sections", out var secs) || secs.ValueKind != JsonValueKind.Array)
                        throw new ArgumentException("home.layout requiere 'sections' (array).");
                    break;
                case PersonalizacionClaves.TemaActivo:
                    if (!root.TryGetProperty("nombre", out var nom) || nom.ValueKind != JsonValueKind.String
                        || string.IsNullOrWhiteSpace(nom.GetString()))
                        throw new ArgumentException("tema.activo requiere 'nombre'.");
                    break;
                case PersonalizacionClaves.CheckoutTextos:
                    if (!root.TryGetProperty("titulo", out var tit) || tit.ValueKind != JsonValueKind.String
                        || string.IsNullOrWhiteSpace(tit.GetString()))
                        throw new ArgumentException("checkout.textos requiere 'titulo'.");
                    break;
                case PersonalizacionClaves.CarritoTextos:
                    if (!root.TryGetProperty("ctaCheckout", out var cta) || cta.ValueKind != JsonValueKind.String
                        || string.IsNullOrWhiteSpace(cta.GetString()))
                        throw new ArgumentException("carrito.textos requiere 'ctaCheckout'.");
                    break;
                case PersonalizacionClaves.WidgetsToggles:
                    // todos los flags son opcionales boolean
                    break;
            }
        }
    }

    private static string CompactJson(string json)
    {
        using var doc = JsonDocument.Parse(json);
        return JsonSerializer.Serialize(doc.RootElement);
    }

    private static string NormalizeClave(string clave)
    {
        var c = (clave ?? "").Trim().ToLowerInvariant();
        if (!PersonalizacionClaves.Todas.Contains(c))
            throw new ArgumentException($"Clave no soportada: {clave}");
        return c;
    }

    private PersonalizacionItemDto MapDefault(string clave) =>
        new(clave, GetDefaultSchemas()[clave], 0, null, null, true, DateTime.UtcNow, "default", "Valor por defecto");

    private static PersonalizacionItemDto Map(ConfiguracionPersonalizacion c) =>
        new(c.Clave, c.ValorJson, c.Version, c.FechaInicioVigencia, c.FechaFinVigencia,
            c.Activo, c.FechaCreacion, c.CreadoPor, c.Nota);

    private static string DefaultHomeLayout() =>
        """{"sections":[{"id":"hero","visible":true,"orden":1},{"id":"categorias","visible":true,"orden":2},{"id":"publicidad","visible":true,"orden":3},{"id":"flyers","visible":true,"orden":4},{"id":"recomendados","visible":true,"orden":5},{"id":"masVendidos","visible":true,"orden":6},{"id":"newsletter","visible":true,"orden":7},{"id":"catalogoCta","visible":true,"orden":8}],"mostrarBannerMayorista":true,"mostrarTopBar":true,"mostrarTrust":true,"mostrarCupon":false,"mostrarProductosOferta":true,"mostrarHeroFeature":true}""";

    private static string DefaultTema() =>
        """{"nombre":"default","primary":"#0f766e","accent":"#b45309","ink":"#1c1917","paper":"#fafaf9","displayFont":null,"bannerUrl":null}""";

    private static string DefaultCheckoutTextos() =>
        """{"titulo":"Finalizar compra","subtitulo":"Completá tus datos y la forma de entrega","ctaConfirmar":"Confirmar pedido","hintMapa":"Tocá el mapa para marcar la ubicación.","msgExito":"¡Gracias por tu compra!"}""";

    private static string DefaultCarritoTextos() =>
        """{"titulo":"Carrito","seguirComprando":"Seguir comprando","vacioTitulo":"Tu carrito está vacío.","vacioSub":"Agregá productos desde el catálogo para empezar.","ctaCatalogo":"Ir al catálogo","hintEnvio":"El costo de envío se calcula en el checkout.","ctaVaciar":"Vaciar","ctaGuardarLista":"Guardar como lista","ctaCheckout":"Continuar al checkout","ctaStockBlock":"Revisá stock para continuar"}""";

    private static string DefaultWidgets() =>
        """{"chat":true,"resenas":true,"favoritos":true,"seguidos":true,"bannerMayorista":true,"facebook":true,"instagram":true,"whatsapp":true,"youtube":true,"twitter":true,"tiktok":true}""";
}
