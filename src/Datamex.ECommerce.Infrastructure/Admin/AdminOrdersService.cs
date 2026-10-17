using Datamex.ECommerce.Application.Abstractions;
using Datamex.ECommerce.Application.Admin;
using Datamex.ECommerce.Application.Catalog;
using Datamex.ECommerce.Application.Options;
using Datamex.ECommerce.Application.Sync;
using Datamex.ECommerce.Domain.Entities;
using Datamex.ECommerce.Infrastructure.Orders;
using Datamex.ECommerce.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Datamex.ECommerce.Infrastructure.Admin;

public sealed class AdminOrdersService : IAdminOrdersService
{
    private readonly EcomDbContext _db;
    private readonly ITenantContext _tenant;
    private readonly PentahoOptions _pentaho;
    private readonly IErpPedidoLifecycleWriter _erpLife;
    private readonly IErpConnectionResolver _erpCs;

    public AdminOrdersService(
        EcomDbContext db,
        ITenantContext tenant,
        IOptions<PentahoOptions> pentaho,
        IErpPedidoLifecycleWriter erpLife,
        IErpConnectionResolver erpCs)
    {
        _db = db;
        _tenant = tenant;
        _pentaho = pentaho.Value;
        _erpLife = erpLife;
        _erpCs = erpCs;
    }

    public async Task<PagedResult<AdminOrderListItemDto>> SearchAsync(
        string? search, bool? migrado, int page, int pageSize,
        string? estadoFulfillment = null, CancellationToken ct = default)
    {
        var idEmpresa = RequireTenant();
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var q = _db.PedidosMovil.AsNoTracking()
            .Where(p => p.IDEmpresa == idEmpresa
                        && p.Activo
                        && p.OrigenPedido == OrigenPedidoMovil.Ecommerce);

        if (migrado.HasValue)
            q = q.Where(p => p.Migrado == migrado.Value);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLowerInvariant();
            q = q.Where(p =>
                p.NroPedido.ToString().Contains(term)
                || (p.Observacion != null && p.Observacion.ToLower().Contains(term))
                || p.CarritoCompras.Any(c =>
                    c.RazonSocial.ToLower().Contains(term)
                    || c.NumeroDocumento.ToLower().Contains(term)));
        }

        if (!string.IsNullOrWhiteSpace(estadoFulfillment))
        {
            var ef = estadoFulfillment.Trim().ToLowerInvariant();
            var latest = await _db.SeguimientosPedidoEcommerce.AsNoTracking()
                .Where(s => s.IDEmpresa == idEmpresa && s.Activo)
                .GroupBy(s => s.IDPedidoMovil)
                .Select(g => new
                {
                    Id = g.Key,
                    Estado = g.OrderByDescending(x => x.FechaEstado).ThenByDescending(x => x.ID)
                        .Select(x => x.Estado).FirstOrDefault()
                })
                .ToListAsync(ct);
            var matchIds = latest.Where(x => string.Equals(x.Estado, ef, StringComparison.OrdinalIgnoreCase))
                .Select(x => x.Id)
                .ToList();
            q = q.Where(p => matchIds.Contains(p.ID));
        }

        var total = await q.CountAsync(ct);

        var rows = await q
            .OrderByDescending(p => p.FechaPedido)
            .ThenByDescending(p => p.ID)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(p => new
            {
                p.ID,
                p.NroPedido,
                p.FechaPedido,
                p.Estado,
                p.Migrado,
                p.NroFactura,
                ItemsCount = p.DetallePedidoMoviles.Count(d => d.Activo),
                Cart = p.CarritoCompras
                    .OrderByDescending(c => c.ID)
                    .Select(c => new
                    {
                        c.RazonSocial,
                        c.NumeroDocumento,
                        c.MontoTotal,
                        c.TipoEntrega,
                        c.TipoPago,
                        c.LugarEntrega
                    })
                    .FirstOrDefault()
            })
            .ToListAsync(ct);

        var ids = rows.Select(r => r.ID).ToList();
        var lastTrack = await _db.SeguimientosPedidoEcommerce.AsNoTracking()
            .Where(s => s.IDEmpresa == idEmpresa && ids.Contains(s.IDPedidoMovil) && s.Activo)
            .GroupBy(s => s.IDPedidoMovil)
            .Select(g => new
            {
                Id = g.Key,
                Estado = g.OrderByDescending(x => x.FechaEstado).ThenByDescending(x => x.ID)
                    .Select(x => x.Estado).FirstOrDefault()
            })
            .ToListAsync(ct);
        var trackMap = lastTrack.ToDictionary(x => x.Id, x => x.Estado);

        var items = rows.Select(r => new AdminOrderListItemDto(
            r.ID,
            r.NroPedido,
            r.FechaPedido,
            r.Cart?.RazonSocial ?? "(sin carrito)",
            r.Cart?.NumeroDocumento ?? "",
            r.Cart?.MontoTotal ?? 0,
            r.Cart?.TipoEntrega ?? 0,
            r.Cart?.TipoPago ?? 0,
            r.Estado,
            r.Migrado,
            r.ItemsCount,
            r.Cart?.LugarEntrega,
            trackMap.GetValueOrDefault(r.ID),
            r.NroFactura,
            FacturaSerie.EsFacturado(r.NroFactura),
            FacturaSerie.EsImprimibleErp(r.NroFactura) && r.Migrado
        )).ToList();

        return new PagedResult<AdminOrderListItemDto>(items, page, pageSize, total);
    }

    public async Task<AdminOrderDetailDto?> GetAsync(long idPedidoMovil, CancellationToken ct = default)
    {
        var idEmpresa = RequireTenant();
        var p = await _db.PedidosMovil.AsNoTracking()
            .Include(x => x.DetallePedidoMoviles)
            .Include(x => x.CarritoCompras)
            .Include(x => x.Chofer)
            .FirstOrDefaultAsync(x => x.ID == idPedidoMovil && x.IDEmpresa == idEmpresa, ct);

        if (p is null)
            return null;

        if (p.Migrado && !FacturaSerie.EsFacturado(p.NroFactura))
        {
            var sync = await _erpLife.SincronizarFacturaAsync(idEmpresa, idPedidoMovil, ct);
            if (sync.Ok && sync.NroFactura is > 0)
            {
                p = await _db.PedidosMovil.AsNoTracking()
                    .Include(x => x.DetallePedidoMoviles)
                    .Include(x => x.CarritoCompras)
                    .Include(x => x.Chofer)
                    .FirstAsync(x => x.ID == idPedidoMovil && x.IDEmpresa == idEmpresa, ct);
            }
        }

        var cart = p.CarritoCompras.OrderByDescending(c => c.ID).FirstOrDefault();
        var artIds = p.DetallePedidoMoviles.Select(d => d.IDArticulo).Distinct().ToList();
        var arts = await _db.Articulos.AsNoTracking()
            .Where(a => artIds.Contains(a.ID))
            .Select(a => new { a.ID, a.SKU, a.DescripcionArticulo })
            .ToListAsync(ct);
        var map = arts.ToDictionary(a => a.ID);

        var lines = p.DetallePedidoMoviles
            .Where(d => d.Activo)
            .Select(d =>
            {
                map.TryGetValue(d.IDArticulo, out var a);
                var qty = d.CantidadEmbalada;
                var price = (decimal)d.PrecioNetoConImpuesto;
                return new AdminOrderLineDto(
                    d.IDArticulo,
                    a?.SKU,
                    a?.DescripcionArticulo ?? $"Artículo {d.IDArticulo}",
                    qty,
                    price,
                    price * qty);
            })
            .ToList();

        return new AdminOrderDetailDto(
            p.ID,
            p.NroPedido,
            cart?.ID,
            p.FechaPedido,
            p.FechaPrevistaEntrega,
            cart?.RazonSocial ?? "",
            cart?.NumeroDocumento ?? "",
            p.Observacion ?? cart?.Observacion,
            cart?.LugarEntrega ?? p.DireccionEntrega,
            cart?.TipoEntrega ?? 0,
            cart?.TipoPago ?? 0,
            p.Estado,
            p.Migrado,
            p.OrigenPedido,
            cart?.MontoTotal ?? lines.Sum(l => l.MontoLinea),
            cart?.MontoIVA ?? 0,
            p.IDDeposito,
            lines,
            p.Latitud ?? cart?.LatitudEntrega,
            p.Longitud ?? cart?.LongitudEntrega,
            p.DireccionEntrega ?? cart?.LugarEntrega,
            p.IDChofer,
            p.Chofer?.Nombre,
            await _db.SeguimientosPedidoEcommerce.AsNoTracking()
                .Where(s => s.IDEmpresa == idEmpresa && s.IDPedidoMovil == p.ID && s.Activo)
                .OrderByDescending(s => s.FechaEstado).ThenByDescending(s => s.ID)
                .Select(s => s.Estado)
                .FirstOrDefaultAsync(ct),
            p.NroFactura,
            FacturaSerie.EsFacturado(p.NroFactura),
            FacturaSerie.EsImprimibleErp(p.NroFactura) && p.Migrado);
    }

    public async Task<AsignarFacturaResult> AsignarFacturaAsync(
        long idPedidoMovil, long? nroFactura = null, CancellationToken ct = default)
    {
        var idEmpresa = RequireTenant();
        var pedido = await _db.PedidosMovil
            .FirstOrDefaultAsync(p => p.ID == idPedidoMovil
                                      && p.IDEmpresa == idEmpresa
                                      && p.OrigenPedido == OrigenPedidoMovil.Ecommerce, ct)
            ?? throw new InvalidOperationException("Pedido no encontrado.");

        if (pedido.Estado == EstadoPedidoMovil.Anulado)
            throw new InvalidOperationException("No se puede facturar un pedido anulado.");

        // Con ERP write-back: la factura fiscal sale del ERP (planilla/caja), no del admin e-com.
        var allowWb = await _erpCs.AllowWriteBackAsync(idEmpresa, ct);
        var cs = await _erpCs.ResolveAsync(idEmpresa, ct);
        if (allowWb && !string.IsNullOrWhiteSpace(cs))
        {
            var sync = await _erpLife.SincronizarFacturaAsync(idEmpresa, idPedidoMovil, ct);
            if (sync.Ok && sync.NroFactura is > 0)
            {
                await EnsureTrackingFacturadoAsync(idEmpresa, pedido.ID, sync.NroFactura.Value, ct);
                return new AsignarFacturaResult(pedido.ID, pedido.NroPedido, sync.NroFactura.Value, true);
            }

            throw new InvalidOperationException(
                "La factura debe emitirse en el ERP. Cuando esté lista, usá «Traer factura ERP». "
                + (sync.Mensaje ?? ""));
        }

        if (FacturaSerie.EsFiscalErp(pedido.NroFactura))
        {
            await EnsureTrackingFacturadoAsync(idEmpresa, pedido.ID, pedido.NroFactura!.Value, ct);
            return new AsignarFacturaResult(pedido.ID, pedido.NroPedido, pedido.NroFactura.Value, true);
        }

        if (pedido.NroFactura is >= FacturaSerie.LocalMin)
        {
            await EnsureTrackingFacturadoAsync(idEmpresa, pedido.ID, pedido.NroFactura.Value, ct);
            return new AsignarFacturaResult(pedido.ID, pedido.NroPedido, pedido.NroFactura.Value, true);
        }

        long nro;
        if (nroFactura is > 0)
        {
            nro = nroFactura.Value;
            var clash = await _db.PedidosMovil.AsNoTracking()
                .AnyAsync(p => p.IDEmpresa == idEmpresa && p.NroFactura == nro && p.ID != pedido.ID, ct);
            if (clash)
                throw new InvalidOperationException($"Ya existe una factura N° {nro} en esta empresa.");
        }
        else
        {
            nro = await AllocateNroFacturaAsync(idEmpresa, ct);
        }

        pedido.NroFactura = nro;
        pedido.UltModificacion = DateTime.UtcNow;
        pedido.UserName = "admin-factura";
        await _db.SaveChangesAsync(ct);
        await EnsureTrackingFacturadoAsync(idEmpresa, pedido.ID, nro, ct);
        return new AsignarFacturaResult(pedido.ID, pedido.NroPedido, nro, false);
    }

    public async Task<ErpFacturaSyncResultDto> SyncFacturaErpAsync(long idPedidoMovil, CancellationToken ct = default)
    {
        var idEmpresa = RequireTenant();
        _ = await _db.PedidosMovil.AsNoTracking()
                .FirstOrDefaultAsync(p => p.ID == idPedidoMovil && p.IDEmpresa == idEmpresa, ct)
            ?? throw new InvalidOperationException("Pedido no encontrado.");

        var r = await _erpLife.SincronizarFacturaAsync(idEmpresa, idPedidoMovil, ct);
        if (r.Ok && r.NroFactura is > 0)
            await EnsureTrackingFacturadoAsync(idEmpresa, idPedidoMovil, r.NroFactura.Value, ct);
        return new ErpFacturaSyncResultDto(r.Ok, r.Mensaje, r.NroFactura, r.IdPedidoErp, r.EsFiscalErp);
    }

    public async Task<AsignarFacturaBulkResult> AsignarFacturaBulkAsync(
        IReadOnlyList<long> ids, CancellationToken ct = default)
    {
        var items = new List<AsignarFacturaResult>();
        var ok = 0;
        var err = 0;
        foreach (var id in ids.Distinct())
        {
            try
            {
                items.Add(await AsignarFacturaAsync(id, null, ct));
                ok++;
            }
            catch
            {
                err++;
            }
        }

        return new AsignarFacturaBulkResult(ids.Count, ok, err, items);
    }

    private async Task<long> AllocateNroFacturaAsync(long idEmpresa, CancellationToken ct)
    {
        var max = await _db.PedidosMovil.AsNoTracking()
            .Where(p => p.IDEmpresa == idEmpresa && p.NroFactura != null)
            .MaxAsync(p => (long?)p.NroFactura, ct) ?? 0;
        // Serie local e-com: empieza en 1_000_000 para no chocar con series fiscales ERP típicas.
        return Math.Max(max + 1, FacturaSerie.LocalMin);
    }

    private async Task EnsureTrackingFacturadoAsync(
        long idEmpresa, long idPedidoMovil, long nroFactura, CancellationToken ct)
    {
        var last = await _db.SeguimientosPedidoEcommerce.AsNoTracking()
            .Where(s => s.IDEmpresa == idEmpresa && s.IDPedidoMovil == idPedidoMovil && s.Activo)
            .OrderByDescending(s => s.FechaEstado).ThenByDescending(s => s.ID)
            .Select(s => s.Estado)
            .FirstOrDefaultAsync(ct);

        if (string.Equals(last, FulfillmentEstado.Facturado, StringComparison.OrdinalIgnoreCase)
            || string.Equals(last, FulfillmentEstado.Entregado, StringComparison.OrdinalIgnoreCase)
            || string.Equals(last, FulfillmentEstado.Cancelado, StringComparison.OrdinalIgnoreCase))
            return;

        _db.SeguimientosPedidoEcommerce.Add(new SeguimientoPedidoEcommerce
        {
            IDEmpresa = idEmpresa,
            IDPedidoMovil = idPedidoMovil,
            Estado = FulfillmentEstado.Facturado,
            Mensaje = $"Factura N° {nroFactura}",
            CodigoTracking = null,
            FechaEstado = DateTime.UtcNow,
            Activo = true,
            UltModificacion = DateTime.UtcNow,
            UserName = "admin-factura"
        });
        await _db.SaveChangesAsync(ct);
    }

    public async Task<MarkMigradosResult> MarkMigradosAsync(
        IReadOnlyList<long> ids, CancellationToken ct = default)
    {
        if (ids.Count == 0) return new MarkMigradosResult(0);
        var idEmpresa = RequireTenant();
        var rows = await _db.PedidosMovil
            .Where(p => p.IDEmpresa == idEmpresa
                        && ids.Contains(p.ID)
                        && p.OrigenPedido == OrigenPedidoMovil.Ecommerce
                        && !p.Migrado)
            .ToListAsync(ct);

        var now = DateTime.UtcNow;
        foreach (var p in rows)
        {
            p.Migrado = true;
            p.UltModificacion = now;
            p.UserName = "admin-etl";

            var carts = await _db.CarritosCompra
                .Where(c => c.IDEmpresa == idEmpresa && c.IDPedidoMovil == p.ID && !c.Migrado)
                .ToListAsync(ct);
            foreach (var c in carts)
            {
                c.Migrado = true;
                c.UltModificacion = now;
                c.UserName = "admin-etl";
            }
        }

        await _db.SaveChangesAsync(ct);
        return new MarkMigradosResult(rows.Count);
    }

    public async Task<IReadOnlyList<AdminOrderExportRowDto>> GetExportAsync(
        DateTime? fechaDesde = null, DateTime? fechaHasta = null, CancellationToken ct = default)
    {
        var idEmpresa = RequireTenant();
        var q = _db.PedidosMovil.AsNoTracking()
            .Where(p => p.IDEmpresa == idEmpresa
                        && p.Activo
                        && p.OrigenPedido == OrigenPedidoMovil.Ecommerce);

        if (fechaDesde.HasValue)
            q = q.Where(p => p.FechaPedido >= fechaDesde.Value);
        if (fechaHasta.HasValue)
        {
            var hasta = fechaHasta.Value.Date.AddDays(1);
            q = q.Where(p => p.FechaPedido < hasta);
        }

        var rows = await q
            .OrderByDescending(p => p.FechaPedido)
            .ThenByDescending(p => p.ID)
            .Select(p => new
            {
                p.ID,
                p.NroPedido,
                p.FechaPedido,
                p.Estado,
                p.Migrado,
                p.NroFactura,
                Cart = p.CarritoCompras
                    .OrderByDescending(c => c.ID)
                    .Select(c => new
                    {
                        c.RazonSocial,
                        c.NumeroDocumento,
                        c.MontoTotal,
                        c.TipoEntrega,
                        c.TipoPago,
                        c.LugarEntrega
                    })
                    .FirstOrDefault()
            })
            .Take(5000)
            .ToListAsync(ct);

        return rows.Select(r => new AdminOrderExportRowDto(
            r.ID,
            r.NroPedido,
            r.FechaPedido,
            r.Cart?.RazonSocial ?? "",
            r.Cart?.NumeroDocumento ?? "",
            r.Cart?.MontoTotal ?? 0,
            r.Cart?.TipoEntrega ?? 0,
            r.Cart?.TipoPago ?? 0,
            r.Estado,
            r.Migrado,
            r.Cart?.LugarEntrega,
            r.NroFactura
        )).ToList();
    }

    private long RequireTenant()
    {
        if (!_tenant.IsResolved || !_tenant.IDEmpresa.HasValue)
            throw new InvalidOperationException("Tenant no resuelto.");
        return _tenant.IDEmpresa.Value;
    }
}

public sealed class AdminDashboardService : IAdminDashboardService
{
    private readonly EcomDbContext _db;
    private readonly ITenantContext _tenant;

    public AdminDashboardService(EcomDbContext db, ITenantContext tenant)
    {
        _db = db;
        _tenant = tenant;
    }

    public async Task<AdminDashboardDto> GetAsync(CancellationToken ct = default)
    {
        var idEmpresa = RequireTenant();
        var today = DateTime.UtcNow.Date;

        var articulos = await _db.Articulos.AsNoTracking()
            .CountAsync(a => a.IDEmpresa == idEmpresa && a.Activo && a.ActivoEcommerce, ct);
        var agotados = await _db.Articulos.AsNoTracking()
            .CountAsync(a => a.IDEmpresa == idEmpresa && a.ActivoEcommerce && a.EstadoEcommerce == "agotado", ct);
        var pedidosHoy = await _db.PedidosMovil.AsNoTracking()
            .CountAsync(p => p.IDEmpresa == idEmpresa
                             && p.OrigenPedido == OrigenPedidoMovil.Ecommerce
                             && p.FechaPedido >= today, ct);
        var pendientes = await _db.PedidosMovil.AsNoTracking()
            .CountAsync(p => p.IDEmpresa == idEmpresa
                             && p.OrigenPedido == OrigenPedidoMovil.Ecommerce
                             && !p.Migrado, ct);

        var montoHoyRows = await _db.CarritosCompra.AsNoTracking()
            .Where(c => c.IDEmpresa == idEmpresa
                        && c.Estado == EstadoCarrito.Cerrado
                        && c.FechaPedido >= today
                        && c.IDPedidoMovil != null)
            .Select(c => (double)c.MontoTotal)
            .ToListAsync(ct);
        var montoHoy = (decimal)montoHoyRows.Sum();

        return new AdminDashboardDto(articulos, agotados, pedidosHoy, pendientes, montoHoy);
    }

    private long RequireTenant()
    {
        if (!_tenant.IsResolved || !_tenant.IDEmpresa.HasValue)
            throw new InvalidOperationException("Tenant no resuelto.");
        return _tenant.IDEmpresa.Value;
    }
}
