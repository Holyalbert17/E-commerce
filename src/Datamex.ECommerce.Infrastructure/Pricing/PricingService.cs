using Datamex.ECommerce.Application.Abstractions;
using Datamex.ECommerce.Application.Pricing;
using Datamex.ECommerce.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Datamex.ECommerce.Infrastructure.Pricing;

public sealed class PricingService : IPricingService
{
    private readonly EcomDbContext _db;
    private readonly ITenantContext _tenant;

    public PricingService(EcomDbContext db, ITenantContext tenant)
    {
        _db = db;
        _tenant = tenant;
    }

    public async Task<string> ResolveEffectiveCanalAsync(string? requestedCanal, CancellationToken ct = default)
    {
        var requested = NormalizeCanal(requestedCanal);
        if (requested != "B2B")
            return "B2C";

        if (!_tenant.IDUsuarioEcommerce.HasValue || !_tenant.IDEmpresa.HasValue)
            return "B2C";

        var aprobado = await _db.UsuariosEcommerce.AsNoTracking()
            .AnyAsync(u =>
                u.ID == _tenant.IDUsuarioEcommerce.Value
                && u.IDEmpresa == _tenant.IDEmpresa.Value
                && u.Activo
                && u.EstadoMayorista == "Aprobado", ct);

        return aprobado ? "B2B" : "B2C";
    }

    public async Task<ListaPrecioInfo?> ResolveListaAsync(
        string canal, long? idListaOverride = null, CancellationToken ct = default)
    {
        var idEmpresa = RequireTenant();
        var now = DateTime.UtcNow;
        var canalNorm = await ResolveEffectiveCanalAsync(canal, ct);

        if (!idListaOverride.HasValue)
            idListaOverride = await TryResolveCuentaListaAsync(idEmpresa, ct);

        if (idListaOverride.HasValue)
        {
            var specific = await _db.ListasPrecio.AsNoTracking()
                .Where(l => l.IDEmpresa == idEmpresa && l.ID == idListaOverride.Value
                            && l.Activo && l.EsListaPrecioEcommerce && l.Estado == 2)
                .Select(l => new ListaPrecioInfo(l.ID, l.Descripcion, l.CanalEcommerce, l.PrecioConImpuestos))
                .FirstOrDefaultAsync(ct);
            if (specific is not null)
            {
                // Lista de cuenta casi vacía (ej. "ECOMMERCE" con 19 SKUs) → usar la de mayor cobertura.
                var cov = await CountPricedArticulosAsync(idEmpresa, specific.Id, ct);
                if (cov >= 100)
                    return specific;
                idListaOverride = null; // caer al ranking por cobertura
            }
        }

        var listas = await _db.ListasPrecio.AsNoTracking()
            .Where(l => l.IDEmpresa == idEmpresa
                        && l.Activo
                        && l.EsListaPrecioEcommerce
                        && l.Estado == 2
                        && (l.FechaInicioVigencia == null || l.FechaInicioVigencia <= now)
                        && (l.FechaFinVigencia == null || l.FechaFinVigencia >= now)
                        && (l.CanalEcommerce == canalNorm || l.CanalEcommerce == "BOTH"))
            .Select(l => new ListaPrecioInfo(l.ID, l.Descripcion, l.CanalEcommerce, l.PrecioConImpuestos))
            .ToListAsync(ct);

        if (listas.Count == 0)
            return null;
        if (listas.Count == 1)
            return listas[0];

        // Si hay varias listas e-com, preferir la de mayor cobertura de precios.
        var ids = listas.Select(l => l.Id).ToList();
        var ranked = await _db.DetallesListaPrecio.AsNoTracking()
            .Where(d => d.IDEmpresa == idEmpresa && ids.Contains(d.IDListaPrecio) && d.Activo && d.Precio > 0)
            .GroupBy(d => d.IDListaPrecio)
            .Select(g => new { Id = g.Key, N = g.Count() })
            .OrderByDescending(x => x.N)
            .ToListAsync(ct);

        foreach (var r in ranked)
        {
            var hit = listas.FirstOrDefault(l => l.Id == r.Id);
            if (hit is not null)
                return hit;
        }

        return listas.FirstOrDefault(l => l.Canal == canalNorm) ?? listas[0];
    }

    private async Task<int> CountPricedArticulosAsync(long idEmpresa, long idLista, CancellationToken ct)
    {
        var fromSimple = await _db.DetallesListaPrecio.AsNoTracking()
            .Where(d => d.IDEmpresa == idEmpresa && d.IDListaPrecio == idLista && d.Activo && d.Precio > 0)
            .Select(d => d.IDArticuloEmbalaje)
            .Distinct()
            .CountAsync(ct);
        if (fromSimple >= 100)
            return fromSimple;

        var fromCv = await _db.DetallesListaPrecioCondicionVenta.AsNoTracking()
            .Where(d => d.IDEmpresa == idEmpresa && d.IDListaPrecio == idLista && d.Activo && d.PrecioContado > 0)
            .Select(d => d.IDArticuloEmbalaje)
            .Distinct()
            .CountAsync(ct);
        return Math.Max(fromSimple, fromCv);
    }

    private async Task<long?> TryResolveCuentaListaAsync(long idEmpresa, CancellationToken ct)
    {
        var config = await _db.EmpresaConfiguracionesEcommerce.AsNoTracking()
            .FirstOrDefaultAsync(c => c.ID == idEmpresa, ct);
        if (config is null || !config.UsarListaPrecioCuentaErp)
            return null;
        if (!_tenant.IDUsuarioEcommerce.HasValue)
            return null;

        var idCuenta = await _db.UsuariosEcommerceCuenta.AsNoTracking()
            .Where(x => x.IDEmpresa == idEmpresa
                        && x.IDUsuarioEcommerce == _tenant.IDUsuarioEcommerce.Value
                        && x.Activo)
            .OrderByDescending(x => x.EsPrincipal)
            .Select(x => (long?)x.IDCuenta)
            .FirstOrDefaultAsync(ct);
        if (!idCuenta.HasValue)
            return null;

        return await ListaPrecioCuentaHelper.ResolveListaIdForCuentaAsync(_db, idEmpresa, idCuenta.Value, ct);
    }

    public async Task<IReadOnlyDictionary<long, ProductPriceSummaryDto>> GetProductPriceSummariesAsync(
        IEnumerable<long> articuloIds, string canal, CancellationToken ct = default)
    {
        var idEmpresa = RequireTenant();
        var ids = articuloIds.Distinct().ToList();
        if (ids.Count == 0)
            return new Dictionary<long, ProductPriceSummaryDto>();

        var canalEff = await ResolveEffectiveCanalAsync(canal, ct);
        var lista = await ResolveListaAsync(canalEff, null, ct);
        if (lista is null)
            return ids.ToDictionary(id => id, id => new ProductPriceSummaryDto(id, null, null, null, canalEff));

        var embIdsByArt = await _db.ArticuloEmbalajes.AsNoTracking()
            .Where(e => e.IDEmpresa == idEmpresa && e.Activo && ids.Contains(e.IDArticulo))
            .Select(e => new { e.ID, e.IDArticulo })
            .ToListAsync(ct);

        var embIds = embIdsByArt.Select(x => x.ID).ToList();

        var precios = new List<(long IDArticuloEmbalaje, decimal Precio, decimal PorDescuento)>();
        precios.AddRange(await _db.DetallesListaPrecioCondicionVenta.AsNoTracking()
            .Where(d => d.IDEmpresa == idEmpresa
                        && d.Activo
                        && d.IDListaPrecio == lista.Id
                        && embIds.Contains(d.IDArticuloEmbalaje)
                        && d.PrecioContado > 0)
            .Select(d => new ValueTuple<long, decimal, decimal>(
                d.IDArticuloEmbalaje, d.PrecioContado, d.PorDescuentoMinorista))
            .ToListAsync(ct));

        // Fallback: ERP también carga precios en detallelistaprecio (simple).
        var coveredEmbs = precios.Select(p => p.IDArticuloEmbalaje).ToHashSet();
        var missingEmbs = embIds.Where(id => !coveredEmbs.Contains(id)).ToList();
        if (missingEmbs.Count > 0)
        {
            precios.AddRange(await _db.DetallesListaPrecio.AsNoTracking()
                .Where(d => d.IDEmpresa == idEmpresa
                            && d.Activo
                            && d.IDListaPrecio == lista.Id
                            && missingEmbs.Contains(d.IDArticuloEmbalaje)
                            && d.Precio > 0)
                .Select(d => new ValueTuple<long, decimal, decimal>(
                    d.IDArticuloEmbalaje, (decimal)d.Precio, 0m))
                .ToListAsync(ct));
        }

        var listaPorDesc = await _db.ListasPrecio.AsNoTracking()
            .Where(l => l.ID == lista.Id)
            .Select(l => l.PorDescuentoMinorista)
            .FirstOrDefaultAsync(ct);

        var artByEmb = embIdsByArt.ToDictionary(x => x.ID, x => x.IDArticulo);
        var bestByArt = new Dictionary<long, (decimal Precio, decimal? Lista, decimal? Pct)>();

        foreach (var p in precios)
        {
            if (!artByEmb.TryGetValue(p.IDArticuloEmbalaje, out var artId))
                continue;
            var pct = p.PorDescuento > 0 ? p.PorDescuento : listaPorDesc;
            var (precioLista, pctOut) = ComputeListDiscount(p.Precio, pct);
            if (!bestByArt.TryGetValue(artId, out var current) || p.Precio < current.Precio)
                bestByArt[artId] = (p.Precio, precioLista, pctOut);
        }

        return ids.ToDictionary(
            id => id,
            id =>
            {
                if (!bestByArt.TryGetValue(id, out var best))
                    return new ProductPriceSummaryDto(id, null, lista.Id, lista.Descripcion, canalEff);
                return new ProductPriceSummaryDto(
                    id, best.Precio, lista.Id, lista.Descripcion, canalEff, best.Lista, best.Pct);
            });
    }

    public async Task<IReadOnlyList<EmbalajePriceDto>> GetEmbalajePricesAsync(
        long articuloId, string canal, CancellationToken ct = default)
    {
        var idEmpresa = RequireTenant();
        var canalEff = await ResolveEffectiveCanalAsync(canal, ct);
        var lista = await ResolveListaAsync(canalEff, null, ct);
        if (lista is null)
            return Array.Empty<EmbalajePriceDto>();

        var embIds = await _db.ArticuloEmbalajes.AsNoTracking()
            .Where(e => e.IDEmpresa == idEmpresa && e.IDArticulo == articuloId && e.Activo)
            .Select(e => e.ID)
            .ToListAsync(ct);

        var fromCvRows = await _db.DetallesListaPrecioCondicionVenta.AsNoTracking()
            .Where(d => d.IDEmpresa == idEmpresa
                        && d.Activo
                        && d.IDListaPrecio == lista.Id
                        && embIds.Contains(d.IDArticuloEmbalaje)
                        && d.PrecioContado > 0)
            .Select(d => new
            {
                d.IDArticuloEmbalaje,
                d.PrecioContado,
                d.PrecioCredito,
                d.CantidadMinimaMayorista,
                d.PorDescuentoMinorista
            })
            .ToListAsync(ct);

        var listaPorDesc = await _db.ListasPrecio.AsNoTracking()
            .Where(l => l.ID == lista.Id)
            .Select(l => l.PorDescuentoMinorista)
            .FirstOrDefaultAsync(ct);

        var fromCv = fromCvRows.Select(d =>
        {
            var pct = d.PorDescuentoMinorista > 0 ? d.PorDescuentoMinorista : listaPorDesc;
            var (precioLista, pctOut) = ComputeListDiscount(d.PrecioContado, pct);
            return new EmbalajePriceDto(
                d.IDArticuloEmbalaje,
                articuloId,
                d.PrecioContado,
                d.PrecioCredito,
                d.CantidadMinimaMayorista,
                lista.Id,
                lista.Descripcion,
                canalEff,
                lista.PrecioConImpuestos,
                precioLista,
                pctOut);
        }).ToList();

        if (fromCv.Count == embIds.Count)
            return fromCv;

        var covered = fromCv.Select(x => x.IDArticuloEmbalaje).ToHashSet();
        var missing = embIds.Where(id => !covered.Contains(id)).ToList();
        var fromSimple = await _db.DetallesListaPrecio.AsNoTracking()
            .Where(d => d.IDEmpresa == idEmpresa
                        && d.Activo
                        && d.IDListaPrecio == lista.Id
                        && missing.Contains(d.IDArticuloEmbalaje)
                        && d.Precio > 0)
            .Select(d => new EmbalajePriceDto(
                d.IDArticuloEmbalaje,
                articuloId,
                d.Precio,
                d.Precio,
                0m,
                lista.Id,
                lista.Descripcion,
                canalEff,
                lista.PrecioConImpuestos,
                null,
                null))
            .ToListAsync(ct);

        return fromCv.Concat(fromSimple).ToList();
    }

    private static (decimal? PrecioLista, decimal? PorcentajeDescuento) ComputeListDiscount(
        decimal precioContado, decimal porDescuento)
    {
        if (precioContado <= 0 || porDescuento <= 0 || porDescuento >= 100)
            return (null, null);
        var precioLista = Math.Round(precioContado / (1m - porDescuento / 100m), 0, MidpointRounding.AwayFromZero);
        if (precioLista <= precioContado)
            return (null, null);
        return (precioLista, Math.Round(porDescuento, 2));
    }

    private long RequireTenant()
    {
        if (!_tenant.IsResolved || !_tenant.IDEmpresa.HasValue)
            throw new InvalidOperationException("Tenant no resuelto.");
        return _tenant.IDEmpresa.Value;
    }

    private static string NormalizeCanal(string? canal) =>
        string.Equals(canal, "B2B", StringComparison.OrdinalIgnoreCase) ? "B2B" : "B2C";
}
