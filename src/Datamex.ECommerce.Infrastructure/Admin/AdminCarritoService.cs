using Datamex.ECommerce.Application.Abstractions;
using Datamex.ECommerce.Application.Admin;
using Datamex.ECommerce.Application.Catalog;
using Datamex.ECommerce.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Datamex.ECommerce.Infrastructure.Admin;

public sealed class AdminCarritoService : IAdminCarritoService
{
    private readonly EcomDbContext _db;
    private readonly ITenantContext _tenant;

    public AdminCarritoService(EcomDbContext db, ITenantContext tenant)
    {
        _db = db;
        _tenant = tenant;
    }

    private long EmpresaId => _tenant.IDEmpresa
        ?? throw new InvalidOperationException("Tenant no resuelto.");

    public async Task<PagedResult<AdminCarritoListItemDto>> SearchAsync(
        string? search,
        int? estado,
        bool? migrado,
        DateTime? fechaDesde,
        DateTime? fechaHasta,
        int page,
        int pageSize,
        CancellationToken ct = default)
    {
        var idEmpresa = EmpresaId;
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var q = _db.CarritosCompra.AsNoTracking().Where(c => c.IDEmpresa == idEmpresa);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLowerInvariant();
            q = q.Where(c =>
                c.RazonSocial.ToLower().Contains(term)
                || c.NumeroDocumento.ToLower().Contains(term)
                || c.NroPedidoCarritoCompra.ToString().Contains(term)
                || (c.LugarEntrega != null && c.LugarEntrega.ToLower().Contains(term)));
        }

        if (estado.HasValue) q = q.Where(c => c.Estado == estado.Value);
        if (migrado.HasValue) q = q.Where(c => c.Migrado == migrado.Value);
        if (fechaDesde.HasValue) q = q.Where(c => c.FechaPedido >= fechaDesde.Value);
        if (fechaHasta.HasValue)
        {
            var hasta = fechaHasta.Value.Date.AddDays(1);
            q = q.Where(c => c.FechaPedido < hasta);
        }

        var total = await q.CountAsync(ct);
        var rows = await q
            .OrderByDescending(c => c.FechaPedido)
            .ThenByDescending(c => c.ID)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(c => new AdminCarritoListItemDto(
                c.ID,
                c.NroPedidoCarritoCompra,
                c.FechaPedido,
                c.RazonSocial,
                c.NumeroDocumento,
                c.MontoTotal,
                c.TipoEntrega,
                c.TipoPago,
                c.Estado,
                c.Migrado,
                c.IDPedidoMovil,
                c.IDCuenta,
                c.DetalleCarritoCompras.Count(d => d.Activo),
                c.LugarEntrega))
            .ToListAsync(ct);

        return new PagedResult<AdminCarritoListItemDto>(rows, page, pageSize, total);
    }

    public async Task<AdminCarritoDetailDto?> GetAsync(long id, CancellationToken ct = default)
    {
        var idEmpresa = EmpresaId;
        var c = await _db.CarritosCompra.AsNoTracking()
            .FirstOrDefaultAsync(x => x.ID == id && x.IDEmpresa == idEmpresa, ct);
        if (c is null) return null;

        var lines = await (
            from d in _db.DetallesCarritoCompra.AsNoTracking()
            join a in _db.Articulos.AsNoTracking() on d.IDArticulo equals a.ID into aj
            from a in aj.DefaultIfEmpty()
            where d.IDEmpresa == idEmpresa && d.IDCarritoCompra == id && d.Activo
            select new AdminCarritoLineDto(
                d.ID,
                d.IDArticulo,
                a != null ? a.SKU : null,
                a != null ? a.DescripcionArticulo : $"Artículo #{d.IDArticulo}",
                d.CantidadEmbalada,
                d.PrecioNetoConImpuesto,
                d.MontoTotal)
        ).ToListAsync(ct);

        DateTime? pedidoFecha = null;
        if (c.IDPedidoMovil is long idPedido)
        {
            pedidoFecha = await _db.PedidosMovil.AsNoTracking()
                .Where(p => p.ID == idPedido && p.IDEmpresa == idEmpresa)
                .Select(p => (DateTime?)p.FechaPedido)
                .FirstOrDefaultAsync(ct);
        }

        var trazabilidad = BuildTrazabilidad(c, pedidoFecha);
        return new AdminCarritoDetailDto(
            c.ID, c.NroPedidoCarritoCompra, c.FechaPedido, c.FechaPrevistaEntrega,
            c.RazonSocial, c.NumeroDocumento, c.Observacion, c.LugarEntrega,
            c.TipoEntrega, c.TipoPago, c.Estado, c.Migrado, c.IDPedidoMovil,
            c.IDCuenta, c.IDListaPrecio, c.IDDeposito, c.MontoTotal, c.MontoIVA,
            c.MontoDescuento, c.LatitudEntrega, c.LongitudEntrega, c.UltModificacion,
            lines, trazabilidad);
    }

    /// <summary>
    /// Timeline estilo ERP ConsultaTrazabilidad (sin tabla aparte: derivado de estado/pedido).
    /// </summary>
    private static IReadOnlyList<AdminCarritoTrazabilidadStepDto> BuildTrazabilidad(
        Domain.Entities.CarritoCompra c, DateTime? fechaPedidoMovil)
    {
        var creado = c.Estado >= 0 || c.Activo;
        var confirmado = c.Estado >= 1 || c.IDPedidoMovil.HasValue;
        var procesado = c.IDPedidoMovil.HasValue;
        var migrado = c.Migrado;

        return
        [
            new("CARRITO_CREADO", "Carrito creado", creado, c.FechaPedido,
                $"Nro {c.NroPedidoCarritoCompra}"),
            new("CARRITO_CONFIRMADO", "Carrito confirmado / checkout", confirmado,
                confirmado ? c.UltModificacion : null,
                $"Estado={c.Estado}"),
            new("PEDIDO_PROCESADO", "Pedido móvil generado", procesado, fechaPedidoMovil ?? (procesado ? c.UltModificacion : null),
                c.IDPedidoMovil is long idp ? $"IDPedidoMovil={idp}" : null),
            new("MIGRADO_ERP", "Migrado a ERP", migrado, migrado ? c.UltModificacion : null,
                migrado ? "ProcesaPedidosERP / planilla" : "Pendiente migración"),
        ];
    }

    public async Task<MarkMigradosResult> MarkMigradosAsync(
        IReadOnlyList<long> ids, CancellationToken ct = default)
    {
        if (ids.Count == 0) return new MarkMigradosResult(0);
        var idEmpresa = EmpresaId;
        var rows = await _db.CarritosCompra
            .Where(c => c.IDEmpresa == idEmpresa && ids.Contains(c.ID) && !c.Migrado
                        && c.Estado == Domain.Entities.EstadoCarrito.Cerrado)
            .ToListAsync(ct);

        var now = DateTime.UtcNow;
        foreach (var c in rows)
        {
            c.Migrado = true;
            c.UltModificacion = now;
            c.UserName = "admin-export";
            if (c.IDPedidoMovil is long idPedido)
            {
                var p = await _db.PedidosMovil.FirstOrDefaultAsync(
                    x => x.ID == idPedido && x.IDEmpresa == idEmpresa, ct);
                if (p is not null && !p.Migrado)
                {
                    p.Migrado = true;
                    p.UltModificacion = now;
                    p.UserName = "admin-export";
                }
            }
        }

        await _db.SaveChangesAsync(ct);
        return new MarkMigradosResult(rows.Count);
    }

    public async Task<IReadOnlyList<AdminCarritoExportRowDto>> GetPendingExportAsync(
        IReadOnlyList<long>? ids, CancellationToken ct = default)
    {
        var idEmpresa = EmpresaId;
        var q = _db.CarritosCompra.AsNoTracking()
            .Where(c => c.IDEmpresa == idEmpresa
                        && c.Estado == Domain.Entities.EstadoCarrito.Cerrado
                        && !c.Migrado);

        if (ids is { Count: > 0 })
            q = q.Where(c => ids.Contains(c.ID));

        return await q
            .OrderBy(c => c.FechaPedido)
            .Select(c => new AdminCarritoExportRowDto(
                c.ID, c.NroPedidoCarritoCompra, c.FechaPedido, c.RazonSocial, c.NumeroDocumento,
                c.MontoTotal, c.TipoEntrega, c.TipoPago, c.Estado, c.IDPedidoMovil,
                c.LugarEntrega, c.Observacion))
            .ToListAsync(ct);
    }

    public async Task<byte[]> ExportAbandonadosCsvAsync(int hoursOld = 24, CancellationToken ct = default)
    {
        var idEmpresa = EmpresaId;
        hoursOld = Math.Max(1, hoursOld);
        var cutoff = DateTime.UtcNow.AddHours(-hoursOld);

        var rows = await _db.CarritosCompra.AsNoTracking()
            .Where(c => c.IDEmpresa == idEmpresa
                        && c.Estado == Domain.Entities.EstadoCarrito.Abierto
                        && c.UltModificacion < cutoff)
            .OrderBy(c => c.UltModificacion)
            .Select(c => new AdminCarritoExportRowDto(
                c.ID, c.NroPedidoCarritoCompra, c.FechaPedido, c.RazonSocial, c.NumeroDocumento,
                c.MontoTotal, c.TipoEntrega, c.TipoPago, c.Estado, c.IDPedidoMovil,
                c.LugarEntrega, c.Observacion))
            .ToListAsync(ct);

        var sb = new System.Text.StringBuilder();
        sb.AppendLine("Id,NroPedido,FechaPedido,RazonSocial,Documento,MontoTotal,TipoEntrega,TipoPago,Estado,IDPedidoMovil,LugarEntrega,Observacion,UltModificacionHoras");
        foreach (var r in rows)
        {
            sb.Append(r.Id).Append(',')
                .Append(r.NroPedidoCarritoCompra).Append(',')
                .Append(r.FechaPedido.ToString("o")).Append(',')
                .Append(CsvField(r.RazonSocial)).Append(',')
                .Append(CsvField(r.NumeroDocumento)).Append(',')
                .Append(r.MontoTotal.ToString(System.Globalization.CultureInfo.InvariantCulture)).Append(',')
                .Append(r.TipoEntrega).Append(',')
                .Append(r.TipoPago).Append(',')
                .Append(r.Estado).Append(',')
                .Append(r.IDPedidoMovil?.ToString() ?? "").Append(',')
                .Append(CsvField(r.LugarEntrega ?? "")).Append(',')
                .Append(CsvField(r.Observacion ?? "")).Append(',')
                .Append(hoursOld)
                .AppendLine();
        }

        return System.Text.Encoding.UTF8.GetBytes(sb.ToString());
    }

    private static string CsvField(string v) => "\"" + v.Replace("\"", "\"\"") + "\"";
}
