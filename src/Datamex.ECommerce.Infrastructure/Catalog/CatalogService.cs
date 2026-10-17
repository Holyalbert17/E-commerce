using Datamex.ECommerce.Application.Abstractions;
using Datamex.ECommerce.Application.Catalog;
using Datamex.ECommerce.Application.Integration;
using Datamex.ECommerce.Application.Inventory;
using Datamex.ECommerce.Application.Options;
using Datamex.ECommerce.Application.Pricing;
using Datamex.ECommerce.Domain.Entities;
using Datamex.ECommerce.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Datamex.ECommerce.Infrastructure.Catalog;

public sealed class CatalogService : ICatalogService
{
    private static readonly string[] VisibleStates = ["activo", "agotado"];

    private readonly EcomDbContext _db;
    private readonly ITenantContext _tenant;
    private readonly ECommerceOptions _options;
    private readonly IPricingService _pricing;
    private readonly IInventoryService _inventory;
    private readonly ILogger<CatalogService> _logger;

    public CatalogService(
        EcomDbContext db,
        ITenantContext tenant,
        IOptions<ECommerceOptions> options,
        IPricingService pricing,
        IInventoryService inventory,
        ILogger<CatalogService> logger)
    {
        _db = db;
        _tenant = tenant;
        _options = options.Value;
        _pricing = pricing;
        _inventory = inventory;
        _logger = logger;
    }

    public async Task<CatalogPingResponse> PingAsync(CancellationToken ct = default)
    {
        var idEmpresa = RequireTenant();

        var count = await _db.Articulos
            .AsNoTracking()
            .CountAsync(a => a.IDEmpresa == idEmpresa && a.Activo && a.ActivoEcommerce, ct);

        var slug = _tenant.Slug ?? await _db.EmpresaConfiguracionesEcommerce
            .AsNoTracking()
            .Where(c => c.ID == idEmpresa)
            .Select(c => c.SlugTienda)
            .FirstOrDefaultAsync(ct);

        var cfg = await _db.EmpresaConfiguracionesEcommerce.AsNoTracking()
            .FirstOrDefaultAsync(c => c.ID == idEmpresa, ct);
        var mode = TenantIntegrationMode.Resolve(cfg, _options.IntegrationMode);

        return new CatalogPingResponse(
            idEmpresa,
            slug,
            mode.ToString(),
            count,
            DateTime.UtcNow);
    }

    public async Task<PagedResult<ProductListItemDto>> SearchProductsAsync(CatalogQuery query, CancellationToken ct = default)
    {
        var idEmpresa = RequireTenant();
        var page = Math.Max(1, query.Page);
        var pageSize = Math.Clamp(query.PageSize, 1, 100);
        var canal = await _pricing.ResolveEffectiveCanalAsync(query.Canal, ct);

        var q = BaseVisibleProducts(idEmpresa);

        // Paridad ERP: solo artículos con precio > 0 en lista ecommerce del canal.
        var lista = await _pricing.ResolveListaAsync(canal, null, ct);
        if (lista is null)
        {
            return new PagedResult<ProductListItemDto>(Array.Empty<ProductListItemDto>(), page, pageSize, 0);
        }

        q = q.Where(a => _db.ArticuloEmbalajes.Any(e =>
            e.IDEmpresa == idEmpresa
            && e.IDArticulo == a.ID
            && e.Activo
            && (
                _db.DetallesListaPrecioCondicionVenta.Any(d =>
                    d.IDEmpresa == idEmpresa
                    && d.IDListaPrecio == lista.Id
                    && d.IDArticuloEmbalaje == e.ID
                    && d.Activo
                    && d.PrecioContado > 0)
                || _db.DetallesListaPrecio.Any(d =>
                    d.IDEmpresa == idEmpresa
                    && d.IDListaPrecio == lista.Id
                    && d.IDArticuloEmbalaje == e.ID
                    && d.Activo
                    && d.Precio > 0))));

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var term = query.Search.Trim().ToLowerInvariant();
            q = q.Where(a =>
                (a.SKU != null && a.SKU.ToLower().Contains(term))
                || a.DescripcionArticulo.ToLower().Contains(term)
                || a.DescripcionArticuloAbreviada.ToLower().Contains(term)
                || (a.DescripcionArticuloExtensa != null && a.DescripcionArticuloExtensa.ToLower().Contains(term))
                || (a.NombreMarca != null && a.NombreMarca.ToLower().Contains(term))
                || (a.CodReferenciaProveedor != null && a.CodReferenciaProveedor.ToLower().Contains(term)));
        }

        if (!string.IsNullOrWhiteSpace(query.Estado))
            q = q.Where(a => a.EstadoEcommerce == query.Estado);

        if (query.MarcaId.HasValue)
            q = q.Where(a => a.IDMarca == query.MarcaId.Value);

        if (query.LineaId.HasValue)
            q = q.Where(a => a.IDLinea == query.LineaId.Value);

        if (query.TipoId.HasValue)
            q = q.Where(a => a.IDTipoArticulo == query.TipoId.Value);

        if (query.GrupoId.HasValue)
        {
            var grupoId = query.GrupoId.Value;
            q = q.Where(a => _db.DetallesGrupoArticuloEcommerce.Any(d =>
                d.IDEmpresa == idEmpresa
                && d.IDGrupoArticuloEcommerce == grupoId
                && d.IDArticulo == a.ID
                && d.Activo));
        }

        q = (query.Sort?.ToLowerInvariant()) switch
        {
            "nombre_desc" => q.OrderByDescending(a => a.DescripcionArticulo),
            "sku" => q.OrderBy(a => a.SKU),
            "nuevo" => q.OrderByDescending(a => a.ID),
            _ => q.OrderBy(a => a.DescripcionArticulo)
        };

        var total = await q.CountAsync(ct);

        var rows = await q
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(a => new
            {
                a.ID,
                a.SKU,
                a.DescripcionArticulo,
                a.DescripcionArticuloAbreviada,
                a.NombreMarca,
                a.EstadoEcommerce,
                a.SlugSeo,
                Imagen = a.Imagenes
                    .Where(i => i.Activo)
                    .OrderByDescending(i => i.EsPrincipal)
                    .ThenBy(i => i.Orden)
                    .Select(i => i.Url)
                    .FirstOrDefault(),
                EmbalajesCount = a.Embalajes.Count(e => e.Activo)
            })
            .ToListAsync(ct);

        var priceMap = await _pricing.GetProductPriceSummariesAsync(rows.Select(r => r.ID), canal, ct);
        var stockMap = await _inventory.GetStockBatchAsync(rows.Select(r => r.ID), null, ct);

        var items = rows.Select(a =>
        {
            priceMap.TryGetValue(a.ID, out var price);
            stockMap.TryGetValue(a.ID, out var stock);
            var libre = stock?.CantidadLibre ?? 0m;
            // Badge según espejo ERP (no según ControlaStockEnCarrito — Integrated lo deja en false).
            var ok = a.EstadoEcommerce != "agotado" && libre > 0;

            return new ProductListItemDto(
                a.ID,
                a.SKU,
                a.DescripcionArticulo,
                a.DescripcionArticuloAbreviada,
                a.NombreMarca,
                a.EstadoEcommerce,
                a.Imagen,
                price?.PrecioDesde,
                a.EmbalajesCount,
                canal,
                price?.ListaDescripcion,
                libre,
                ok,
                a.SlugSeo,
                price?.PrecioLista,
                price?.PorcentajeDescuento);
        }).ToList();

        return new PagedResult<ProductListItemDto>(items, page, pageSize, total);
    }

    public async Task<ProductDetailDto?> GetProductAsync(long id, string canal = "B2C", CancellationToken ct = default)
    {
        var idEmpresa = RequireTenant();
        canal = await _pricing.ResolveEffectiveCanalAsync(canal, ct);

        var detail = await TryMapVisibleProductAsync(idEmpresa, id, sku: null, slug: null, canal, ct);
        if (detail is not null) return detail;

        // Inbound solo vía ecom-etl — sin lazy-pull C#.
        return null;
    }

    public async Task<ProductDetailDto?> GetProductBySkuAsync(string sku, string canal = "B2C", CancellationToken ct = default)
    {
        var idEmpresa = RequireTenant();
        canal = await _pricing.ResolveEffectiveCanalAsync(canal, ct);

        var detail = await TryMapVisibleProductAsync(idEmpresa, null, sku, null, canal, ct);
        if (detail is not null) return detail;

        return null;
    }

    public async Task<ProductDetailDto?> GetProductBySlugAsync(string slug, string canal = "B2C", CancellationToken ct = default)
    {
        var idEmpresa = RequireTenant();
        canal = await _pricing.ResolveEffectiveCanalAsync(canal, ct);
        if (string.IsNullOrWhiteSpace(slug)) return null;
        return await TryMapVisibleProductAsync(idEmpresa, null, null, slug.Trim(), canal, ct);
    }

    private async Task<ProductDetailDto?> TryMapVisibleProductAsync(
        long idEmpresa, long? id, string? sku, string? slug, string canal, CancellationToken ct)
    {
        var q = BaseVisibleProducts(idEmpresa)
            .Include(a => a.Embalajes.Where(e => e.Activo))
            .Include(a => a.Imagenes.Where(i => i.Activo));

        Articulo? articulo;
        if (id is > 0)
            articulo = await q.FirstOrDefaultAsync(a => a.ID == id.Value, ct);
        else if (!string.IsNullOrWhiteSpace(slug))
            articulo = await q.FirstOrDefaultAsync(a => a.SlugSeo == slug, ct);
        else if (!string.IsNullOrWhiteSpace(sku))
            articulo = await q.FirstOrDefaultAsync(a => a.SKU == sku, ct);
        else
            return null;

        if (articulo is null) return null;

        // Por id/slug exigimos precio > 0 (paridad listado). Por SKU devolvemos detalle si existe.
        if (id is > 0 || !string.IsNullOrWhiteSpace(slug))
        {
            var prices = await _pricing.GetEmbalajePricesAsync(articulo.ID, canal, ct);
            if (prices.Count == 0 || prices.All(p => p.PrecioContado <= 0))
                return null;
        }

        return await MapDetailAsync(articulo, canal, ct);
    }

    public async Task<IReadOnlyList<CollectionDto>> GetCollectionsAsync(int? tipoGrupo = null, CancellationToken ct = default)
    {
        var idEmpresa = RequireTenant();
        var now = DateTime.UtcNow;

        var q = _db.GruposArticuloEcommerce
            .AsNoTracking()
            .Where(g => g.IDEmpresa == idEmpresa
                        && g.Activo
                        && g.FechaInicioVigencia <= now
                        && g.FechaFinVigencia >= now);
        if (tipoGrupo.HasValue)
            q = q.Where(g => g.TipoGrupo == tipoGrupo.Value);

        return await q
            .OrderBy(g => g.Orden)
            .ThenBy(g => g.DescripcionGrupoArticulo)
            .Select(g => new CollectionDto(
                g.ID,
                g.CodGrupoArticulo,
                g.DescripcionGrupoArticulo,
                g.Imagen,
                g.DetalleGrupoArticuloEcommerces.Count(d => d.Activo),
                g.TipoGrupo,
                g.UrlDestino,
                g.Subtitulo))
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<MarketingSlotDto>> GetMarketingSlotsAsync(CancellationToken ct = default)
    {
        var idEmpresa = RequireTenant();
        var now = DateTime.UtcNow;

        // TipoGrupo: 2 = Publicitados, 3 = Promoción (flyer/oferta)
        var rows = await _db.GruposArticuloEcommerce.AsNoTracking()
            .Where(g => g.IDEmpresa == idEmpresa
                        && g.Activo
                        && (g.TipoGrupo == 2 || g.TipoGrupo == 3)
                        && g.Imagen != null && g.Imagen != ""
                        && g.FechaInicioVigencia <= now
                        && g.FechaFinVigencia >= now)
            .OrderBy(g => g.Orden)
            .ThenBy(g => g.DescripcionGrupoArticulo)
            .Select(g => new MarketingSlotDto(
                g.ID,
                g.TipoGrupo,
                g.TipoGrupo == 3 ? "Oferta" : "Publicidad",
                g.DescripcionGrupoArticulo,
                g.Subtitulo,
                g.Imagen,
                g.UrlDestino,
                g.Orden))
            .ToListAsync(ct);
        return rows;
    }

    public async Task<IReadOnlyList<CatalogMarcaDto>> GetMarcasAsync(CancellationToken ct = default)
    {
        var idEmpresa = RequireTenant();
        // Distinct + materialize: Npgsql/EF no traduce GroupBy → DTO ctor en OrderBy.
        var rows = await _db.Articulos.AsNoTracking()
            .Where(a => a.IDEmpresa == idEmpresa && a.Activo && a.ActivoEcommerce && a.IDMarca > 0
                        && a.NombreMarca != null && a.NombreMarca != "")
            .Select(a => new { a.IDMarca, a.NombreMarca })
            .Distinct()
            .OrderBy(x => x.NombreMarca)
            .ToListAsync(ct);
        return rows.Select(x => new CatalogMarcaDto(x.IDMarca, x.NombreMarca!)).ToList();
    }

    public async Task<IReadOnlyList<CatalogFacetDto>> GetLineasAsync(CancellationToken ct = default)
    {
        var idEmpresa = RequireTenant();
        var fromParam = await _db.CatalogoParametros.AsNoTracking()
            .Where(p => p.IDEmpresa == idEmpresa && p.Activo && p.Tipo == CatalogoParametroTipos.Linea)
            .OrderBy(p => p.Descripcion)
            .Select(p => new CatalogFacetDto(p.ID, p.Descripcion))
            .ToListAsync(ct);
        if (fromParam.Count > 0)
            return fromParam;

        var rows = await _db.Articulos.AsNoTracking()
            .Where(a => a.IDEmpresa == idEmpresa && a.Activo && a.ActivoEcommerce
                        && a.IDLinea != null && a.IDLinea > 0
                        && a.NombreLinea != null && a.NombreLinea != "")
            .Select(a => new { a.IDLinea, a.NombreLinea })
            .Distinct()
            .OrderBy(x => x.NombreLinea)
            .ToListAsync(ct);
        return rows.Select(x => new CatalogFacetDto(x.IDLinea!.Value, x.NombreLinea!)).ToList();
    }

    public async Task<IReadOnlyList<CatalogFacetDto>> GetTiposAsync(CancellationToken ct = default)
    {
        var idEmpresa = RequireTenant();
        var fromParam = await _db.CatalogoParametros.AsNoTracking()
            .Where(p => p.IDEmpresa == idEmpresa && p.Activo && p.Tipo == CatalogoParametroTipos.Tipo)
            .OrderBy(p => p.Descripcion)
            .Select(p => new CatalogFacetDto(p.ID, p.Descripcion))
            .ToListAsync(ct);
        if (fromParam.Count > 0)
            return fromParam;

        var rows = await _db.Articulos.AsNoTracking()
            .Where(a => a.IDEmpresa == idEmpresa && a.Activo && a.ActivoEcommerce
                        && a.IDTipoArticulo != null && a.IDTipoArticulo > 0
                        && a.NombreTipoArticulo != null && a.NombreTipoArticulo != "")
            .Select(a => new { a.IDTipoArticulo, a.NombreTipoArticulo })
            .Distinct()
            .OrderBy(x => x.NombreTipoArticulo)
            .ToListAsync(ct);
        return rows.Select(x => new CatalogFacetDto(x.IDTipoArticulo!.Value, x.NombreTipoArticulo!)).ToList();
    }

    public async Task<IReadOnlyList<ProductListItemDto>> GetMasVendidosAsync(
        int take = 10, string canal = "B2C", CancellationToken ct = default)
    {
        var idEmpresa = RequireTenant();
        take = Math.Clamp(take, 1, 50);
        canal = await _pricing.ResolveEffectiveCanalAsync(canal, ct);

        var topIds = await (
            from d in _db.DetallesPedidoMovil.AsNoTracking()
            join p in _db.PedidosMovil.AsNoTracking() on d.IDPedidoMovil equals p.ID
            join a in BaseVisibleProducts(idEmpresa) on d.IDArticulo equals a.ID
            where p.IDEmpresa == idEmpresa
                  && p.Activo
                  && p.Estado != EstadoPedidoMovil.Anulado
                  && p.OrigenPedido == OrigenPedidoMovil.Ecommerce
                  && d.Activo
            group d by d.IDArticulo into g
            orderby g.Sum(x => (decimal)x.CantidadEmbalada) descending
            select g.Key
        ).Take(take).ToListAsync(ct);

        if (topIds.Count == 0)
            return Array.Empty<ProductListItemDto>();

        var rows = await BaseVisibleProducts(idEmpresa)
            .Where(a => topIds.Contains(a.ID))
            .Select(a => new
            {
                a.ID,
                a.SKU,
                a.DescripcionArticulo,
                a.DescripcionArticuloAbreviada,
                a.NombreMarca,
                a.EstadoEcommerce,
                a.SlugSeo,
                Imagen = a.Imagenes
                    .Where(i => i.Activo)
                    .OrderByDescending(i => i.EsPrincipal)
                    .ThenBy(i => i.Orden)
                    .Select(i => i.Url)
                    .FirstOrDefault(),
                EmbalajesCount = a.Embalajes.Count(e => e.Activo)
            })
            .ToListAsync(ct);

        var order = topIds.Select((id, i) => (id, i)).ToDictionary(x => x.id, x => x.i);
        rows = rows.OrderBy(r => order.GetValueOrDefault(r.ID, int.MaxValue)).ToList();

        var priceMap = await _pricing.GetProductPriceSummariesAsync(rows.Select(r => r.ID), canal, ct);
        var stockMap = await _inventory.GetStockBatchAsync(rows.Select(r => r.ID), null, ct);

        return rows.Select(a =>
        {
            priceMap.TryGetValue(a.ID, out var price);
            stockMap.TryGetValue(a.ID, out var stock);
            var libre = stock?.CantidadLibre ?? 0m;
            var ok = a.EstadoEcommerce != "agotado" && libre > 0;
            return new ProductListItemDto(
                a.ID, a.SKU, a.DescripcionArticulo, a.DescripcionArticuloAbreviada,
                a.NombreMarca, a.EstadoEcommerce, a.Imagen, price?.PrecioDesde,
                a.EmbalajesCount, canal, price?.ListaDescripcion, libre, ok, a.SlugSeo,
                price?.PrecioLista, price?.PorcentajeDescuento);
        }).ToList();
    }

    public async Task<IReadOnlyList<ProductListItemDto>> GetOfertasAsync(
        int take = 12, string canal = "B2C", CancellationToken ct = default)
    {
        var idEmpresa = RequireTenant();
        canal = await _pricing.ResolveEffectiveCanalAsync(canal, ct);
        take = Math.Clamp(take, 1, 48);
        var now = DateTime.UtcNow;

        var grupoIds = await _db.GruposArticuloEcommerce.AsNoTracking()
            .Where(g => g.IDEmpresa == idEmpresa
                        && g.Activo
                        && g.TipoGrupo == 3
                        && g.FechaInicioVigencia <= now
                        && g.FechaFinVigencia >= now)
            .OrderBy(g => g.Orden)
            .Select(g => g.ID)
            .ToListAsync(ct);

        if (grupoIds.Count == 0)
            return Array.Empty<ProductListItemDto>();

        var artIds = await _db.DetallesGrupoArticuloEcommerce.AsNoTracking()
            .Where(d => d.IDEmpresa == idEmpresa && d.Activo && grupoIds.Contains(d.IDGrupoArticuloEcommerce))
            .Select(d => d.IDArticulo)
            .Distinct()
            .Take(take * 3)
            .ToListAsync(ct);

        if (artIds.Count == 0)
            return Array.Empty<ProductListItemDto>();

        var rows = await _db.Articulos.AsNoTracking()
            .Where(a => a.IDEmpresa == idEmpresa && a.Activo && a.ActivoEcommerce && artIds.Contains(a.ID))
            .Select(a => new
            {
                a.ID,
                a.SKU,
                a.DescripcionArticulo,
                a.DescripcionArticuloAbreviada,
                a.NombreMarca,
                a.EstadoEcommerce,
                a.SlugSeo,
                Imagen = a.Imagenes
                    .Where(i => i.Activo)
                    .OrderByDescending(i => i.EsPrincipal)
                    .ThenBy(i => i.Orden)
                    .Select(i => i.Url)
                    .FirstOrDefault(),
                EmbalajesCount = a.Embalajes.Count(e => e.Activo)
            })
            .ToListAsync(ct);

        var order = artIds.Select((id, i) => (id, i)).ToDictionary(x => x.id, x => x.i);
        rows = rows.OrderBy(r => order.GetValueOrDefault(r.ID, int.MaxValue)).ToList();

        var priceMap = await _pricing.GetProductPriceSummariesAsync(rows.Select(r => r.ID), canal, ct);
        var stockMap = await _inventory.GetStockBatchAsync(rows.Select(r => r.ID), null, ct);

        return rows
            .Select(a =>
            {
                priceMap.TryGetValue(a.ID, out var price);
                stockMap.TryGetValue(a.ID, out var stock);
                var libre = stock?.CantidadLibre ?? 0m;
                var ok = a.EstadoEcommerce != "agotado" && libre > 0;
                return new ProductListItemDto(
                    a.ID, a.SKU, a.DescripcionArticulo, a.DescripcionArticuloAbreviada,
                    a.NombreMarca, a.EstadoEcommerce, a.Imagen, price?.PrecioDesde,
                    a.EmbalajesCount, canal, price?.ListaDescripcion, libre, ok, a.SlugSeo,
                    price?.PrecioLista, price?.PorcentajeDescuento);
            })
            .Where(p => p.StockOk)
            .Take(take)
            .ToList();
    }

    private async Task<ProductDetailDto> MapDetailAsync(Articulo a, string canal, CancellationToken ct)
    {
        canal = await _pricing.ResolveEffectiveCanalAsync(canal, ct);
        var prices = await _pricing.GetEmbalajePricesAsync(a.ID, canal, ct);
        var byEmb = prices.ToDictionary(p => p.IDArticuloEmbalaje);
        var listaNombre = prices.FirstOrDefault()?.ListaDescripcion;
        var stock = await _inventory.GetStockAsync(a.ID, null, ct);
        var libre = stock?.CantidadLibre ?? 0m;
        var ok = a.EstadoEcommerce != "agotado" && libre > 0;

        var ratingAgg = await _db.ResenasArticuloEcommerce.AsNoTracking()
            .Where(r => r.IDEmpresa == a.IDEmpresa && r.IDArticulo == a.ID && r.Activo && r.Aprobado)
            .GroupBy(_ => 1)
            .Select(g => new { Avg = g.Average(x => x.Puntuacion), Cnt = g.Count() })
            .FirstOrDefaultAsync(ct);

        var uomCodes = a.Embalajes.Select(e => e.CodigoUnidadMedida).Distinct().ToList();
        var uoms = await _db.UnidadesMedidaEcommerce.AsNoTracking()
            .Where(u => u.IDEmpresa == a.IDEmpresa && u.Activo && uomCodes.Contains(u.Codigo))
            .ToDictionaryAsync(u => u.Codigo, ct);

        return new ProductDetailDto(
            a.ID,
            a.SKU,
            a.DescripcionArticulo,
            a.DescripcionArticuloAbreviada,
            a.DescripcionArticuloExtensa,
            a.CodReferenciaProveedor,
            a.NombreMarca,
            a.EstadoEcommerce,
            a.FichaTecnicaUrl,
            a.VideoUrl,
            canal,
            listaNombre,
            a.Embalajes
                .OrderBy(e => e.CantidadxEmbalaje)
                .Select(e =>
                {
                    byEmb.TryGetValue(e.ID, out var px);
                    uoms.TryGetValue(e.CodigoUnidadMedida, out var um);
                    return new ProductEmbalajeDto(
                        e.ID,
                        e.DescripcionEmbalaje,
                        e.CantidadxEmbalaje,
                        e.CodigoBarras,
                        px?.PrecioContado ?? e.PrecioDemo,
                        px?.PrecioCredito,
                        px?.CantidadMinimaMayorista ?? 0,
                        e.EmbalajeParaFacturacion,
                        e.CodigoUnidadMedida,
                        um?.Simbolo,
                        um?.Nombre,
                        e.PermiteFraccion || (um?.PermiteDecimales ?? false),
                        e.IncrementoVenta > 0 ? e.IncrementoVenta : (um?.IncrementoDefault ?? 1),
                        e.CantidadMinimaVenta > 0 ? e.CantidadMinimaVenta : 1,
                        px?.PrecioLista,
                        px?.PorcentajeDescuento);
                })
                .ToList(),
            a.Imagenes
                .OrderByDescending(i => i.EsPrincipal)
                .ThenBy(i => i.Orden)
                .Select(i => new ProductImagenDto(i.ID, i.Url, i.Titulo, i.Orden, i.EsPrincipal))
                .ToList(),
            libre,
            ok,
            stock?.DepositoNombre,
            ratingAgg is null ? 0 : Math.Round(ratingAgg.Avg, 2),
            ratingAgg?.Cnt ?? 0,
            a.SlugSeo);
    }

    private IQueryable<Articulo> BaseVisibleProducts(long idEmpresa) =>
        _db.Articulos
            .AsNoTracking()
            .Where(a => a.IDEmpresa == idEmpresa
                        && a.Activo
                        && a.ActivoEcommerce
                        && VisibleStates.Contains(a.EstadoEcommerce));

    private long RequireTenant()
    {
        if (!_tenant.IsResolved || !_tenant.IDEmpresa.HasValue)
            throw new InvalidOperationException("Tenant no resuelto. Envíe X-Tenant-ID o X-Tenant-Slug.");
        return _tenant.IDEmpresa.Value;
    }
}
