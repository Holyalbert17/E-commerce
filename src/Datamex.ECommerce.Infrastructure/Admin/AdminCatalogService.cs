using Datamex.ECommerce.Application.Abstractions;
using Datamex.ECommerce.Application.Admin;
using Datamex.ECommerce.Application.Catalog;
using Datamex.ECommerce.Application.Integration;
using Datamex.ECommerce.Application.Media;
using Datamex.ECommerce.Application.Pricing;
using Datamex.ECommerce.Domain.Entities;
using Datamex.ECommerce.Domain.Enums;
using Datamex.ECommerce.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Datamex.ECommerce.Infrastructure.Admin;

public sealed class AdminCatalogService : IAdminCatalogService
{
    private readonly EcomDbContext _db;
    private readonly ITenantContext _tenant;
    private readonly IPricingService _pricing;
    private readonly IAdminStockHelper _stock;
    private readonly IMediaStorage _media;

    public AdminCatalogService(
        EcomDbContext db,
        ITenantContext tenant,
        IPricingService pricing,
        IAdminStockHelper stock,
        IMediaStorage media)
    {
        _db = db;
        _tenant = tenant;
        _pricing = pricing;
        _stock = stock;
        _media = media;
    }

    public async Task<PagedResult<AdminProductDto>> SearchAsync(
        string? search, string? estado, int page, int pageSize,
        bool? activo = null, bool? activoEcommerce = null,
        string? calidad = null, CancellationToken ct = default)
    {
        var idEmpresa = RequireTenant();
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);
        var calidadKey = (calidad ?? "").Trim().ToLowerInvariant();

        var q = _db.Articulos.AsNoTracking().Where(a => a.IDEmpresa == idEmpresa);
        if (activo is bool aFlag)
            q = q.Where(a => a.Activo == aFlag);
        else
            q = q.Where(a => a.Activo);

        if (activoEcommerce is bool ecomFlag)
            q = q.Where(a => a.ActivoEcommerce == ecomFlag);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLowerInvariant();
            q = q.Where(a =>
                (a.SKU != null && a.SKU.ToLower().Contains(term))
                || a.DescripcionArticulo.ToLower().Contains(term)
                || a.DescripcionArticuloAbreviada.ToLower().Contains(term)
                || (a.CodReferenciaProveedor != null && a.CodReferenciaProveedor.ToLower().Contains(term)));
        }

        if (!string.IsNullOrWhiteSpace(estado))
            q = q.Where(a => a.EstadoEcommerce == estado);

        if (calidadKey is "sin_stock")
        {
            q = q.Where(a => !_db.Stocks.Any(s =>
                s.IDEmpresa == idEmpresa && s.IDArticulo == a.ID && s.Activo && s.TotalExistencia > 0));
        }

        // sin_precio: precios viven fuera de Articulo → filtrar por IDs candidatos.
        if (calidadKey is "sin_precio")
        {
            var candidateIds = await q.OrderBy(a => a.DescripcionArticulo)
                .Select(a => a.ID)
                .Take(3000)
                .ToListAsync(ct);
            var pricesAll = await _pricing.GetProductPriceSummariesAsync(candidateIds, "B2C", ct);
            var sinPrecioIds = candidateIds
                .Where(id => !pricesAll.TryGetValue(id, out var px) || px?.PrecioDesde is null || px.PrecioDesde <= 0)
                .ToList();
            var totalSin = sinPrecioIds.Count;
            var pageIds = sinPrecioIds.Skip((page - 1) * pageSize).Take(pageSize).ToList();
            if (pageIds.Count == 0)
                return new PagedResult<AdminProductDto>([], page, pageSize, totalSin);

            var rowsSin = await _db.Articulos.AsNoTracking()
                .Where(a => a.IDEmpresa == idEmpresa && pageIds.Contains(a.ID))
                .Select(a => new { a.ID, a.SKU, a.DescripcionArticulo, a.NombreMarca, a.Activo, a.ActivoEcommerce, a.EstadoEcommerce, a.UserName, a.SlugSeo })
                .ToListAsync(ct);
            var order = pageIds.Select((id, i) => (id, i)).ToDictionary(x => x.id, x => x.i);
            rowsSin = rowsSin.OrderBy(r => order[r.ID]).ToList();
            var stocksSin = await _stock.GetExistenciasAsync(pageIds, ct);
            var modoSin = await _db.EmpresaConfiguracionesEcommerce.AsNoTracking()
                .Where(c => c.ID == idEmpresa)
                .Select(c => c.ModoIntegracion)
                .FirstOrDefaultAsync(ct);
            var itemsSin = rowsSin.Select(a =>
            {
                pricesAll.TryGetValue(a.ID, out var px);
                stocksSin.TryGetValue(a.ID, out var st);
                var (origen, readOnly) = ResolveOwnership(a.UserName, modoSin);
                return new AdminProductDto(
                    a.ID, a.SKU, a.DescripcionArticulo, a.NombreMarca,
                    a.Activo, a.ActivoEcommerce, a.EstadoEcommerce,
                    px?.PrecioDesde, st.Existencia, st.IDDeposito,
                    origen, readOnly, a.SlugSeo);
            }).ToList();
            return new PagedResult<AdminProductDto>(itemsSin, page, pageSize, totalSin);
        }

        var total = await q.CountAsync(ct);
        var rows = await q.OrderBy(a => a.DescripcionArticulo)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(a => new { a.ID, a.SKU, a.DescripcionArticulo, a.NombreMarca, a.Activo, a.ActivoEcommerce, a.EstadoEcommerce, a.UserName, a.SlugSeo })
            .ToListAsync(ct);

        var prices = await _pricing.GetProductPriceSummariesAsync(rows.Select(r => r.ID), "B2C", ct);
        var stocks = await _stock.GetExistenciasAsync(rows.Select(r => r.ID), ct);
        var modo = await _db.EmpresaConfiguracionesEcommerce.AsNoTracking()
            .Where(c => c.ID == idEmpresa)
            .Select(c => c.ModoIntegracion)
            .FirstOrDefaultAsync(ct);

        var items = rows.Select(a =>
        {
            prices.TryGetValue(a.ID, out var px);
            stocks.TryGetValue(a.ID, out var st);
            var (origen, readOnly) = ResolveOwnership(a.UserName, modo);
            return new AdminProductDto(
                a.ID, a.SKU, a.DescripcionArticulo, a.NombreMarca,
                a.Activo, a.ActivoEcommerce, a.EstadoEcommerce,
                px?.PrecioDesde, st.Existencia, st.IDDeposito,
                origen, readOnly, a.SlugSeo);
        }).ToList();

        return new PagedResult<AdminProductDto>(items, page, pageSize, total);
    }

    public async Task<AdminProductDto?> GetAsync(long id, CancellationToken ct = default)
    {
        var idEmpresa = RequireTenant();
        var a = await _db.Articulos.AsNoTracking()
            .FirstOrDefaultAsync(x => x.ID == id && x.IDEmpresa == idEmpresa, ct);
        if (a is null) return null;

        var prices = await _pricing.GetProductPriceSummariesAsync([id], "B2C", ct);
        var stocks = await _stock.GetExistenciasAsync([id], ct);
        prices.TryGetValue(id, out var px);
        stocks.TryGetValue(id, out var st);
        var modo = await _db.EmpresaConfiguracionesEcommerce.AsNoTracking()
            .Where(c => c.ID == idEmpresa)
            .Select(c => c.ModoIntegracion)
            .FirstOrDefaultAsync(ct);
        var (origen, readOnly) = ResolveOwnership(a.UserName, modo);

        return new AdminProductDto(
            a.ID, a.SKU, a.DescripcionArticulo, a.NombreMarca,
            a.Activo, a.ActivoEcommerce, a.EstadoEcommerce,
            px?.PrecioDesde, st.Existencia, st.IDDeposito,
            origen, readOnly, a.SlugSeo);
    }

    public async Task<AdminArticuloDetailDto?> GetDetailAsync(long id, CancellationToken ct = default)
    {
        var idEmpresa = RequireTenant();
        var a = await _db.Articulos.AsNoTracking()
            .FirstOrDefaultAsync(x => x.ID == id && x.IDEmpresa == idEmpresa, ct);
        if (a is null) return null;

        var emb = await _db.ArticuloEmbalajes.AsNoTracking()
            .Where(e => e.IDEmpresa == idEmpresa && e.IDArticulo == id && e.Activo)
            .OrderByDescending(e => e.EmbalajeParaFacturacion)
            .ThenBy(e => e.ID)
            .FirstOrDefaultAsync(ct);

        string? barcode = emb?.CodigoBarras;
        if (emb is not null && string.IsNullOrWhiteSpace(barcode))
        {
            barcode = await _db.ArticuloEmbalajeCodigosBarra.AsNoTracking()
                .Where(c => c.IDEmpresa == idEmpresa && c.IDArticuloEmbalaje == emb.ID && c.Activo)
                .OrderBy(c => c.ID)
                .Select(c => c.CodigoBarra)
                .FirstOrDefaultAsync(ct);
        }

        var img = await _db.ArticuloImagenes.AsNoTracking()
            .Where(i => i.IDEmpresa == idEmpresa && i.IDArticulo == id && i.Activo)
            .OrderByDescending(i => i.EsPrincipal)
            .ThenBy(i => i.Orden)
            .Select(i => i.Url)
            .FirstOrDefaultAsync(ct);

        var prices = await _pricing.GetProductPriceSummariesAsync([id], "B2C", ct);
        var stocks = await _stock.GetExistenciasAsync([id], ct);
        prices.TryGetValue(id, out var px);
        stocks.TryGetValue(id, out var st);
        var modo = await _db.EmpresaConfiguracionesEcommerce.AsNoTracking()
            .Where(c => c.ID == idEmpresa)
            .Select(c => c.ModoIntegracion)
            .FirstOrDefaultAsync(ct);
        var (origen, readOnly) = ResolveOwnership(a.UserName, modo);

        return new AdminArticuloDetailDto(
            a.ID, a.NroArticulo, a.SKU, a.DescripcionArticulo,
            a.DescripcionArticuloAbreviada, a.DescripcionArticuloExtensa,
            a.CodReferenciaProveedor,
            a.IDMarca, a.NombreMarca, a.IDLinea, a.NombreLinea,
            a.IDTipoArticulo, a.NombreTipoArticulo, a.IDProveedor, a.NombreProveedor,
            a.IDPais, a.NombrePais, a.IDRegimenImpositivo, a.NombreRegimenImpositivo,
            a.Activo, a.ActivoEcommerce, a.EstadoEcommerce,
            barcode, emb?.IDEmbalaje, emb?.DescripcionEmbalaje,
            emb?.CodigoUnidadMedida ?? "UN", emb?.CantidadxEmbalaje ?? 1,
            emb?.ID, img, st.Existencia, px?.PrecioDesde,
            a.FichaTecnicaUrl, a.VideoUrl, a.SlugSeo, origen, readOnly);
    }

    public async Task<AdminArticuloDetailDto> CreateAsync(
        CreateAdminArticuloRequest request, CancellationToken ct = default)
    {
        var idEmpresa = RequireTenant();
        await EnsureStandaloneCatalogAltaAsync(idEmpresa, ct);
        ValidateMasterRequest(
            request.Sku, request.Descripcion, request.CantidadPorEmbalaje, request.CodigoUnidadMedida);

        var marca = await RequireParamAsync(idEmpresa, CatalogoParametroTipos.Marca, request.IdMarca, ct);
        var linea = await RequireParamAsync(idEmpresa, CatalogoParametroTipos.Linea, request.IdLinea, ct);
        var tipo = await RequireParamAsync(idEmpresa, CatalogoParametroTipos.Tipo, request.IdTipoArticulo, ct);
        var proveedor = await RequireParamAsync(idEmpresa, CatalogoParametroTipos.Proveedor, request.IdProveedor, ct);
        var embalajeParam = await RequireParamAsync(idEmpresa, CatalogoParametroTipos.Embalaje, request.IdEmbalaje, ct);
        var pais = await OptionalParamAsync(idEmpresa, CatalogoParametroTipos.Pais, request.IdPais, ct);
        var regimen = await OptionalParamAsync(idEmpresa, CatalogoParametroTipos.Regimen, request.IdRegimenImpositivo, ct);

        var sku = request.Sku.Trim();
        if (await _db.Articulos.AnyAsync(a => a.IDEmpresa == idEmpresa && a.SKU == sku && a.Activo, ct))
            throw new InvalidOperationException($"Ya existe un artículo con SKU '{sku}'.");

        var nro = request.NroArticulo
                  ?? ((await _db.Articulos.AsNoTracking()
                      .Where(a => a.IDEmpresa == idEmpresa)
                      .MaxAsync(a => (int?)a.NroArticulo, ct)) ?? 0) + 1;

        var now = DateTime.UtcNow;
        var desc = request.Descripcion.Trim();
        var art = new Articulo
        {
            IDEmpresa = idEmpresa,
            NroArticulo = nro,
            SKU = sku,
            DescripcionArticulo = desc,
            DescripcionArticuloAbreviada = desc.Length <= 50 ? desc : desc[..50],
            DescripcionArticuloExtensa = string.IsNullOrWhiteSpace(request.DescripcionExtensa)
                ? desc
                : request.DescripcionExtensa.Trim(),
            CodReferenciaProveedor = string.IsNullOrWhiteSpace(request.CodReferenciaProveedor)
                ? null
                : request.CodReferenciaProveedor.Trim(),
            IDMarca = marca.ID,
            NombreMarca = marca.Descripcion,
            IDLinea = linea.ID,
            NombreLinea = linea.Descripcion,
            IDTipoArticulo = tipo.ID,
            NombreTipoArticulo = tipo.Descripcion,
            IDProveedor = proveedor.ID,
            NombreProveedor = proveedor.Descripcion,
            IDPais = pais?.ID,
            NombrePais = pais?.Descripcion,
            IDRegimenImpositivo = regimen?.ID,
            NombreRegimenImpositivo = regimen?.Descripcion,
            Activo = true,
            // ERP: alta no publica en vitrina — Activar/Inactivar
            ActivoEcommerce = false,
            EstadoEcommerce = "borrador",
            FichaTecnicaUrl = NullIfEmpty(request.FichaTecnicaUrl),
            VideoUrl = NullIfEmpty(request.VideoUrl),
            UltModificacion = now,
            UserName = "admin"
        };
        art.SlugSeo = await EnsureUniqueSlugAsync(
            idEmpresa,
            string.IsNullOrWhiteSpace(request.SlugSeo) ? Slugify(desc) : request.SlugSeo!,
            null,
            ct);
        _db.Articulos.Add(art);
        await _db.SaveChangesAsync(ct);

        var uom = request.CodigoUnidadMedida.Trim().ToUpperInvariant();
        var emb = new ArticuloEmbalaje
        {
            IDEmpresa = idEmpresa,
            IDArticulo = art.ID,
            IDEmbalaje = embalajeParam.ID,
            DescripcionEmbalaje = embalajeParam.Descripcion,
            CantidadxEmbalaje = request.CantidadPorEmbalaje,
            CodigoUnidadMedida = uom,
            EmbalajeParaFacturacion = true,
            EmbalajeParaControlStock = true,
            CodigoBarras = NullIfEmpty(request.CodigoBarra),
            Activo = true,
            UltModificacion = now,
            UserName = "admin"
        };
        _db.ArticuloEmbalajes.Add(emb);
        await _db.SaveChangesAsync(ct);

        if (!string.IsNullOrWhiteSpace(request.CodigoBarra))
        {
            _db.ArticuloEmbalajeCodigosBarra.Add(new ArticuloEmbalajeCodigoBarra
            {
                IDEmpresa = idEmpresa,
                IDArticuloEmbalaje = emb.ID,
                CodigoBarra = request.CodigoBarra.Trim(),
                Activo = true,
                UltModificacion = now,
                UserName = "admin"
            });
            await _db.SaveChangesAsync(ct);
        }

        if (!string.IsNullOrWhiteSpace(request.ImagenUrl))
        {
            await CreateImageAsync(art.ID, new UpsertProductImageRequest(
                request.ImagenUrl.Trim(), "Principal", 1, true, true), ct);
        }

        if (request.StockInicial is > 0)
        {
            await EnsureLocalStockEditableAsync(idEmpresa, ct);
            await _stock.SetExistenciaAsync(art.ID, (double)request.StockInicial.Value, ct);
        }

        return (await GetDetailAsync(art.ID, ct))!;
    }

    public async Task<AdminArticuloDetailDto> UpsertMasterAsync(
        long id, UpsertAdminArticuloRequest request, CancellationToken ct = default)
    {
        var idEmpresa = RequireTenant();

        var art = await _db.Articulos.FirstOrDefaultAsync(a => a.ID == id && a.IDEmpresa == idEmpresa, ct)
                  ?? throw new InvalidOperationException("Artículo no encontrado.");

        var cfgMode = await _db.EmpresaConfiguracionesEcommerce.AsNoTracking()
            .FirstOrDefaultAsync(c => c.ID == idEmpresa, ct);
        var integrated = TenantIntegrationMode.Resolve(cfgMode, IntegrationMode.Standalone) == IntegrationMode.Integrated;
        if (integrated)
            return await UpsertShopFieldsOnlyAsync(idEmpresa, art, request, ct);

        ValidateMasterRequest(
            request.Sku, request.Descripcion, request.CantidadPorEmbalaje, request.CodigoUnidadMedida);

        var marca = await RequireParamAsync(idEmpresa, CatalogoParametroTipos.Marca, request.IdMarca, ct);
        var linea = await RequireParamAsync(idEmpresa, CatalogoParametroTipos.Linea, request.IdLinea, ct);
        var tipo = await RequireParamAsync(idEmpresa, CatalogoParametroTipos.Tipo, request.IdTipoArticulo, ct);
        var proveedor = await RequireParamAsync(idEmpresa, CatalogoParametroTipos.Proveedor, request.IdProveedor, ct);
        var embalajeParam = await RequireParamAsync(idEmpresa, CatalogoParametroTipos.Embalaje, request.IdEmbalaje, ct);
        var pais = await OptionalParamAsync(idEmpresa, CatalogoParametroTipos.Pais, request.IdPais, ct);
        var regimen = await OptionalParamAsync(idEmpresa, CatalogoParametroTipos.Regimen, request.IdRegimenImpositivo, ct);

        var sku = request.Sku.Trim();
        if (await _db.Articulos.AnyAsync(a =>
                a.IDEmpresa == idEmpresa && a.SKU == sku && a.Activo && a.ID != id, ct))
            throw new InvalidOperationException($"Ya existe un artículo con SKU '{sku}'.");

        var now = DateTime.UtcNow;
        var desc = request.Descripcion.Trim();
        art.SKU = sku;
        if (request.NroArticulo is int nro) art.NroArticulo = nro;
        art.DescripcionArticulo = desc;
        art.DescripcionArticuloAbreviada = string.IsNullOrWhiteSpace(request.DescripcionAbreviada)
            ? (desc.Length <= 50 ? desc : desc[..50])
            : request.DescripcionAbreviada.Trim();
        art.DescripcionArticuloExtensa = string.IsNullOrWhiteSpace(request.DescripcionExtensa)
            ? desc
            : request.DescripcionExtensa.Trim();
        art.CodReferenciaProveedor = NullIfEmpty(request.CodReferenciaProveedor);
        art.IDMarca = marca.ID;
        art.NombreMarca = marca.Descripcion;
        art.IDLinea = linea.ID;
        art.NombreLinea = linea.Descripcion;
        art.IDTipoArticulo = tipo.ID;
        art.NombreTipoArticulo = tipo.Descripcion;
        art.IDProveedor = proveedor.ID;
        art.NombreProveedor = proveedor.Descripcion;
        art.IDPais = pais?.ID;
        art.NombrePais = pais?.Descripcion;
        art.IDRegimenImpositivo = regimen?.ID;
        art.NombreRegimenImpositivo = regimen?.Descripcion;
        art.FichaTecnicaUrl = NullIfEmpty(request.FichaTecnicaUrl);
        art.VideoUrl = NullIfEmpty(request.VideoUrl);
        if (request.Activo.HasValue) art.Activo = request.Activo.Value;
        if (request.ActivoEcommerce.HasValue) art.ActivoEcommerce = request.ActivoEcommerce.Value;
        if (!string.IsNullOrWhiteSpace(request.EstadoEcommerce))
            art.EstadoEcommerce = request.EstadoEcommerce.Trim().ToLowerInvariant();
        art.SlugSeo = await EnsureUniqueSlugAsync(
            idEmpresa,
            string.IsNullOrWhiteSpace(request.SlugSeo) ? (art.SlugSeo ?? Slugify(desc)) : request.SlugSeo!,
            id,
            ct);
        art.UltModificacion = now;
        // Conservar origen ERP para badge ownership Integrated
        if (!string.Equals(art.UserName, "erp-sync", StringComparison.OrdinalIgnoreCase))
            art.UserName = "admin";

        var emb = await _db.ArticuloEmbalajes
            .Include(e => e.CodigosBarra)
            .Where(e => e.IDEmpresa == idEmpresa && e.IDArticulo == id && e.Activo)
            .OrderByDescending(e => e.EmbalajeParaFacturacion)
            .ThenBy(e => e.ID)
            .FirstOrDefaultAsync(ct);

        if (emb is null)
        {
            emb = new ArticuloEmbalaje
            {
                IDEmpresa = idEmpresa,
                IDArticulo = id,
                Activo = true,
                EmbalajeParaFacturacion = true,
                EmbalajeParaControlStock = true,
                UserName = "admin"
            };
            _db.ArticuloEmbalajes.Add(emb);
        }

        emb.IDEmbalaje = embalajeParam.ID;
        emb.DescripcionEmbalaje = embalajeParam.Descripcion;
        emb.CantidadxEmbalaje = request.CantidadPorEmbalaje;
        emb.CodigoUnidadMedida = request.CodigoUnidadMedida.Trim().ToUpperInvariant();
        emb.CodigoBarras = NullIfEmpty(request.CodigoBarra);
        emb.UltModificacion = now;
        emb.UserName = "admin";
        await _db.SaveChangesAsync(ct);

        var existingBar = emb.CodigosBarra?.FirstOrDefault(c => c.Activo)
                          ?? await _db.ArticuloEmbalajeCodigosBarra
                              .FirstOrDefaultAsync(c =>
                                  c.IDEmpresa == idEmpresa && c.IDArticuloEmbalaje == emb.ID && c.Activo, ct);

        if (!string.IsNullOrWhiteSpace(request.CodigoBarra))
        {
            var code = request.CodigoBarra.Trim();
            if (existingBar is null)
            {
                _db.ArticuloEmbalajeCodigosBarra.Add(new ArticuloEmbalajeCodigoBarra
                {
                    IDEmpresa = idEmpresa,
                    IDArticuloEmbalaje = emb.ID,
                    CodigoBarra = code,
                    Activo = true,
                    UltModificacion = now,
                    UserName = "admin"
                });
            }
            else if (existingBar.CodigoBarra != code)
            {
                existingBar.CodigoBarra = code;
                existingBar.UltModificacion = now;
            }
        }
        else if (existingBar is not null)
        {
            existingBar.Activo = false;
            existingBar.UltModificacion = now;
        }

        if (!string.IsNullOrWhiteSpace(request.ImagenUrl))
        {
            var principal = await _db.ArticuloImagenes
                .Where(i => i.IDEmpresa == idEmpresa && i.IDArticulo == id && i.Activo)
                .OrderByDescending(i => i.EsPrincipal)
                .ThenBy(i => i.Orden)
                .FirstOrDefaultAsync(ct);
            if (principal is null)
            {
                await CreateImageAsync(id, new UpsertProductImageRequest(
                    request.ImagenUrl.Trim(), "Principal", 1, true, true), ct);
            }
            else if (!string.Equals(principal.Url, request.ImagenUrl.Trim(), StringComparison.OrdinalIgnoreCase))
            {
                principal.Url = request.ImagenUrl.Trim();
                principal.UltModificacion = now;
            }
        }

        await _db.SaveChangesAsync(ct);
        return (await GetDetailAsync(id, ct))!;
    }

    /// <summary>
    /// Integrated: no se pisan SKU, marca, línea, embalaje (los pisa el ERP en el próximo sync).
    /// Solo vitrina, SEO, texto de tienda e imagen.
    /// </summary>
    private async Task<AdminArticuloDetailDto> UpsertShopFieldsOnlyAsync(
        long idEmpresa, Articulo art, UpsertAdminArticuloRequest request, CancellationToken ct)
    {
        var now = DateTime.UtcNow;
        if (!string.IsNullOrWhiteSpace(request.DescripcionExtensa))
            art.DescripcionArticuloExtensa = request.DescripcionExtensa.Trim();
        art.FichaTecnicaUrl = NullIfEmpty(request.FichaTecnicaUrl);
        art.VideoUrl = NullIfEmpty(request.VideoUrl);
        if (request.Activo.HasValue) art.Activo = request.Activo.Value;
        if (request.ActivoEcommerce.HasValue) art.ActivoEcommerce = request.ActivoEcommerce.Value;
        if (!string.IsNullOrWhiteSpace(request.EstadoEcommerce))
            art.EstadoEcommerce = request.EstadoEcommerce.Trim().ToLowerInvariant();
        if (!string.IsNullOrWhiteSpace(request.SlugSeo) || string.IsNullOrWhiteSpace(art.SlugSeo))
        {
            art.SlugSeo = await EnsureUniqueSlugAsync(
                idEmpresa,
                string.IsNullOrWhiteSpace(request.SlugSeo)
                    ? (art.SlugSeo ?? Slugify(art.DescripcionArticulo))
                    : request.SlugSeo!,
                art.ID,
                ct);
        }
        art.UltModificacion = now;

        if (!string.IsNullOrWhiteSpace(request.ImagenUrl))
        {
            var principal = await _db.ArticuloImagenes
                .Where(i => i.IDEmpresa == idEmpresa && i.IDArticulo == art.ID && i.Activo)
                .OrderByDescending(i => i.EsPrincipal)
                .ThenBy(i => i.Orden)
                .FirstOrDefaultAsync(ct);
            if (principal is null)
            {
                await CreateImageAsync(art.ID, new UpsertProductImageRequest(
                    request.ImagenUrl.Trim(), "Principal", 1, true, true), ct);
            }
            else if (!string.Equals(principal.Url, request.ImagenUrl.Trim(), StringComparison.OrdinalIgnoreCase))
            {
                principal.Url = request.ImagenUrl.Trim();
                principal.UltModificacion = now;
            }
        }

        await _db.SaveChangesAsync(ct);
        return (await GetDetailAsync(art.ID, ct))!;
    }

    public async Task DeleteAsync(long id, CancellationToken ct = default)
    {
        var idEmpresa = RequireTenant();
        await EnsureStandaloneCatalogAltaAsync(idEmpresa, ct);
        var art = await _db.Articulos.FirstOrDefaultAsync(a => a.ID == id && a.IDEmpresa == idEmpresa, ct)
                  ?? throw new InvalidOperationException("Artículo no encontrado.");

        art.Activo = false;
        art.ActivoEcommerce = false;
        if (art.EstadoEcommerce == "activo")
            art.EstadoEcommerce = "descontinuado";
        art.UltModificacion = DateTime.UtcNow;
        art.UserName = "admin";
        await _db.SaveChangesAsync(ct);
    }

    private async Task<CatalogoParametro> RequireParamAsync(
        long idEmpresa, string tipo, long id, CancellationToken ct)
    {
        var p = await _db.CatalogoParametros.AsNoTracking()
            .FirstOrDefaultAsync(x => x.ID == id && x.IDEmpresa == idEmpresa && x.Tipo == tipo && x.Activo, ct);
        if (p is null)
            throw new InvalidOperationException($"Parámetro {tipo} #{id} no encontrado o inactivo.");
        return p;
    }

    private async Task<CatalogoParametro?> OptionalParamAsync(
        long idEmpresa, string tipo, long? id, CancellationToken ct)
    {
        if (id is null or <= 0) return null;
        return await RequireParamAsync(idEmpresa, tipo, id.Value, ct);
    }

    private static void ValidateMasterRequest(
        string? sku, string? desc, double cantidad, string? uom)
    {
        if (string.IsNullOrWhiteSpace(sku))
            throw new ArgumentException("SKU requerido.");
        if (sku.Trim().Length > 100)
            throw new ArgumentException("SKU máx. 100 caracteres.");
        if (string.IsNullOrWhiteSpace(desc))
            throw new ArgumentException("Descripción requerida.");
        if (desc.Trim().Length > 100)
            throw new ArgumentException("Descripción máx. 100 caracteres.");
        if (cantidad < 1)
            throw new ArgumentException("Cantidad por embalaje debe ser ≥ 1.");
        if (string.IsNullOrWhiteSpace(uom))
            throw new ArgumentException("Unidad de medida requerida.");
    }

    private static string? NullIfEmpty(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    public async Task<AdminProductDto> UpdateAsync(long id, UpdateAdminProductRequest request, CancellationToken ct = default)
    {
        var idEmpresa = RequireTenant();
        var a = await _db.Articulos.FirstOrDefaultAsync(x => x.ID == id && x.IDEmpresa == idEmpresa, ct)
                ?? throw new InvalidOperationException("Artículo no encontrado.");

        if (request.ActivoEcommerce.HasValue)
            a.ActivoEcommerce = request.ActivoEcommerce.Value;
        if (!string.IsNullOrWhiteSpace(request.EstadoEcommerce))
            a.EstadoEcommerce = request.EstadoEcommerce.Trim().ToLowerInvariant();
        if (!string.IsNullOrWhiteSpace(request.Nombre))
            a.DescripcionArticulo = request.Nombre.Trim();
        if (!string.IsNullOrWhiteSpace(request.NombreCorto))
            a.DescripcionArticuloAbreviada = request.NombreCorto.Trim();

        a.UltModificacion = DateTime.UtcNow;
        if (!string.Equals(a.UserName, "erp-sync", StringComparison.OrdinalIgnoreCase))
            a.UserName = "admin";
        await _db.SaveChangesAsync(ct);

        return (await GetAsync(id, ct))!;
    }

    public async Task<AdminProductDto> UpdateStockAsync(long id, UpdateStockRequest request, CancellationToken ct = default)
    {
        var idEmpresa = RequireTenant();
        _ = await _db.Articulos.AsNoTracking()
                .FirstOrDefaultAsync(x => x.ID == id && x.IDEmpresa == idEmpresa, ct)
            ?? throw new InvalidOperationException("Artículo no encontrado.");

        await EnsureLocalStockEditableAsync(idEmpresa, ct);
        await _stock.SetExistenciaAsync(id, (double)request.TotalExistencia, ct);
        return (await GetAsync(id, ct))!;
    }

    public async Task<BulkActivoEcommerceResult> BulkSetActivoEcommerceAsync(
        BulkActivoEcommerceRequest request, CancellationToken ct = default)
    {
        var idEmpresa = RequireTenant();
        if (request.Ids is null || request.Ids.Count == 0)
            throw new ArgumentException("Ids vacío.");

        var ids = request.Ids.Distinct().ToList();
        var rows = await _db.Articulos
            .Where(a => a.IDEmpresa == idEmpresa && ids.Contains(a.ID))
            .ToListAsync(ct);

        var now = DateTime.UtcNow;
        var estado = string.IsNullOrWhiteSpace(request.EstadoEcommerce)
            ? null
            : request.EstadoEcommerce.Trim().ToLowerInvariant();

        foreach (var a in rows)
        {
            a.ActivoEcommerce = request.ActivoEcommerce;
            if (estado is not null)
                a.EstadoEcommerce = estado;
            else if (!request.ActivoEcommerce && a.EstadoEcommerce == "activo")
                a.EstadoEcommerce = "oculto";
            else if (request.ActivoEcommerce && a.EstadoEcommerce is "oculto" or "borrador")
                a.EstadoEcommerce = "activo";

            a.UltModificacion = now;
            a.UserName = "admin";
        }

        await _db.SaveChangesAsync(ct);
        return new BulkActivoEcommerceResult(rows.Count);
    }

    public async Task<IReadOnlyList<AdminProductImageDto>> ListImagesAsync(long articuloId, CancellationToken ct = default)
    {
        var idEmpresa = RequireTenant();
        await EnsureArticuloAsync(idEmpresa, articuloId, ct);

        return await _db.ArticuloImagenes.AsNoTracking()
            .Where(i => i.IDEmpresa == idEmpresa && i.IDArticulo == articuloId)
            .OrderBy(i => i.Orden)
            .ThenBy(i => i.ID)
            .Select(i => new AdminProductImageDto(i.ID, i.IDArticulo, i.Url, i.Titulo, i.Orden, i.EsPrincipal, i.Activo))
            .ToListAsync(ct);
    }

    public async Task<AdminProductImageDto> CreateImageAsync(
        long articuloId, UpsertProductImageRequest request, CancellationToken ct = default)
    {
        var idEmpresa = RequireTenant();
        await EnsureArticuloAsync(idEmpresa, articuloId, ct);
        ValidateImage(request);

        var now = DateTime.UtcNow;
        var orden = request.Orden > 0
            ? request.Orden
            : ((await _db.ArticuloImagenes.AsNoTracking()
                .Where(i => i.IDEmpresa == idEmpresa && i.IDArticulo == articuloId)
                .MaxAsync(i => (int?)i.Orden, ct)) ?? 0) + 1;

        if (request.EsPrincipal)
            await ClearPrincipalAsync(idEmpresa, articuloId, excludeId: null, ct);

        var img = new ArticuloImagen
        {
            IDEmpresa = idEmpresa,
            IDArticulo = articuloId,
            Activo = request.Activo,
            UltModificacion = now,
            UserName = "admin",
            Url = request.Url.Trim(),
            Titulo = string.IsNullOrWhiteSpace(request.Titulo) ? null : request.Titulo.Trim(),
            Orden = orden,
            EsPrincipal = request.EsPrincipal
        };
        _db.ArticuloImagenes.Add(img);
        await _db.SaveChangesAsync(ct);

        // Si es la primera imagen, marcarla principal.
        if (!img.EsPrincipal)
        {
            var anyPrincipal = await _db.ArticuloImagenes.AnyAsync(i =>
                i.IDEmpresa == idEmpresa && i.IDArticulo == articuloId && i.EsPrincipal && i.Activo, ct);
            if (!anyPrincipal)
            {
                img.EsPrincipal = true;
                await _db.SaveChangesAsync(ct);
            }
        }

        return MapImage(img);
    }

    public async Task<AdminProductImageDto> UploadImageAsync(
        long articuloId,
        Stream content,
        string contentType,
        string fileName,
        string? titulo,
        bool esPrincipal,
        CancellationToken ct = default)
    {
        var idEmpresa = RequireTenant();
        await EnsureArticuloAsync(idEmpresa, articuloId, ct);

        var stored = await _media.SaveProductImageAsync(
            idEmpresa, articuloId, content, contentType, fileName, ct);

        return await CreateImageAsync(articuloId, new UpsertProductImageRequest(
            stored.PublicUrl,
            string.IsNullOrWhiteSpace(titulo) ? Path.GetFileNameWithoutExtension(fileName) : titulo,
            Orden: 0,
            EsPrincipal: esPrincipal,
            Activo: true), ct);
    }

    public async Task<AdminProductImageDto> UpdateImageAsync(
        long articuloId, long imageId, UpsertProductImageRequest request, CancellationToken ct = default)
    {
        var idEmpresa = RequireTenant();
        ValidateImage(request);
        var img = await _db.ArticuloImagenes.FirstOrDefaultAsync(i =>
                i.ID == imageId && i.IDArticulo == articuloId && i.IDEmpresa == idEmpresa, ct)
            ?? throw new InvalidOperationException("Imagen no encontrada.");

        if (request.EsPrincipal)
            await ClearPrincipalAsync(idEmpresa, articuloId, excludeId: imageId, ct);

        img.Url = request.Url.Trim();
        img.Titulo = string.IsNullOrWhiteSpace(request.Titulo) ? null : request.Titulo.Trim();
        img.Orden = request.Orden > 0 ? request.Orden : img.Orden;
        img.EsPrincipal = request.EsPrincipal;
        img.Activo = request.Activo;
        img.UltModificacion = DateTime.UtcNow;
        img.UserName = "admin";
        await _db.SaveChangesAsync(ct);
        return MapImage(img);
    }

    public async Task DeleteImageAsync(long articuloId, long imageId, CancellationToken ct = default)
    {
        var idEmpresa = RequireTenant();
        var img = await _db.ArticuloImagenes.FirstOrDefaultAsync(i =>
                i.ID == imageId && i.IDArticulo == articuloId && i.IDEmpresa == idEmpresa, ct)
            ?? throw new InvalidOperationException("Imagen no encontrada.");

        var wasPrincipal = img.EsPrincipal;
        var url = img.Url;
        _db.ArticuloImagenes.Remove(img);
        await _db.SaveChangesAsync(ct);
        await _media.DeleteIfOwnedAsync(url, ct);

        if (wasPrincipal)
        {
            var next = await _db.ArticuloImagenes
                .Where(i => i.IDEmpresa == idEmpresa && i.IDArticulo == articuloId && i.Activo)
                .OrderBy(i => i.Orden)
                .FirstOrDefaultAsync(ct);
            if (next is not null)
            {
                next.EsPrincipal = true;
                next.UltModificacion = DateTime.UtcNow;
                await _db.SaveChangesAsync(ct);
            }
        }
    }

    public async Task<AdminProductImageDto> SetPrincipalImageAsync(
        long articuloId, long imageId, CancellationToken ct = default)
    {
        var idEmpresa = RequireTenant();
        var img = await _db.ArticuloImagenes.FirstOrDefaultAsync(i =>
                i.ID == imageId && i.IDArticulo == articuloId && i.IDEmpresa == idEmpresa, ct)
            ?? throw new InvalidOperationException("Imagen no encontrada.");

        await ClearPrincipalAsync(idEmpresa, articuloId, excludeId: imageId, ct);
        img.EsPrincipal = true;
        img.Activo = true;
        img.UltModificacion = DateTime.UtcNow;
        img.UserName = "admin";
        await _db.SaveChangesAsync(ct);
        return MapImage(img);
    }

    public async Task ReorderImagesAsync(
        long articuloId, IReadOnlyList<long> orderedIds, CancellationToken ct = default)
    {
        var idEmpresa = RequireTenant();
        await EnsureArticuloAsync(idEmpresa, articuloId, ct);
        if (orderedIds.Count == 0) return;

        var rows = await _db.ArticuloImagenes
            .Where(i => i.IDEmpresa == idEmpresa && i.IDArticulo == articuloId)
            .ToListAsync(ct);

        var now = DateTime.UtcNow;
        for (var i = 0; i < orderedIds.Count; i++)
        {
            var row = rows.FirstOrDefault(r => r.ID == orderedIds[i]);
            if (row is null) continue;
            row.Orden = i + 1;
            row.UltModificacion = now;
            row.UserName = "admin";
        }

        await _db.SaveChangesAsync(ct);
    }

    private async Task EnsureArticuloAsync(long idEmpresa, long articuloId, CancellationToken ct)
    {
        var ok = await _db.Articulos.AnyAsync(a => a.ID == articuloId && a.IDEmpresa == idEmpresa, ct);
        if (!ok) throw new InvalidOperationException("Artículo no encontrado.");
    }

    private async Task ClearPrincipalAsync(long idEmpresa, long articuloId, long? excludeId, CancellationToken ct)
    {
        var q = _db.ArticuloImagenes.Where(i =>
            i.IDEmpresa == idEmpresa && i.IDArticulo == articuloId && i.EsPrincipal);
        if (excludeId is long eid)
            q = q.Where(i => i.ID != eid);

        var rows = await q.ToListAsync(ct);
        foreach (var r in rows)
            r.EsPrincipal = false;
    }

    private static void ValidateImage(UpsertProductImageRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Url))
            throw new ArgumentException("Url requerida.");
        if (request.Url.Trim().Length > 500)
            throw new ArgumentException("Url demasiado larga (máx. 500).");
    }

    private static AdminProductImageDto MapImage(ArticuloImagen i) =>
        new(i.ID, i.IDArticulo, i.Url, i.Titulo, i.Orden, i.EsPrincipal, i.Activo);

    private long RequireTenant()
    {
        if (!_tenant.IsResolved || !_tenant.IDEmpresa.HasValue)
            throw new InvalidOperationException("Tenant no resuelto.");
        return _tenant.IDEmpresa.Value;
    }

    private static (string Origen, bool ReadOnly) ResolveOwnership(string? userName, int modoIntegracion)
    {
        var origen = string.Equals(userName, "erp-sync", StringComparison.OrdinalIgnoreCase) ? "Erp" : "Local";
        // Integrated (0): el stock es espejo ERP — el admin nunca lo edita (ni local ni ERP).
        var readOnly = modoIntegracion == (int)IntegrationMode.Integrated;
        return (origen, readOnly);
    }

    /// <summary>
    /// Integrated: el maestro vive en el ERP (Stock → Artículos / Site ECommerce).
    /// ecom-etl lo replica; acá solo se publica en vitrina.
    /// </summary>
    private async Task EnsureStandaloneCatalogAltaAsync(long idEmpresa, CancellationToken ct)
    {
        var cfg = await _db.EmpresaConfiguracionesEcommerce.AsNoTracking()
            .FirstOrDefaultAsync(c => c.ID == idEmpresa, ct);
        var mode = TenantIntegrationMode.Resolve(cfg, IntegrationMode.Standalone);
        if (mode == IntegrationMode.Integrated)
        {
            throw new InvalidOperationException(
                "En modo Integrated el artículo se da de alta en el ERP (Stock → Artículos). " +
                "ecom-etl lo sincroniza y acá solo se publica en la vitrina (Activar / inactivar).");
        }
    }

    /// <summary>
    /// Stock solo se edita en Standalone (sin ERP). Integrated/External: solo sync ERP.
    /// </summary>
    private async Task EnsureLocalStockEditableAsync(long idEmpresa, CancellationToken ct)
    {
        var cfg = await _db.EmpresaConfiguracionesEcommerce.AsNoTracking()
            .FirstOrDefaultAsync(c => c.ID == idEmpresa, ct);
        var mode = TenantIntegrationMode.Resolve(cfg, IntegrationMode.Standalone);
        if (mode != IntegrationMode.Standalone)
        {
            throw new InvalidOperationException(
                "Stock solo editable en modo Standalone (sin ERP). " +
                "Con Integrated el stock lo actualiza Sync ERP (lectura); no se escribe en el ERP.");
        }
    }

    private static string Slugify(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return "producto";
        var s = text.Trim().ToLowerInvariant();
        var sb = new System.Text.StringBuilder(s.Length);
        var prevDash = false;
        foreach (var ch in s)
        {
            if ((ch >= 'a' && ch <= 'z') || (ch >= '0' && ch <= '9'))
            {
                sb.Append(ch);
                prevDash = false;
            }
            else if (ch is ' ' or '-' or '_' || ch > 127)
            {
                if (!prevDash && sb.Length > 0)
                {
                    sb.Append('-');
                    prevDash = true;
                }
            }
        }
        var slug = sb.ToString().Trim('-');
        return string.IsNullOrEmpty(slug) ? "producto" : slug[..Math.Min(slug.Length, 160)];
    }

    private async Task<string> EnsureUniqueSlugAsync(long idEmpresa, string raw, long? excludeId, CancellationToken ct)
    {
        var baseSlug = Slugify(raw);
        var candidate = baseSlug;
        var n = 2;
        while (await _db.Articulos.AnyAsync(a =>
                   a.IDEmpresa == idEmpresa
                   && a.SlugSeo == candidate
                   && (!excludeId.HasValue || a.ID != excludeId.Value), ct))
        {
            candidate = $"{baseSlug}-{n++}";
        }
        return candidate;
    }
}

public readonly record struct StockSnap(decimal Existencia, long? IDDeposito);

public interface IAdminStockHelper
{
    Task<IReadOnlyDictionary<long, StockSnap>> GetExistenciasAsync(IEnumerable<long> articuloIds, CancellationToken ct = default);
    Task SetExistenciaAsync(long articuloId, double totalExistencia, CancellationToken ct = default);
}

public sealed class AdminStockHelper : IAdminStockHelper
{
    private readonly EcomDbContext _db;
    private readonly ITenantContext _tenant;

    public AdminStockHelper(EcomDbContext db, ITenantContext tenant)
    {
        _db = db;
        _tenant = tenant;
    }

    public async Task<IReadOnlyDictionary<long, StockSnap>> GetExistenciasAsync(
        IEnumerable<long> articuloIds, CancellationToken ct = default)
    {
        var idEmpresa = _tenant.IDEmpresa!.Value;
        var ids = articuloIds.Distinct().ToList();
        var config = await _db.EmpresaConfiguracionesEcommerce.AsNoTracking()
            .FirstOrDefaultAsync(c => c.ID == idEmpresa, ct);
        var depId = config?.IDDeposito;

        if (ids.Count == 0)
            return new Dictionary<long, StockSnap>();

        var q = _db.Stocks.AsNoTracking()
            .Where(s => s.IDEmpresa == idEmpresa && s.Activo && ids.Contains(s.IDArticulo));
        if (depId is > 0)
            q = q.Where(s => s.IDDeposito == depId);

        var rows = await q
            .GroupBy(s => s.IDArticulo)
            .Select(g => new { IDArticulo = g.Key, Existencia = g.Sum(x => x.TotalExistencia) })
            .ToListAsync(ct);

        var map = rows.ToDictionary(r => r.IDArticulo, r => new StockSnap((decimal)r.Existencia, depId));
        foreach (var id in ids)
        {
            if (!map.ContainsKey(id))
                map[id] = new StockSnap(0, depId);
        }

        return map;
    }

    public async Task SetExistenciaAsync(long articuloId, double totalExistencia, CancellationToken ct = default)
    {
        var idEmpresa = _tenant.IDEmpresa!.Value;
        var config = await _db.EmpresaConfiguracionesEcommerce
            .FirstAsync(c => c.ID == idEmpresa, ct);
        var depId = config.IDDeposito;
        if (depId <= 0)
        {
            depId = await _db.Depositos
                .Where(d => d.IDEmpresa == idEmpresa && d.Activo && d.EsDepositoEcommerce)
                .Select(d => d.ID)
                .FirstOrDefaultAsync(ct);
            if (depId <= 0)
                throw new InvalidOperationException("No hay depósito ecom configurado.");
            config.IDDeposito = depId;
        }

        var row = await _db.Stocks.FirstOrDefaultAsync(s =>
            s.IDEmpresa == idEmpresa
            && s.IDArticulo == articuloId
            && s.IDDeposito == depId
            && s.IDContenedor == null
            && s.IDLoteArticulo == null, ct);

        if (row is null)
        {
            row = new Stock
            {
                IDEmpresa = idEmpresa,
                IDArticulo = articuloId,
                IDDeposito = depId,
                Activo = true,
                UserName = "admin",
                UltModificacion = DateTime.UtcNow,
                TotalExistencia = Math.Max(0, totalExistencia)
            };
            _db.Stocks.Add(row);
            _db.InventarioMovimientos.Add(new InventarioMovimiento
            {
                IDEmpresa = idEmpresa,
                IDArticulo = articuloId,
                IDDeposito = depId,
                TipoMovimiento = "ajuste_manual",
                Cantidad = (decimal)row.TotalExistencia,
                ExistenciaAntes = 0,
                ExistenciaDespues = (decimal)row.TotalExistencia,
                Referencia = "AdminStockSet",
                Fecha = DateTime.UtcNow,
                Activo = true,
                UltModificacion = DateTime.UtcNow,
                UserName = "admin"
            });
        }
        else
        {
            var antes = (decimal)row.TotalExistencia;
            row.TotalExistencia = Math.Max(0, totalExistencia);
            row.UltModificacion = DateTime.UtcNow;
            row.UserName = "admin";
            row.Activo = true;
            _db.InventarioMovimientos.Add(new InventarioMovimiento
            {
                IDEmpresa = idEmpresa,
                IDArticulo = articuloId,
                IDDeposito = depId,
                TipoMovimiento = "ajuste_manual",
                Cantidad = Math.Abs((decimal)row.TotalExistencia - antes),
                ExistenciaAntes = antes,
                ExistenciaDespues = (decimal)row.TotalExistencia,
                Referencia = "AdminStockSet",
                Fecha = DateTime.UtcNow,
                Activo = true,
                UltModificacion = DateTime.UtcNow,
                UserName = "admin"
            });
        }

        // Sync estado visual ecom
        var art = await _db.Articulos.FirstOrDefaultAsync(a => a.ID == articuloId && a.IDEmpresa == idEmpresa, ct);
        if (art is not null)
        {
            if (row.TotalExistencia <= 0 && art.EstadoEcommerce == "activo")
                art.EstadoEcommerce = "agotado";
            else if (row.TotalExistencia > 0 && art.EstadoEcommerce == "agotado")
                art.EstadoEcommerce = "activo";
            art.UltModificacion = DateTime.UtcNow;
        }

        await _db.SaveChangesAsync(ct);
    }
}
