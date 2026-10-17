using Datamex.ECommerce.Application.Abstractions;
using Datamex.ECommerce.Application.Admin;
using Datamex.ECommerce.Domain.Entities;
using Datamex.ECommerce.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Datamex.ECommerce.Infrastructure.Admin;

public sealed class AdminUnidadesService : IAdminUnidadesService
{
    private readonly EcomDbContext _db;
    private readonly ITenantContext _tenant;

    public AdminUnidadesService(EcomDbContext db, ITenantContext tenant)
    {
        _db = db;
        _tenant = tenant;
    }

    private long EmpresaId => _tenant.IDEmpresa
        ?? throw new InvalidOperationException("Tenant no resuelto.");

    public async Task<IReadOnlyList<AdminUnidadMedidaDto>> ListUnidadesAsync(CancellationToken ct = default)
    {
        var idEmpresa = EmpresaId;
        return await _db.UnidadesMedidaEcommerce.AsNoTracking()
            .Where(u => u.IDEmpresa == idEmpresa)
            .OrderBy(u => u.Orden).ThenBy(u => u.Codigo)
            .Select(u => new AdminUnidadMedidaDto(
                u.ID, u.Codigo, u.Nombre, u.Simbolo, u.PermiteDecimales, u.IncrementoDefault, u.Orden, u.Activo))
            .ToListAsync(ct);
    }

    public async Task<AdminUnidadMedidaDto> UpsertUnidadAsync(
        long? id, UpsertUnidadMedidaRequest request, CancellationToken ct = default)
    {
        var idEmpresa = EmpresaId;
        var codigo = (request.Codigo ?? "").Trim().ToUpperInvariant();
        if (string.IsNullOrWhiteSpace(codigo) || string.IsNullOrWhiteSpace(request.Nombre))
            throw new ArgumentException("Código y nombre son requeridos.");

        UnidadMedidaEcommerce row;
        if (id is > 0)
        {
            row = await _db.UnidadesMedidaEcommerce.FirstOrDefaultAsync(u => u.ID == id && u.IDEmpresa == idEmpresa, ct)
                ?? throw new InvalidOperationException("Unidad no encontrada.");
        }
        else
        {
            var clash = await _db.UnidadesMedidaEcommerce.AnyAsync(u => u.IDEmpresa == idEmpresa && u.Codigo == codigo, ct);
            if (clash) throw new InvalidOperationException($"Ya existe la unidad {codigo}.");
            row = new UnidadMedidaEcommerce
            {
                IDEmpresa = idEmpresa,
                Activo = true,
                UltModificacion = DateTime.UtcNow,
                UserName = "admin"
            };
            _db.UnidadesMedidaEcommerce.Add(row);
        }

        row.Codigo = codigo;
        row.Nombre = request.Nombre.Trim();
        row.Simbolo = string.IsNullOrWhiteSpace(request.Simbolo) ? null : request.Simbolo.Trim();
        row.PermiteDecimales = request.PermiteDecimales;
        row.IncrementoDefault = request.IncrementoDefault > 0 ? request.IncrementoDefault : 1;
        row.Orden = request.Orden;
        row.Activo = request.Activo;
        row.UltModificacion = DateTime.UtcNow;
        row.UserName = "admin";
        await _db.SaveChangesAsync(ct);

        return new AdminUnidadMedidaDto(
            row.ID, row.Codigo, row.Nombre, row.Simbolo, row.PermiteDecimales, row.IncrementoDefault, row.Orden, row.Activo);
    }

    public async Task DeleteUnidadAsync(long id, CancellationToken ct = default)
    {
        var idEmpresa = EmpresaId;
        var row = await _db.UnidadesMedidaEcommerce.FirstOrDefaultAsync(u => u.ID == id && u.IDEmpresa == idEmpresa, ct)
            ?? throw new InvalidOperationException("Unidad no encontrada.");
        row.Activo = false;
        row.UltModificacion = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);
    }

    public async Task<IReadOnlyList<AdminProductEmbalajeDto>> ListEmbalajesAsync(long idArticulo, CancellationToken ct = default)
    {
        var idEmpresa = EmpresaId;
        await EnsureArticuloAsync(idEmpresa, idArticulo, ct);
        var rows = await _db.ArticuloEmbalajes.AsNoTracking()
            .Where(e => e.IDEmpresa == idEmpresa && e.IDArticulo == idArticulo)
            .OrderBy(e => e.CantidadxEmbalaje)
            .ToListAsync(ct);
        return rows.Select(MapEmb).ToList();
    }

    public async Task<AdminProductEmbalajeDto> UpsertEmbalajeAsync(
        long idArticulo, long? id, UpsertProductEmbalajeRequest request, CancellationToken ct = default)
    {
        var idEmpresa = EmpresaId;
        await EnsureArticuloAsync(idEmpresa, idArticulo, ct);
        var codigo = (request.CodigoUnidadMedida ?? "UN").Trim().ToUpperInvariant();
        if (request.CantidadPorEmbalaje <= 0)
            throw new ArgumentException("Cantidad por embalaje debe ser > 0.");

        ArticuloEmbalaje row;
        if (id is > 0)
        {
            row = await _db.ArticuloEmbalajes.FirstOrDefaultAsync(
                e => e.ID == id && e.IDEmpresa == idEmpresa && e.IDArticulo == idArticulo, ct)
                ?? throw new InvalidOperationException("Embalaje no encontrado.");
        }
        else
        {
            var nextIdEmb = ((await _db.ArticuloEmbalajes.AsNoTracking()
                .Where(e => e.IDEmpresa == idEmpresa && e.IDArticulo == idArticulo)
                .MaxAsync(e => (long?)e.IDEmbalaje, ct)) ?? 0) + 1;
            row = new ArticuloEmbalaje
            {
                IDEmpresa = idEmpresa,
                IDArticulo = idArticulo,
                IDEmbalaje = nextIdEmb,
                Activo = true,
                UltModificacion = DateTime.UtcNow,
                UserName = "admin"
            };
            _db.ArticuloEmbalajes.Add(row);
        }

        row.DescripcionEmbalaje = string.IsNullOrWhiteSpace(request.Descripcion) ? null : request.Descripcion.Trim();
        row.CantidadxEmbalaje = request.CantidadPorEmbalaje;
        row.CodigoBarras = string.IsNullOrWhiteSpace(request.CodigoBarras) ? null : request.CodigoBarras.Trim();
        row.CodigoUnidadMedida = codigo;
        row.PermiteFraccion = request.PermiteFraccion || codigo is "M" or "M2" or "KG" or "L";
        row.IncrementoVenta = request.IncrementoVenta > 0 ? request.IncrementoVenta : 1;
        row.CantidadMinimaVenta = request.CantidadMinimaVenta > 0 ? request.CantidadMinimaVenta : 1;
        row.EmbalajeParaFacturacion = request.ParaFacturacion;
        row.EmbalajeParaControlStock = request.ParaStock;
        row.Activo = request.Activo;
        row.PrecioDemo = request.PrecioDemo;
        row.UltModificacion = DateTime.UtcNow;
        row.UserName = "admin";
        await _db.SaveChangesAsync(ct);
        return MapEmb(row);
    }

    public async Task DeleteEmbalajeAsync(long idArticulo, long id, CancellationToken ct = default)
    {
        var idEmpresa = EmpresaId;
        var row = await _db.ArticuloEmbalajes.FirstOrDefaultAsync(
            e => e.ID == id && e.IDEmpresa == idEmpresa && e.IDArticulo == idArticulo, ct)
            ?? throw new InvalidOperationException("Embalaje no encontrado.");
        row.Activo = false;
        row.UltModificacion = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);
    }

    public async Task<IReadOnlyList<AdminCodigoBarraDto>> ListCodigosBarraAsync(
        long idArticuloEmbalaje, CancellationToken ct = default)
    {
        var idEmpresa = EmpresaId;
        await EnsureEmbalajeAsync(idEmpresa, idArticuloEmbalaje, ct);
        return await _db.ArticuloEmbalajeCodigosBarra.AsNoTracking()
            .Where(c => c.IDEmpresa == idEmpresa && c.IDArticuloEmbalaje == idArticuloEmbalaje)
            .OrderBy(c => c.ID)
            .Select(c => new AdminCodigoBarraDto(c.ID, c.IDArticuloEmbalaje, c.CodigoBarra, c.ParaSupermercado, c.Activo))
            .ToListAsync(ct);
    }

    public async Task<AdminCodigoBarraDto> UpsertCodigoBarraAsync(
        long idArticuloEmbalaje, long? id, UpsertCodigoBarraRequest request, CancellationToken ct = default)
    {
        var idEmpresa = EmpresaId;
        await EnsureEmbalajeAsync(idEmpresa, idArticuloEmbalaje, ct);
        var code = (request.CodigoBarra ?? "").Trim();
        if (string.IsNullOrWhiteSpace(code))
            throw new ArgumentException("Código de barras requerido.");

        ArticuloEmbalajeCodigoBarra row;
        if (id is > 0)
        {
            row = await _db.ArticuloEmbalajeCodigosBarra.FirstOrDefaultAsync(
                c => c.ID == id && c.IDEmpresa == idEmpresa && c.IDArticuloEmbalaje == idArticuloEmbalaje, ct)
                ?? throw new InvalidOperationException("Código de barras no encontrado.");
        }
        else
        {
            row = new ArticuloEmbalajeCodigoBarra
            {
                IDEmpresa = idEmpresa,
                IDArticuloEmbalaje = idArticuloEmbalaje,
                Activo = true,
                UserName = "admin"
            };
            _db.ArticuloEmbalajeCodigosBarra.Add(row);
        }

        row.CodigoBarra = code;
        row.ParaSupermercado = request.ParaSupermercado;
        row.Activo = request.Activo;
        row.UltModificacion = DateTime.UtcNow;
        row.UserName = "admin";
        await _db.SaveChangesAsync(ct);

        // Mantener campo plano del embalaje alineado al primer activo.
        var emb = await _db.ArticuloEmbalajes.FirstAsync(e => e.ID == idArticuloEmbalaje, ct);
        if (row.Activo)
            emb.CodigoBarras = code;
        emb.UltModificacion = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);

        return new AdminCodigoBarraDto(row.ID, row.IDArticuloEmbalaje, row.CodigoBarra, row.ParaSupermercado, row.Activo);
    }

    public async Task DeleteCodigoBarraAsync(long idArticuloEmbalaje, long id, CancellationToken ct = default)
    {
        var idEmpresa = EmpresaId;
        var row = await _db.ArticuloEmbalajeCodigosBarra.FirstOrDefaultAsync(
            c => c.ID == id && c.IDEmpresa == idEmpresa && c.IDArticuloEmbalaje == idArticuloEmbalaje, ct)
            ?? throw new InvalidOperationException("Código de barras no encontrado.");
        row.Activo = false;
        row.UltModificacion = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);
    }

    private async Task EnsureEmbalajeAsync(long idEmpresa, long idEmbalaje, CancellationToken ct)
    {
        var ok = await _db.ArticuloEmbalajes.AnyAsync(e => e.ID == idEmbalaje && e.IDEmpresa == idEmpresa, ct);
        if (!ok) throw new InvalidOperationException("Embalaje no encontrado.");
    }

    private async Task EnsureArticuloAsync(long idEmpresa, long idArticulo, CancellationToken ct)
    {
        var ok = await _db.Articulos.AnyAsync(a => a.ID == idArticulo && a.IDEmpresa == idEmpresa, ct);
        if (!ok) throw new InvalidOperationException("Artículo no encontrado.");
    }

    private static AdminProductEmbalajeDto MapEmb(ArticuloEmbalaje e) => new(
        e.ID, e.IDArticulo, e.DescripcionEmbalaje, e.CantidadxEmbalaje, e.CodigoBarras,
        e.CodigoUnidadMedida, e.PermiteFraccion, e.IncrementoVenta, e.CantidadMinimaVenta,
        e.EmbalajeParaFacturacion, e.EmbalajeParaControlStock, e.Activo, e.PrecioDemo);
}
