using System.Collections.Concurrent;
using System.Diagnostics;
using System.Globalization;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Datamex.ECommerce.Application.Abstractions;
using Datamex.ECommerce.Application.Catalog;
using Datamex.ECommerce.Application.Inventory;
using Datamex.ECommerce.Application.Options;
using Datamex.ECommerce.Application.Pricing;
using Datamex.ECommerce.Application.Search;
using Datamex.ECommerce.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Datamex.ECommerce.Infrastructure.Search;

/// <summary>
/// Cadena: RAM índice local → Redis/MemoryCache resultados → OpenSearch (si URL) → EF Contains.
/// Independiente del ERP: lee espejo EcomDb; sync ERP alimenta el índice vía rebuild.
/// </summary>
public sealed class ProductSearchService : IProductSearchService
{
    private static readonly ConcurrentDictionary<long, TenantIndex> Indexes = new();
    private static readonly JsonSerializerOptions JsonOpts = new(JsonSerializerDefaults.Web);

    private readonly EcomDbContext _db;
    private readonly ITenantContext _tenant;
    private readonly IPricingService _pricing;
    private readonly IInventoryService _inventory;
    private readonly ICatalogService _catalog;
    private readonly SearchOptions _opts;
    private readonly IMemoryCache _memory;
    private readonly IDistributedCache _distributed;
    private readonly IHttpClientFactory _http;
    private readonly ILogger<ProductSearchService> _log;

    public ProductSearchService(
        EcomDbContext db,
        ITenantContext tenant,
        IPricingService pricing,
        IInventoryService inventory,
        ICatalogService catalog,
        IOptions<SearchOptions> opts,
        IMemoryCache memory,
        IDistributedCache distributed,
        IHttpClientFactory http,
        ILogger<ProductSearchService> log)
    {
        _db = db;
        _tenant = tenant;
        _pricing = pricing;
        _inventory = inventory;
        _catalog = catalog;
        _opts = opts.Value;
        _memory = memory;
        _distributed = distributed;
        _http = http;
        _log = log;
    }

    public async Task<SearchSuggestResult> SuggestAsync(
        string query, string canal = "B2C", int take = 8, CancellationToken ct = default)
    {
        take = Math.Clamp(take, 1, 24);
        return await SearchCoreAsync(query, canal, page: 1, pageSize: take, suggest: true, ct);
    }

    public async Task<SearchSuggestResult> SearchAsync(
        string query, string canal = "B2C", int page = 1, int pageSize = 24, CancellationToken ct = default)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);
        return await SearchCoreAsync(query, canal, page, pageSize, suggest: false, ct);
    }

    public async Task<SearchSnapshotDto> GetSnapshotAsync(
        string canal = "B2C", int? maxDocs = null, CancellationToken ct = default)
    {
        var idEmpresa = RequireTenant();
        await EnsureIndexAsync(idEmpresa, canal, ct);
        var idx = Indexes.GetValueOrDefault(idEmpresa);
        var max = Math.Clamp(maxDocs ?? _opts.SnapshotMaxDocs, 50, 20_000);
        var docs = (idx?.Docs ?? new List<IndexedDoc>())
            .Take(max)
            .Select(ToDto)
            .ToList();

        return new SearchSnapshotDto(
            idEmpresa,
            _tenant.Slug,
            idx?.BuiltUtc ?? DateTime.UtcNow,
            docs.Count,
            docs,
            ResolveEngineLabel());
    }

    public async Task RebuildIndexAsync(long? idEmpresa = null, CancellationToken ct = default)
    {
        var id = idEmpresa ?? RequireTenant();
        await BuildIndexAsync(id, "B2C", ct);
        // B2B se construye on-demand; invalidar caches de resultados.
        await InvalidateResultCachesAsync(id, ct);
        _log.LogInformation("Search index rebuilt for empresa {E} docs={N}", id,
            Indexes.GetValueOrDefault(id)?.Docs.Count ?? 0);
    }

    public Task ApplyPatchAsync(long idEmpresa, SearchPatchDto patch, CancellationToken ct = default)
    {
        if (!Indexes.TryGetValue(idEmpresa, out var idx))
            return Task.CompletedTask;

        lock (idx.Gate)
        {
            var i = idx.Docs.FindIndex(d => d.Id == patch.Id);
            if (i < 0) return Task.CompletedTask;
            var d = idx.Docs[i];
            idx.Docs[i] = d with
            {
                StockLibre = patch.StockLibre ?? d.StockLibre,
                StockOk = patch.StockOk ?? d.StockOk,
                PrecioDesde = patch.PrecioDesde ?? d.PrecioDesde,
            };
        }

        return InvalidateResultCachesAsync(idEmpresa, ct);
    }

    private async Task<SearchSuggestResult> SearchCoreAsync(
        string query, string canal, int page, int pageSize, bool suggest, CancellationToken ct)
    {
        var sw = Stopwatch.StartNew();
        var q = (query ?? "").Trim();
        if (!_opts.Enabled || q.Length < 1)
        {
            return new SearchSuggestResult(q, Array.Empty<SearchDocDto>(), "none", 0, false);
        }

        var idEmpresa = RequireTenant();
        var cacheKey = $"search:{idEmpresa}:{canal}:{q.ToLowerInvariant()}:{page}:{pageSize}:{(suggest ? "s" : "f")}";

        if (await TryGetCachedAsync(cacheKey, ct) is { } cached)
        {
            sw.Stop();
            return cached with { TookMs = (int)sw.ElapsedMilliseconds, FromCache = true };
        }

        // 1) OpenSearch si configurado y Engine lo pide
        if (UseOpenSearch)
        {
            try
            {
                var os = await SearchOpenSearchAsync(idEmpresa, q, page, pageSize, ct);
                if (os is not null)
                {
                    await SetCachedAsync(cacheKey, os, ct);
                    sw.Stop();
                    return os with { TookMs = (int)sw.ElapsedMilliseconds, FromCache = false };
                }
            }
            catch (Exception ex)
            {
                _log.LogWarning(ex, "OpenSearch falló; fallback a índice local.");
            }
        }

        // 2) Índice local en RAM
        await EnsureIndexAsync(idEmpresa, canal, ct);
        if (Indexes.TryGetValue(idEmpresa, out var idx) && idx.Docs.Count > 0)
        {
            var local = FilterLocal(idx, q, page, pageSize);
            var result = new SearchSuggestResult(q, local, "local-index", 0, false);
            await SetCachedAsync(cacheKey, result, ct);
            sw.Stop();
            return result with { TookMs = (int)sw.ElapsedMilliseconds };
        }

        // 3) Fallback EF (catálogo clásico)
        var pageDb = await _catalog.SearchProductsAsync(
            new CatalogQuery(q, null, null, null, null, canal, page, pageSize), ct);
        var mapped = pageDb.Items.Select(p => new SearchDocDto(
            p.Id, p.Sku, p.Nombre, p.NombreCorto, p.Marca, p.ImagenPrincipalUrl,
            p.PrecioDesde, p.StockLibre, p.StockOk, p.SlugSeo, p.EstadoEcommerce)).ToList();
        var efResult = new SearchSuggestResult(q, mapped, "ef-catalog", 0, false);
        await SetCachedAsync(cacheKey, efResult, ct);
        sw.Stop();
        return efResult with { TookMs = (int)sw.ElapsedMilliseconds };
    }

    private static IReadOnlyList<SearchDocDto> FilterLocal(TenantIndex idx, string query, int page, int pageSize)
    {
        var norm = Normalize(query);
        var tokens = Tokenize(norm);
        List<(IndexedDoc Doc, int Score)> scored;
        lock (idx.Gate)
        {
            scored = new List<(IndexedDoc, int)>(Math.Min(idx.Docs.Count, 256));
            foreach (var d in idx.Docs)
            {
                var score = Score(d, norm, tokens);
                if (score > 0)
                    scored.Add((d, score));
            }
        }

        return scored
            .OrderByDescending(x => x.Score)
            .ThenBy(x => x.Doc.Nombre)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => ToDto(x.Doc))
            .ToList();
    }

    private static int Score(IndexedDoc d, string norm, string[] tokens)
    {
        var score = 0;
        if (!string.IsNullOrEmpty(d.SkuNorm) && d.SkuNorm == norm) score += 100;
        if (!string.IsNullOrEmpty(d.SkuNorm) && d.SkuNorm.StartsWith(norm, StringComparison.Ordinal)) score += 80;
        if (!string.IsNullOrEmpty(d.SkuNorm) && d.SkuNorm.Contains(norm, StringComparison.Ordinal)) score += 55;
        if (d.NombreNorm.StartsWith(norm, StringComparison.Ordinal)) score += 70;
        if (d.NombreNorm.Contains(norm, StringComparison.Ordinal)) score += 40;
        if (!string.IsNullOrEmpty(d.NombreCortoNorm) && d.NombreCortoNorm.Contains(norm, StringComparison.Ordinal))
            score += 35;
        if (!string.IsNullOrEmpty(d.DescNorm) && d.DescNorm.Contains(norm, StringComparison.Ordinal))
            score += 20;
        if (!string.IsNullOrEmpty(d.MarcaNorm) && d.MarcaNorm.Contains(norm, StringComparison.Ordinal)) score += 25;
        foreach (var t in tokens)
        {
            if (t.Length < 2) continue;
            if (d.NombreNorm.Contains(t, StringComparison.Ordinal)) score += 10;
            if (!string.IsNullOrEmpty(d.SkuNorm) && d.SkuNorm.Contains(t, StringComparison.Ordinal)) score += 12;
            if (!string.IsNullOrEmpty(d.NombreCortoNorm) && d.NombreCortoNorm.Contains(t, StringComparison.Ordinal))
                score += 8;
            if (!string.IsNullOrEmpty(d.DescNorm) && d.DescNorm.Contains(t, StringComparison.Ordinal)) score += 4;
            // tolerancia tipográfica simple: prefijo de token
            if (d.Tokens.Any(tok => tok.StartsWith(t, StringComparison.Ordinal) || t.StartsWith(tok, StringComparison.Ordinal)))
                score += 5;
        }

        // fuzzy leve: distancia de edición ≤ 2 sobre primer token vs nombre
        if (tokens.Length > 0 && tokens[0].Length >= 4)
        {
            var first = tokens[0];
            foreach (var tok in d.Tokens.Take(8))
            {
                if (Math.Abs(tok.Length - first.Length) > 2) continue;
                if (Levenshtein(tok, first) <= 2) { score += 15; break; }
            }
        }

        return score;
    }

    private async Task EnsureIndexAsync(long idEmpresa, string canal, CancellationToken ct)
    {
        if (Indexes.TryGetValue(idEmpresa, out var existing)
            && (DateTime.UtcNow - existing.BuiltUtc) < TimeSpan.FromMinutes(Math.Max(1, _opts.IndexRefreshMinutes)))
            return;
        await BuildIndexAsync(idEmpresa, canal, ct);
    }

    private async Task BuildIndexAsync(long idEmpresa, string canal, CancellationToken ct)
    {
        var canalEff = await _pricing.ResolveEffectiveCanalAsync(canal, ct);
        var lista = await _pricing.ResolveListaAsync(canalEff, null, ct);
        if (lista is null)
        {
            Indexes[idEmpresa] = new TenantIndex(DateTime.UtcNow, new List<IndexedDoc>());
            return;
        }

        var arts = await _db.Articulos.AsNoTracking()
            .Where(a => a.IDEmpresa == idEmpresa && a.Activo && a.ActivoEcommerce
                        && (a.EstadoEcommerce == "activo" || a.EstadoEcommerce == "agotado"))
            .Select(a => new
            {
                a.ID,
                a.SKU,
                a.DescripcionArticulo,
                a.DescripcionArticuloAbreviada,
                a.DescripcionArticuloExtensa,
                a.NombreMarca,
                a.EstadoEcommerce,
                a.SlugSeo,
            })
            .Take(50_000)
            .ToListAsync(ct);

        var artIds = arts.Select(a => a.ID).ToList();
        var imgs = await _db.ArticuloImagenes.AsNoTracking()
            .Where(i => i.IDEmpresa == idEmpresa && artIds.Contains(i.IDArticulo) && i.Activo)
            .GroupBy(i => i.IDArticulo)
            .Select(g => new { IdArticulo = g.Key, Url = g.OrderByDescending(x => x.EsPrincipal).ThenBy(x => x.Orden).Select(x => x.Url).FirstOrDefault() })
            .ToDictionaryAsync(x => x.IdArticulo, x => x.Url, ct);

        var prices = await _pricing.GetProductPriceSummariesAsync(artIds, canalEff, ct);
        var stockMap = await _inventory.GetStockBatchAsync(artIds, null, ct);

        var docs = new List<IndexedDoc>(arts.Count);
        foreach (var a in arts)
        {
            prices.TryGetValue(a.ID, out var px);
            // Incluir también sin precio para búsqueda por SKU/descripción (precio null en suggest).
            var nombre = a.DescripcionArticulo ?? "";
            var nombreNorm = Normalize(nombre);
            var cortoNorm = Normalize(a.DescripcionArticuloAbreviada);
            var descNorm = Normalize(a.DescripcionArticuloExtensa);
            var skuNorm = Normalize(a.SKU);
            var marcaNorm = Normalize(a.NombreMarca);
            var tokens = Tokenize($"{nombreNorm} {cortoNorm} {descNorm} {skuNorm} {marcaNorm}");
            imgs.TryGetValue(a.ID, out var img);
            stockMap.TryGetValue(a.ID, out var stock);
            var libre = stock?.CantidadLibre ?? 0m;
            var ok = a.EstadoEcommerce != "agotado" && libre > 0;

            docs.Add(new IndexedDoc(
                a.ID,
                a.SKU,
                nombre,
                a.DescripcionArticuloAbreviada,
                a.NombreMarca,
                img,
                px?.PrecioDesde,
                libre,
                ok,
                a.SlugSeo,
                a.EstadoEcommerce,
                nombreNorm,
                skuNorm,
                marcaNorm,
                cortoNorm,
                descNorm,
                tokens));
        }

        Indexes[idEmpresa] = new TenantIndex(DateTime.UtcNow, docs);
    }

    private async Task<SearchSuggestResult?> SearchOpenSearchAsync(
        long idEmpresa, string q, int page, int pageSize, CancellationToken ct)
    {
        var url = _opts.OpenSearchUrl!.TrimEnd('/');
        var index = $"{_opts.OpenSearchIndexPrefix}{idEmpresa}";
        var body = new
        {
            from = (page - 1) * pageSize,
            size = pageSize,
            query = new
            {
                multi_match = new
                {
                    query = q,
                    fields = new[] { "nombre^3", "sku^4", "marca^2", "nombreCorto" },
                    fuzziness = "AUTO",
                    type = "best_fields"
                }
            }
        };

        var client = _http.CreateClient("opensearch");
        using var res = await client.PostAsJsonAsync($"{url}/{index}/_search", body, ct);
        if (!res.IsSuccessStatusCode) return null;

        using var stream = await res.Content.ReadAsStreamAsync(ct);
        using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: ct);
        if (!doc.RootElement.TryGetProperty("hits", out var hits)
            || !hits.TryGetProperty("hits", out var arr))
            return null;

        var items = new List<SearchDocDto>();
        foreach (var hit in arr.EnumerateArray())
        {
            if (!hit.TryGetProperty("_source", out var src)) continue;
            items.Add(new SearchDocDto(
                src.TryGetProperty("id", out var id) ? id.GetInt64() : 0,
                src.TryGetProperty("sku", out var sku) ? sku.GetString() : null,
                src.TryGetProperty("nombre", out var n) ? n.GetString() ?? "" : "",
                src.TryGetProperty("nombreCorto", out var nc) ? nc.GetString() : null,
                src.TryGetProperty("marca", out var m) ? m.GetString() : null,
                src.TryGetProperty("imagenPrincipalUrl", out var im) ? im.GetString() : null,
                src.TryGetProperty("precioDesde", out var p) && p.ValueKind == JsonValueKind.Number ? p.GetDecimal() : null,
                src.TryGetProperty("stockLibre", out var s) && s.ValueKind == JsonValueKind.Number ? s.GetDecimal() : null,
                !src.TryGetProperty("stockOk", out var so) || so.ValueKind != JsonValueKind.False,
                src.TryGetProperty("slugSeo", out var sl) ? sl.GetString() : null,
                src.TryGetProperty("estadoEcommerce", out var e) ? e.GetString() : null));
        }

        return new SearchSuggestResult(q, items, "opensearch", 0, false);
    }

    private async Task<SearchSuggestResult?> TryGetCachedAsync(string key, CancellationToken ct)
    {
        // L1 memoria proceso
        if (_memory.TryGetValue(key, out SearchSuggestResult? mem) && mem is not null)
            return mem;

        if (string.IsNullOrWhiteSpace(_opts.RedisConnection))
            return null;

        try
        {
            var bytes = await _distributed.GetAsync(key, ct);
            if (bytes is null || bytes.Length == 0) return null;
            return JsonSerializer.Deserialize<SearchSuggestResult>(bytes, JsonOpts);
        }
        catch (Exception ex)
        {
            _log.LogDebug(ex, "Distributed cache get failed for {Key}", key);
            return null;
        }
    }

    private async Task SetCachedAsync(string key, SearchSuggestResult value, CancellationToken ct)
    {
        var ttl = TimeSpan.FromSeconds(Math.Clamp(_opts.ResultCacheSeconds, 5, 600));
        _memory.Set(key, value, ttl);

        if (string.IsNullOrWhiteSpace(_opts.RedisConnection))
            return;

        try
        {
            var bytes = JsonSerializer.SerializeToUtf8Bytes(value, JsonOpts);
            await _distributed.SetAsync(key, bytes, new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = ttl
            }, ct);
        }
        catch (Exception ex)
        {
            _log.LogDebug(ex, "Distributed cache set failed for {Key}", key);
        }
    }

    private Task InvalidateResultCachesAsync(long idEmpresa, CancellationToken ct)
    {
        // MemoryCache no soporta prefijo-delete; TTL corto mitiga. Redis: best-effort skip.
        _ = idEmpresa;
        _ = ct;
        return Task.CompletedTask;
    }

    private bool UseOpenSearch =>
        !string.IsNullOrWhiteSpace(_opts.OpenSearchUrl)
        && (string.Equals(_opts.Engine, "OpenSearch", StringComparison.OrdinalIgnoreCase)
            || string.Equals(_opts.Engine, "Elasticsearch", StringComparison.OrdinalIgnoreCase));

    private string ResolveEngineLabel()
    {
        if (UseOpenSearch) return "opensearch+local";
        if (!string.IsNullOrWhiteSpace(_opts.RedisConnection)) return "local+redis";
        return "local";
    }

    private long RequireTenant()
    {
        if (!_tenant.IsResolved || !_tenant.IDEmpresa.HasValue)
            throw new InvalidOperationException("Tenant no resuelto.");
        return _tenant.IDEmpresa.Value;
    }

    private static SearchDocDto ToDto(IndexedDoc d) =>
        new(d.Id, d.Sku, d.Nombre, d.NombreCorto, d.Marca, d.ImagenPrincipalUrl,
            d.PrecioDesde, d.StockLibre, d.StockOk, d.SlugSeo, d.EstadoEcommerce);

    private static string Normalize(string? s)
    {
        if (string.IsNullOrWhiteSpace(s)) return "";
        var formD = s.Trim().ToLowerInvariant().Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder(formD.Length);
        foreach (var c in formD)
        {
            var uc = CharUnicodeInfo.GetUnicodeCategory(c);
            if (uc != UnicodeCategory.NonSpacingMark)
                sb.Append(c);
        }
        return Regex.Replace(sb.ToString().Normalize(NormalizationForm.FormC), @"\s+", " ");
    }

    private static string[] Tokenize(string norm) =>
        norm.Split(new[] { ' ', '-', '/', '.', ',', '_', '+', '(', ')' },
                StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(t => t.Length >= 2)
            .Distinct(StringComparer.Ordinal)
            .ToArray();

    private static int Levenshtein(string a, string b)
    {
        if (a == b) return 0;
        if (a.Length == 0) return b.Length;
        if (b.Length == 0) return a.Length;
        var prev = new int[b.Length + 1];
        var cur = new int[b.Length + 1];
        for (var j = 0; j <= b.Length; j++) prev[j] = j;
        for (var i = 1; i <= a.Length; i++)
        {
            cur[0] = i;
            for (var j = 1; j <= b.Length; j++)
            {
                var cost = a[i - 1] == b[j - 1] ? 0 : 1;
                cur[j] = Math.Min(Math.Min(cur[j - 1] + 1, prev[j] + 1), prev[j - 1] + cost);
            }
            (prev, cur) = (cur, prev);
        }
        return prev[b.Length];
    }

    private sealed class TenantIndex(DateTime builtUtc, List<IndexedDoc> docs)
    {
        public DateTime BuiltUtc { get; } = builtUtc;
        public List<IndexedDoc> Docs { get; } = docs;
        public object Gate { get; } = new();
    }

    private sealed record IndexedDoc(
        long Id,
        string? Sku,
        string Nombre,
        string? NombreCorto,
        string? Marca,
        string? ImagenPrincipalUrl,
        decimal? PrecioDesde,
        decimal? StockLibre,
        bool StockOk,
        string? SlugSeo,
        string? EstadoEcommerce,
        string NombreNorm,
        string SkuNorm,
        string MarcaNorm,
        string NombreCortoNorm,
        string DescNorm,
        string[] Tokens);
}
