using Datamex.ECommerce.Application.Abstractions;
using Datamex.ECommerce.Application.Admin;
using Datamex.ECommerce.Domain.Entities;
using Datamex.ECommerce.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Datamex.ECommerce.Infrastructure.Admin;

public sealed class AdminReportsService : IAdminReportsService
{
    private readonly EcomDbContext _db;
    private readonly ITenantContext _tenant;

    public AdminReportsService(EcomDbContext db, ITenantContext tenant)
    {
        _db = db;
        _tenant = tenant;
    }

    public async Task<VentasResumenDto> GetVentasResumenAsync(
        DateTime from, DateTime to, CancellationToken ct = default)
    {
        var idEmpresa = _tenant.IDEmpresa
            ?? throw new InvalidOperationException("Tenant no resuelto.");

        var desde = from.Date;
        var hastaExclusive = to.Date.AddDays(1);
        if (hastaExclusive <= desde)
            throw new ArgumentException("Rango de fechas inválido.");

        var raw = await _db.CarritosCompra.AsNoTracking()
            .Where(c => c.IDEmpresa == idEmpresa
                        && c.Estado == EstadoCarrito.Cerrado
                        && c.FechaPedido >= desde
                        && c.FechaPedido < hastaExclusive
                        && c.IDPedidoMovil != null)
            .Select(c => new { c.FechaPedido, c.MontoTotal, c.ID })
            .ToListAsync(ct);

        var rows = raw
            .GroupBy(c => c.FechaPedido.Date)
            .Select(g => new VentasDiaDto(g.Key, g.Count(), g.Sum(x => x.MontoTotal)))
            .OrderBy(x => x.Fecha)
            .ToList();

        var totalPedidos = rows.Sum(r => r.PedidosCount);
        var montoTotal = rows.Sum(r => r.MontoTotal);
        var ticket = totalPedidos > 0 ? Math.Round(montoTotal / totalPedidos, 0) : 0m;

        var abiertos = await _db.CarritosCompra.AsNoTracking()
            .CountAsync(c => c.IDEmpresa == idEmpresa
                             && c.Estado == EstadoCarrito.Abierto
                             && c.UltModificacion >= desde
                             && c.UltModificacion < hastaExclusive, ct);
        var cerrados = totalPedidos;
        var denom = abiertos + cerrados;
        var conversion = denom > 0 ? Math.Round(100m * cerrados / denom, 1) : 0m;

        var top = new List<TopSkuDto>();
        if (raw.Count > 0)
        {
            var cartIds = raw.Select(c => c.ID).ToList();
            var lines = await _db.DetallesCarritoCompra.AsNoTracking()
                .Where(d => cartIds.Contains(d.IDCarritoCompra) && d.Activo)
                .Select(d => new { d.IDArticulo, d.Cantidad, d.PrecioNetoConImpuesto })
                .ToListAsync(ct);

            var artIds = lines.Select(x => x.IDArticulo).Distinct().ToList();
            var arts = await _db.Articulos.AsNoTracking()
                .Where(a => artIds.Contains(a.ID))
                .Select(a => new { a.ID, a.SKU, a.DescripcionArticulo })
                .ToListAsync(ct);
            var artMap = arts.ToDictionary(a => a.ID);

            top = lines
                .GroupBy(x => x.IDArticulo)
                .Select(g =>
                {
                    artMap.TryGetValue(g.Key, out var a);
                    return new TopSkuDto(
                        g.Key,
                        a?.SKU,
                        a?.DescripcionArticulo ?? $"Artículo {g.Key}",
                        (int)Math.Round(g.Sum(x => (double)x.Cantidad)),
                        g.Sum(x => x.PrecioNetoConImpuesto * (decimal)x.Cantidad));
                })
                .OrderByDescending(x => x.MontoTotal)
                .Take(10)
                .ToList();
        }

        return new VentasResumenDto(
            desde,
            to.Date,
            totalPedidos,
            montoTotal,
            ticket,
            abiertos,
            cerrados,
            conversion,
            rows,
            top);
    }
}
