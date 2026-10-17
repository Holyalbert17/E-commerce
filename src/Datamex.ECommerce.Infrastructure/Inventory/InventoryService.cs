using Datamex.ECommerce.Application.Abstractions;
using Datamex.ECommerce.Application.Inventory;
using Datamex.ECommerce.Domain.Entities;
using Datamex.ECommerce.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Datamex.ECommerce.Infrastructure.Inventory;

/// <summary>
/// Lógica alineada al ecom + Pedido ERP:
/// - Config: EmpresaConfiguracionEcommerce.IDDeposito + ControlaStockEnCarrito
/// - Existencia catálogo/validación = SUM(TotalExistencia) depósitos TipoDeposito.Normal
///   (igual CarritoCompraRepository.TieneArticulosSinStock / ProductoRepository)
/// - Al confirmar pedido Standalone: TotalExistencia −= , TotalPedidoVenta += (CU PedidoMovil)
/// - Al cancelar no migrado: reverso
/// - Integrated: no mutar stock local (lo hace el ERP al facturar)
/// </summary>
public sealed class InventoryService : IInventoryService
{
    private readonly EcomDbContext _db;
    private readonly ITenantContext _tenant;

    public InventoryService(EcomDbContext db, ITenantContext tenant)
    {
        _db = db;
        _tenant = tenant;
    }

    public async Task<DepositoDto?> GetEcommerceDepositoAsync(CancellationToken ct = default)
    {
        var idEmpresa = RequireTenant();
        var config = await _db.EmpresaConfiguracionesEcommerce.AsNoTracking()
            .FirstOrDefaultAsync(c => c.ID == idEmpresa, ct);

        if (config is null)
            return null;

        var dep = await _db.Depositos.AsNoTracking()
            .Where(d => d.IDEmpresa == idEmpresa && d.Activo)
            .OrderByDescending(d => d.ID == config.IDDeposito)
            .ThenByDescending(d => d.EsDepositoEcommerce)
            .ThenBy(d => d.CodDeposito)
            .Select(d => new DepositoDto(d.ID, d.CodDeposito, d.DescripcionDeposito, d.DepositoEntrega))
            .FirstOrDefaultAsync(ct);

        return dep;
    }

    public async Task<StockItemDto?> GetStockAsync(long articuloId, long? depositoId = null, CancellationToken ct = default)
    {
        var map = await GetStockBatchAsync([articuloId], depositoId, ct);
        return map.TryGetValue(articuloId, out var s) ? s : null;
    }

    public async Task<IReadOnlyDictionary<long, StockItemDto>> GetStockBatchAsync(
        IEnumerable<long> articuloIds, long? depositoId = null, CancellationToken ct = default)
    {
        var idEmpresa = RequireTenant();
        var ids = articuloIds.Distinct().ToList();
        if (ids.Count == 0)
            return new Dictionary<long, StockItemDto>();

        var config = await _db.EmpresaConfiguracionesEcommerce.AsNoTracking()
            .FirstOrDefaultAsync(c => c.ID == idEmpresa, ct);

        var controla = config?.ControlaStockEnCarrito ?? false;

        // depósito explícito (pickup/carrito): NO hacer fallback a otra sucursal.
        // vitrina (null): sumar depósitos ecommerce / sucursales de retiro → "con stock" = disponible en alguna.
        List<long>? depositosConsulta = null;
        long? idDepositoConsulta = depositoId;
        string? depNombre = null;

        if (depositoId is > 0)
        {
            idDepositoConsulta = depositoId;
            depNombre = await _db.Depositos.AsNoTracking()
                .Where(d => d.ID == depositoId)
                .Select(d => d.DescripcionDeposito)
                .FirstOrDefaultAsync(ct);
        }
        else
        {
            var pickupDeps = await _db.Agencias.AsNoTracking()
                .Where(a => a.IDEmpresa == idEmpresa && a.Activo && a.EsSucursalEcommerce && a.IDDeposito != null)
                .Select(a => a.IDDeposito!.Value)
                .Distinct()
                .ToListAsync(ct);

            var ecomDeps = await _db.Depositos.AsNoTracking()
                .Where(d => d.IDEmpresa == idEmpresa && d.Activo && d.EsDepositoEcommerce)
                .Select(d => d.ID)
                .ToListAsync(ct);

            depositosConsulta = pickupDeps.Concat(ecomDeps).Distinct().ToList();
            if (config is { IDDeposito: > 0 } && !depositosConsulta.Contains(config.IDDeposito))
                depositosConsulta.Add(config.IDDeposito);

            if (depositosConsulta.Count == 0 && config is { IDDeposito: > 0 })
            {
                idDepositoConsulta = config.IDDeposito;
                depositosConsulta = null;
            }

            if (idDepositoConsulta is > 0)
            {
                depNombre = await _db.Depositos.AsNoTracking()
                    .Where(d => d.ID == idDepositoConsulta)
                    .Select(d => d.DescripcionDeposito)
                    .FirstOrDefaultAsync(ct);
            }
            else if (depositosConsulta is { Count: 1 })
            {
                idDepositoConsulta = depositosConsulta[0];
                depNombre = await _db.Depositos.AsNoTracking()
                    .Where(d => d.ID == idDepositoConsulta)
                    .Select(d => d.DescripcionDeposito)
                    .FirstOrDefaultAsync(ct);
            }
            else
            {
                depNombre = "Sucursales";
            }
        }

        var rows = await (
            from s in _db.Stocks.AsNoTracking()
            join d in _db.Depositos.AsNoTracking() on s.IDDeposito equals d.ID
            join t in _db.TiposDeposito.AsNoTracking() on d.IDTipoDeposito equals t.ID
            where s.IDEmpresa == idEmpresa
                  && s.Activo
                  && !s.ExistenciaCongelada
                  && ids.Contains(s.IDArticulo)
                  && t.Normal
                  && (
                      (depositosConsulta != null && depositosConsulta.Contains(s.IDDeposito))
                      || (depositosConsulta == null && (!idDepositoConsulta.HasValue || s.IDDeposito == idDepositoConsulta.Value))
                  )
            group s by s.IDArticulo into g
            select new
            {
                IDArticulo = g.Key,
                TotalExistencia = g.Sum(x => x.TotalExistencia),
                TotalReserva = g.Sum(x => (double)x.TotalReserva)
            }
        ).ToListAsync(ct);

        var byArt = rows.ToDictionary(r => r.IDArticulo);
        var result = new Dictionary<long, StockItemDto>();
        var depId = idDepositoConsulta ?? 0;

        foreach (var id in ids)
        {
            if (byArt.TryGetValue(id, out var r))
            {
                var existencia = (decimal)r.TotalExistencia;
                var reserva = (decimal)r.TotalReserva;
                var libre = Math.Max(0, existencia - reserva);
                result[id] = new StockItemDto(
                    id, depId, depNombre, existencia, reserva, libre, controla);
            }
            else
            {
                result[id] = new StockItemDto(id, depId, depNombre, 0, 0, 0, controla);
            }
        }

        return result;
    }

    public async Task<StockCheckResult> CheckAvailabilityAsync(
        long articuloId, decimal cantidad, long? depositoId = null, CancellationToken ct = default)
    {
        var idEmpresa = RequireTenant();
        var config = await _db.EmpresaConfiguracionesEcommerce.AsNoTracking()
            .FirstOrDefaultAsync(c => c.ID == idEmpresa, ct);

        if (config is null)
            return new StockCheckResult(true, decimal.MaxValue, null);

        // Integrated: el stock lo refleja el sync ERP — validar contra espejo local.
        // Standalone sin ControlaStockEnCarrito: no bloquea (comportamiento histórico demo).
        var integrated = config.ModoIntegracion == (int)Datamex.ECommerce.Domain.Enums.IntegrationMode.Integrated;
        if (!config.ControlaStockEnCarrito && !integrated)
            return new StockCheckResult(true, decimal.MaxValue, null);

        // null = stock agregado de sucursales ecommerce (vitrina / carrito sin sucursal aún).
        // id concreto = solo ese depósito (retiro en sucursal).
        var stock = await GetStockAsync(articuloId, depositoId, ct);
        // ERP TieneArticulosSinStock: compara contra TotalExistencia (no resta reserva)
        var existencia = stock?.TotalExistencia ?? 0;
        if (existencia >= cantidad)
            return new StockCheckResult(true, existencia, null);

        return new StockCheckResult(false, existencia, $"Stock insuficiente. Disponible: {existencia:0.##}");
    }

    public async Task ReserveAsync(long articuloId, decimal cantidad, long depositoId, CancellationToken ct = default)
    {
        var idEmpresa = RequireTenant();
        var row = await GetOrCreateStockRowAsync(idEmpresa, articuloId, depositoId, ct);

        if ((decimal)row.TotalExistencia < cantidad)
            throw new InvalidOperationException($"Stock insuficiente. Existencia: {row.TotalExistencia:0.##}");

        row.TotalReserva += cantidad;
        row.UltModificacion = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);
    }

    public async Task ReleaseReservationAsync(long articuloId, decimal cantidad, long depositoId, CancellationToken ct = default)
    {
        var idEmpresa = RequireTenant();
        var row = await _db.Stocks.FirstOrDefaultAsync(e =>
            e.IDEmpresa == idEmpresa
            && e.IDArticulo == articuloId
            && e.IDDeposito == depositoId
            && e.IDContenedor == null
            && e.IDLoteArticulo == null
            && e.Activo, ct);

        if (row is null)
            return;

        row.TotalReserva = Math.Max(0, row.TotalReserva - cantidad);
        row.UltModificacion = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);
    }

    public async Task ApplyPedidoStockAsync(
        long depositoId,
        IReadOnlyList<StockLineMove> lines,
        long? skipArticuloId = null,
        CancellationToken ct = default)
    {
        var idEmpresa = RequireTenant();
        var now = DateTime.UtcNow;

        foreach (var group in lines
                     .Where(l => l.Cantidad > 0 && (!skipArticuloId.HasValue || l.IdArticulo != skipArticuloId.Value))
                     .GroupBy(l => l.IdArticulo))
        {
            var qty = (double)group.Sum(x => x.Cantidad);
            if (qty <= 0) continue;

            var row = await GetOrCreateStockRowAsync(idEmpresa, group.Key, depositoId, ct);

            // Paridad PedidoMovil CU: baja existencia y sube compromiso de venta/pedido
            if (row.TotalExistencia < qty)
                throw new InvalidOperationException(
                    $"Stock insuficiente al confirmar (art. {group.Key}). Existencia: {row.TotalExistencia:0.##}, pedido: {qty:0.##}.");

            var antes = (decimal)row.TotalExistencia;
            row.TotalExistencia -= qty;
            row.TotalPedidoVenta += qty;
            // Si había reserva blanda, liberarla
            if (row.TotalReserva > 0)
                row.TotalReserva = Math.Max(0, row.TotalReserva - (decimal)qty);
            row.UltModificacion = now;
            row.UserName = "ecom-pedido";

            _db.InventarioMovimientos.Add(new InventarioMovimiento
            {
                IDEmpresa = idEmpresa,
                IDArticulo = group.Key,
                IDDeposito = depositoId,
                TipoMovimiento = "pedido_venta",
                Cantidad = (decimal)qty,
                ExistenciaAntes = antes,
                ExistenciaDespues = (decimal)row.TotalExistencia,
                Referencia = "ApplyPedidoStock",
                Fecha = now,
                Activo = true,
                UltModificacion = now,
                UserName = "ecom-pedido"
            });
        }

        await _db.SaveChangesAsync(ct);
    }

    public async Task RestorePedidoStockAsync(
        long depositoId,
        IReadOnlyList<StockLineMove> lines,
        long? skipArticuloId = null,
        CancellationToken ct = default)
    {
        var idEmpresa = RequireTenant();
        var now = DateTime.UtcNow;

        foreach (var group in lines
                     .Where(l => l.Cantidad > 0 && (!skipArticuloId.HasValue || l.IdArticulo != skipArticuloId.Value))
                     .GroupBy(l => l.IdArticulo))
        {
            var qty = (double)group.Sum(x => x.Cantidad);
            if (qty <= 0) continue;

            var row = await _db.Stocks.FirstOrDefaultAsync(e =>
                e.IDEmpresa == idEmpresa
                && e.IDArticulo == group.Key
                && e.IDDeposito == depositoId
                && e.IDContenedor == null
                && e.IDLoteArticulo == null
                && e.Activo, ct);

            if (row is null)
                continue;

            var antes = (decimal)row.TotalExistencia;
            // Reverso CU PedidoMovil / SetBackValuesStockOnError
            row.TotalExistencia += qty;
            row.TotalPedidoVenta = Math.Max(0, row.TotalPedidoVenta - qty);
            row.UltModificacion = now;
            row.UserName = "ecom-cancel";

            _db.InventarioMovimientos.Add(new InventarioMovimiento
            {
                IDEmpresa = idEmpresa,
                IDArticulo = group.Key,
                IDDeposito = depositoId,
                TipoMovimiento = "pedido_cancelacion",
                Cantidad = (decimal)qty,
                ExistenciaAntes = antes,
                ExistenciaDespues = (decimal)row.TotalExistencia,
                Referencia = "RestorePedidoStock",
                Fecha = now,
                Activo = true,
                UltModificacion = now,
                UserName = "ecom-cancel"
            });
        }

        await _db.SaveChangesAsync(ct);
    }

    public async Task<IReadOnlyList<InventarioMovimientoDto>> ListMovimientosAsync(
        long? articuloId = null, int take = 100, CancellationToken ct = default)
    {
        var idEmpresa = RequireTenant();
        take = Math.Clamp(take, 1, 500);
        var q = _db.InventarioMovimientos.AsNoTracking()
            .Where(m => m.IDEmpresa == idEmpresa && m.Activo);
        if (articuloId is > 0)
            q = q.Where(m => m.IDArticulo == articuloId.Value);

        return await q
            .OrderByDescending(m => m.Fecha)
            .ThenByDescending(m => m.ID)
            .Take(take)
            .Select(m => new InventarioMovimientoDto(
                m.ID,
                m.IDArticulo,
                m.IDDeposito,
                m.TipoMovimiento,
                m.Cantidad,
                m.ExistenciaAntes,
                m.ExistenciaDespues,
                m.Referencia,
                m.Fecha,
                m.UserName))
            .ToListAsync(ct);
    }

    private async Task<Stock> GetOrCreateStockRowAsync(
        long idEmpresa, long articuloId, long depositoId, CancellationToken ct)
    {
        var row = await _db.Stocks.FirstOrDefaultAsync(e =>
            e.IDEmpresa == idEmpresa
            && e.IDArticulo == articuloId
            && e.IDDeposito == depositoId
            && e.IDContenedor == null
            && e.IDLoteArticulo == null
            && e.Activo, ct);

        if (row is not null)
            return row;

        row = new Stock
        {
            IDEmpresa = idEmpresa,
            IDArticulo = articuloId,
            IDDeposito = depositoId,
            Activo = true,
            UltModificacion = DateTime.UtcNow,
            UserName = "ecom"
        };
        _db.Stocks.Add(row);
        await _db.SaveChangesAsync(ct);
        return row;
    }

    private long RequireTenant()
    {
        if (!_tenant.IsResolved || !_tenant.IDEmpresa.HasValue)
            throw new InvalidOperationException("Tenant no resuelto.");
        return _tenant.IDEmpresa.Value;
    }
}
