using Datamex.ECommerce.Application.Abstractions;
using Datamex.ECommerce.Application.Admin;
using Datamex.ECommerce.Domain.Entities;
using Datamex.ECommerce.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Datamex.ECommerce.Infrastructure.Admin;

public sealed class AdminListaPrecioService : IAdminListaPrecioService
{
    private readonly EcomDbContext _db;
    private readonly ITenantContext _tenant;

    public AdminListaPrecioService(EcomDbContext db, ITenantContext tenant)
    {
        _db = db;
        _tenant = tenant;
    }

    private long EmpresaId => _tenant.IDEmpresa
        ?? throw new InvalidOperationException("Tenant no resuelto.");

    public async Task<IReadOnlyList<AdminListaPrecioDto>> ListAsync(CancellationToken ct = default)
    {
        var idEmpresa = EmpresaId;
        var rows = await _db.ListasPrecio.AsNoTracking()
            .Where(l => l.IDEmpresa == idEmpresa)
            .OrderBy(l => l.NroListaPrecio)
            .Select(l => new
            {
                l.ID, l.NroListaPrecio, l.Descripcion, l.EsListaPrecioEcommerce,
                l.PrecioConImpuestos, l.CanalEcommerce, l.FechaInicioVigencia, l.FechaFinVigencia,
                l.Activo, l.Estado,
                l.CantidadMinimaMayorista, l.PorDescuentoMinorista, l.PorDescuentoMayorista,
                DetalleCount = l.DetallesCondicionVenta.Count(d => d.Activo)
            })
            .ToListAsync(ct);

        return rows.Select(l => new AdminListaPrecioDto(
            l.ID, l.NroListaPrecio, l.Descripcion, l.EsListaPrecioEcommerce,
            l.PrecioConImpuestos, l.CanalEcommerce, l.FechaInicioVigencia, l.FechaFinVigencia,
            l.Activo, l.Estado, l.DetalleCount,
            l.CantidadMinimaMayorista, l.PorDescuentoMinorista, l.PorDescuentoMayorista)).ToList();
    }

    public async Task<AdminListaPrecioDto?> GetAsync(long id, CancellationToken ct = default)
    {
        var idEmpresa = EmpresaId;
        var l = await _db.ListasPrecio.AsNoTracking()
            .Where(x => x.ID == id && x.IDEmpresa == idEmpresa)
            .Select(x => new
            {
                x.ID, x.NroListaPrecio, x.Descripcion, x.EsListaPrecioEcommerce,
                x.PrecioConImpuestos, x.CanalEcommerce, x.FechaInicioVigencia, x.FechaFinVigencia,
                x.Activo, x.Estado,
                x.CantidadMinimaMayorista, x.PorDescuentoMinorista, x.PorDescuentoMayorista,
                DetalleCount = x.DetallesCondicionVenta.Count(d => d.Activo)
            })
            .FirstOrDefaultAsync(ct);
        if (l is null) return null;
        return new AdminListaPrecioDto(
            l.ID, l.NroListaPrecio, l.Descripcion, l.EsListaPrecioEcommerce,
            l.PrecioConImpuestos, l.CanalEcommerce, l.FechaInicioVigencia, l.FechaFinVigencia,
            l.Activo, l.Estado, l.DetalleCount,
            l.CantidadMinimaMayorista, l.PorDescuentoMinorista, l.PorDescuentoMayorista);
    }

    public async Task<AdminListaPrecioDto> CreateAsync(UpsertListaPrecioRequest request, CancellationToken ct = default)
    {
        var idEmpresa = EmpresaId;
        ValidateLista(request);
        var now = DateTime.UtcNow;
        var nro = request.NroListaPrecio ?? ((await _db.ListasPrecio.AsNoTracking()
            .Where(l => l.IDEmpresa == idEmpresa)
            .MaxAsync(l => (int?)l.NroListaPrecio, ct)) ?? 0) + 1;

        if (await _db.ListasPrecio.AnyAsync(l => l.IDEmpresa == idEmpresa && l.NroListaPrecio == nro, ct))
            throw new InvalidOperationException($"Ya existe NroListaPrecio={nro}.");

        var l = new ListaPrecio
        {
            IDEmpresa = idEmpresa,
            Activo = request.Activo,
            UltModificacion = now,
            UserName = "admin",
            NroListaPrecio = nro,
            Descripcion = request.Descripcion.Trim(),
            EsListaPrecioEcommerce = request.EsListaPrecioEcommerce,
            PrecioConImpuestos = request.PrecioConImpuestos,
            CanalEcommerce = NormalizeCanal(request.CanalEcommerce),
            FechaRegistro = now,
            FechaInicioVigencia = request.FechaInicioVigencia ?? now.Date,
            FechaFinVigencia = request.FechaFinVigencia,
            CantidadMinimaMayorista = Math.Max(0, request.CantidadMinimaMayorista ?? 0),
            PorDescuentoMinorista = Math.Clamp(request.PorDescuentoMinorista ?? 0, 0, 100),
            PorDescuentoMayorista = Math.Clamp(request.PorDescuentoMayorista ?? 0, 0, 100),
            Estado = request.Estado ?? 1 // ABIERTO pendiente auth
        };
        _db.ListasPrecio.Add(l);
        await _db.SaveChangesAsync(ct);
        return (await GetAsync(l.ID, ct))!;
    }

    public async Task<AdminListaPrecioDto> UpdateAsync(long id, UpsertListaPrecioRequest request, CancellationToken ct = default)
    {
        var idEmpresa = EmpresaId;
        ValidateLista(request);
        var l = await _db.ListasPrecio.FirstOrDefaultAsync(x => x.ID == id && x.IDEmpresa == idEmpresa, ct)
            ?? throw new InvalidOperationException("Lista de precio no encontrada.");

        if (request.NroListaPrecio is int nro && nro != l.NroListaPrecio)
        {
            if (await _db.ListasPrecio.AnyAsync(x => x.IDEmpresa == idEmpresa && x.NroListaPrecio == nro && x.ID != id, ct))
                throw new InvalidOperationException($"Ya existe NroListaPrecio={nro}.");
            l.NroListaPrecio = nro;
        }

        l.Descripcion = request.Descripcion.Trim();
        l.EsListaPrecioEcommerce = request.EsListaPrecioEcommerce;
        l.PrecioConImpuestos = request.PrecioConImpuestos;
        l.CanalEcommerce = NormalizeCanal(request.CanalEcommerce);
        l.FechaInicioVigencia = request.FechaInicioVigencia;
        l.FechaFinVigencia = request.FechaFinVigencia;
        if (request.CantidadMinimaMayorista.HasValue)
            l.CantidadMinimaMayorista = Math.Max(0, request.CantidadMinimaMayorista.Value);
        if (request.PorDescuentoMinorista.HasValue)
            l.PorDescuentoMinorista = Math.Clamp(request.PorDescuentoMinorista.Value, 0, 100);
        if (request.PorDescuentoMayorista.HasValue)
            l.PorDescuentoMayorista = Math.Clamp(request.PorDescuentoMayorista.Value, 0, 100);
        l.Activo = request.Activo;
        if (request.Estado.HasValue) l.Estado = request.Estado.Value;
        l.UltModificacion = DateTime.UtcNow;
        l.UserName = "admin";
        await _db.SaveChangesAsync(ct);
        return (await GetAsync(id, ct))!;
    }

    public async Task DeleteAsync(long id, CancellationToken ct = default)
    {
        var idEmpresa = EmpresaId;
        var l = await _db.ListasPrecio.FirstOrDefaultAsync(x => x.ID == id && x.IDEmpresa == idEmpresa, ct)
            ?? throw new InvalidOperationException("Lista de precio no encontrada.");
        l.Activo = false;
        l.Estado = 0; // ANULADO
        l.UltModificacion = DateTime.UtcNow;
        l.UserName = "admin";
        await _db.SaveChangesAsync(ct);
    }

    public async Task<IReadOnlyList<AdminDetalleListaDto>> ListDetallesAsync(long idLista, CancellationToken ct = default)
    {
        var idEmpresa = EmpresaId;
        await EnsureListaAsync(idEmpresa, idLista, ct);

        var rows = await (
            from d in _db.DetallesListaPrecioCondicionVenta.AsNoTracking()
            join e in _db.ArticuloEmbalajes.AsNoTracking() on d.IDArticuloEmbalaje equals e.ID
            join a in _db.Articulos.AsNoTracking() on e.IDArticulo equals a.ID
            where d.IDEmpresa == idEmpresa && d.IDListaPrecio == idLista
            orderby a.DescripcionArticulo, e.DescripcionEmbalaje
            select new AdminDetalleListaDto(
                d.ID, d.IDArticuloEmbalaje, a.ID, a.SKU, a.DescripcionArticulo,
                e.DescripcionEmbalaje, d.PrecioContado, d.PrecioCheque, d.PrecioCredito,
                d.CantidadMinimaMayorista, d.Activo)
        ).ToListAsync(ct);

        return rows;
    }

    public async Task<AdminDetalleListaDto> UpsertDetalleAsync(
        long idLista, UpsertDetalleListaRequest request, CancellationToken ct = default)
    {
        var idEmpresa = EmpresaId;
        await EnsureListaAsync(idEmpresa, idLista, ct);
        if (request.IDArticuloEmbalaje <= 0)
            throw new ArgumentException("IDArticuloEmbalaje requerido.");
        if (request.PrecioContado < 0)
            throw new ArgumentException("PrecioContado inválido.");

        var emb = await _db.ArticuloEmbalajes.AsNoTracking()
            .FirstOrDefaultAsync(e => e.ID == request.IDArticuloEmbalaje && e.IDEmpresa == idEmpresa, ct)
            ?? throw new InvalidOperationException("Embalaje no encontrado.");

        var existing = await _db.DetallesListaPrecioCondicionVenta
            .FirstOrDefaultAsync(d =>
                d.IDEmpresa == idEmpresa
                && d.IDListaPrecio == idLista
                && d.IDArticuloEmbalaje == request.IDArticuloEmbalaje, ct);

        var now = DateTime.UtcNow;
        if (existing is null)
        {
            existing = new DetalleListaPrecioCondicionVenta
            {
                IDEmpresa = idEmpresa,
                IDListaPrecio = idLista,
                IDArticuloEmbalaje = request.IDArticuloEmbalaje,
                Activo = request.Activo,
                UltModificacion = now,
                UserName = "admin",
                PrecioContado = request.PrecioContado,
                PrecioCheque = request.PrecioCheque > 0 ? request.PrecioCheque : request.PrecioContado,
                PrecioCredito = request.PrecioCredito > 0 ? request.PrecioCredito : request.PrecioContado,
                CantidadMinimaMayorista = request.CantidadMinimaMayorista,
                EsAutomatico = false
            };
            _db.DetallesListaPrecioCondicionVenta.Add(existing);
        }
        else
        {
            existing.PrecioContado = request.PrecioContado;
            existing.PrecioCheque = request.PrecioCheque > 0 ? request.PrecioCheque : request.PrecioContado;
            existing.PrecioCredito = request.PrecioCredito > 0 ? request.PrecioCredito : request.PrecioContado;
            existing.CantidadMinimaMayorista = request.CantidadMinimaMayorista;
            existing.Activo = request.Activo;
            existing.UltModificacion = now;
            existing.UserName = "admin";
        }

        await _db.SaveChangesAsync(ct);
        return (await ListDetallesAsync(idLista, ct)).First(d => d.Id == existing.ID);
    }

    public async Task<AdminDetalleListaDto> UpdateDetalleAsync(
        long idLista, long detalleId, UpdateDetalleListaRequest request, CancellationToken ct = default)
    {
        var idEmpresa = EmpresaId;
        var d = await _db.DetallesListaPrecioCondicionVenta
            .FirstOrDefaultAsync(x => x.ID == detalleId && x.IDListaPrecio == idLista && x.IDEmpresa == idEmpresa, ct)
            ?? throw new InvalidOperationException("Detalle no encontrado.");

        if (request.PrecioContado.HasValue) d.PrecioContado = Math.Max(0, request.PrecioContado.Value);
        if (request.PrecioCheque.HasValue) d.PrecioCheque = Math.Max(0, request.PrecioCheque.Value);
        if (request.PrecioCredito.HasValue) d.PrecioCredito = Math.Max(0, request.PrecioCredito.Value);
        if (request.CantidadMinimaMayorista.HasValue) d.CantidadMinimaMayorista = Math.Max(0, request.CantidadMinimaMayorista.Value);
        if (request.Activo.HasValue) d.Activo = request.Activo.Value;
        d.UltModificacion = DateTime.UtcNow;
        d.UserName = "admin";
        await _db.SaveChangesAsync(ct);

        return (await ListDetallesAsync(idLista, ct)).First(x => x.Id == detalleId);
    }

    public async Task DeleteDetalleAsync(long idLista, long detalleId, CancellationToken ct = default)
    {
        var idEmpresa = EmpresaId;
        var d = await _db.DetallesListaPrecioCondicionVenta
            .FirstOrDefaultAsync(x => x.ID == detalleId && x.IDListaPrecio == idLista && x.IDEmpresa == idEmpresa, ct)
            ?? throw new InvalidOperationException("Detalle no encontrado.");
        d.Activo = false;
        d.UltModificacion = DateTime.UtcNow;
        d.UserName = "admin";
        await _db.SaveChangesAsync(ct);
    }

    public async Task<IReadOnlyList<AdminEmbalajeOptionDto>> SearchEmbalajesAsync(
        string? search, int take = 30, CancellationToken ct = default)
    {
        var idEmpresa = EmpresaId;
        take = Math.Clamp(take, 1, 100);
        var q =
            from e in _db.ArticuloEmbalajes.AsNoTracking()
            join a in _db.Articulos.AsNoTracking() on e.IDArticulo equals a.ID
            where e.IDEmpresa == idEmpresa && e.Activo && a.Activo
            select new { e, a };

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLowerInvariant();
            q = q.Where(x =>
                (x.a.SKU != null && x.a.SKU.ToLower().Contains(term))
                || x.a.DescripcionArticulo.ToLower().Contains(term));
        }

        return await q
            .OrderBy(x => x.a.DescripcionArticulo)
            .Take(take)
            .Select(x => new AdminEmbalajeOptionDto(
                x.e.ID, x.a.ID, x.a.SKU, x.a.DescripcionArticulo,
                x.e.DescripcionEmbalaje, x.e.PrecioDemo))
            .ToListAsync(ct);
    }

    public async Task<AdminListaPrecioDto> AutorizarAsync(long id, CancellationToken ct = default)
    {
        var l = await RequireListaEntityAsync(id, ct);
        if (l.Estado is not (1 or 0))
            throw new InvalidOperationException("Solo se autorizan listas ABIERTAS (o reabrir anuladas).");
        l.Estado = 2;
        l.Activo = true;
        l.UltModificacion = DateTime.UtcNow;
        l.UserName = "admin";
        await _db.SaveChangesAsync(ct);
        return (await GetAsync(id, ct))!;
    }

    public async Task<AdminListaPrecioDto> AnularAsync(long id, CancellationToken ct = default)
    {
        var l = await RequireListaEntityAsync(id, ct);
        if (l.Estado is not (1 or 2))
            throw new InvalidOperationException("Solo se anulan listas ABIERTAS o AUTORIZADAS.");
        l.Estado = 0;
        l.UltModificacion = DateTime.UtcNow;
        l.UserName = "admin";
        await _db.SaveChangesAsync(ct);
        return (await GetAsync(id, ct))!;
    }

    public async Task<AdminListaPrecioDto> CancelarAsync(long id, CancellationToken ct = default)
    {
        var l = await RequireListaEntityAsync(id, ct);
        if (l.Estado != 2)
            throw new InvalidOperationException("Solo se cancelan listas AUTORIZADAS.");
        l.Estado = 3;
        l.FechaFinVigencia = DateTime.UtcNow.Date;
        l.UltModificacion = DateTime.UtcNow;
        l.UserName = "admin";
        await _db.SaveChangesAsync(ct);
        return (await GetAsync(id, ct))!;
    }

    public async Task<int> CambioVigenciaAsync(CambioVigenciaListasRequest request, CancellationToken ct = default)
    {
        var idEmpresa = EmpresaId;
        if (request.Ids is null || request.Ids.Count == 0)
            throw new ArgumentException("Ids vacío.");
        var ids = request.Ids.Distinct().ToList();
        var rows = await _db.ListasPrecio
            .Where(l => l.IDEmpresa == idEmpresa && ids.Contains(l.ID) && l.Activo)
            .ToListAsync(ct);
        var now = DateTime.UtcNow;
        foreach (var l in rows)
        {
            if (request.FechaInicioVigencia.HasValue)
                l.FechaInicioVigencia = request.FechaInicioVigencia;
            if (request.FechaFinVigencia.HasValue)
                l.FechaFinVigencia = request.FechaFinVigencia;
            l.UltModificacion = now;
            l.UserName = "admin";
        }
        await _db.SaveChangesAsync(ct);
        return rows.Count;
    }

    private async Task<ListaPrecio> RequireListaEntityAsync(long id, CancellationToken ct)
    {
        var idEmpresa = EmpresaId;
        return await _db.ListasPrecio.FirstOrDefaultAsync(x => x.ID == id && x.IDEmpresa == idEmpresa, ct)
            ?? throw new InvalidOperationException("Lista de precio no encontrada.");
    }

    private async Task EnsureListaAsync(long idEmpresa, long idLista, CancellationToken ct)
    {
        var ok = await _db.ListasPrecio.AnyAsync(l => l.ID == idLista && l.IDEmpresa == idEmpresa, ct);
        if (!ok) throw new InvalidOperationException("Lista de precio no encontrada.");
    }

    private static void ValidateLista(UpsertListaPrecioRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Descripcion))
            throw new ArgumentException("Descripcion requerida.");
        var canal = NormalizeCanal(request.CanalEcommerce);
        if (canal is not ("B2C" or "B2B" or "BOTH"))
            throw new ArgumentException("CanalEcommerce debe ser B2C, B2B o BOTH.");
    }

    private static string NormalizeCanal(string? canal)
    {
        var c = (canal ?? "B2C").Trim().ToUpperInvariant();
        return c switch
        {
            "B2C" or "B2B" or "BOTH" => c,
            _ => c
        };
    }
}
