using Datamex.ECommerce.Application.Abstractions;
using Datamex.ECommerce.Application.Admin;
using Datamex.ECommerce.Domain.Entities;
using Datamex.ECommerce.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Datamex.ECommerce.Infrastructure.Admin;

public sealed class AdminParametrosProductoService : IAdminParametrosProductoService
{
    private readonly EcomDbContext _db;
    private readonly ITenantContext _tenant;

    public AdminParametrosProductoService(EcomDbContext db, ITenantContext tenant)
    {
        _db = db;
        _tenant = tenant;
    }

    public async Task<IReadOnlyList<CatalogoParametroDto>> ListAsync(
        string tipo, bool incluirInactivos = false, CancellationToken ct = default)
    {
        var idEmpresa = RequireTenant();
        var t = NormalizeTipo(tipo);
        var q = _db.CatalogoParametros.AsNoTracking()
            .Where(p => p.IDEmpresa == idEmpresa && p.Tipo == t);
        if (!incluirInactivos)
            q = q.Where(p => p.Activo);

        return await q.OrderBy(p => p.Codigo).ThenBy(p => p.Descripcion)
            .Select(p => new CatalogoParametroDto(p.ID, p.Tipo, p.Codigo, p.Descripcion, p.Activo))
            .ToListAsync(ct);
    }

    public async Task<CatalogoParametroDto> CreateAsync(
        UpsertCatalogoParametroRequest request, CancellationToken ct = default)
    {
        var idEmpresa = RequireTenant();
        var tipo = NormalizeTipo(request.Tipo);
        var desc = RequireDesc(request.Descripcion);

        var codigo = request.Codigo ?? await NextCodigoAsync(idEmpresa, tipo, ct);
        if (await _db.CatalogoParametros.AnyAsync(p =>
                p.IDEmpresa == idEmpresa && p.Tipo == tipo && p.Codigo == codigo, ct))
            throw new InvalidOperationException($"Ya existe código {codigo} para {tipo}.");

        var row = new CatalogoParametro
        {
            IDEmpresa = idEmpresa,
            Tipo = tipo,
            Codigo = codigo,
            Descripcion = desc,
            Activo = request.Activo,
            UltModificacion = DateTime.UtcNow,
            UserName = "admin"
        };
        _db.CatalogoParametros.Add(row);
        await _db.SaveChangesAsync(ct);
        return new CatalogoParametroDto(row.ID, row.Tipo, row.Codigo, row.Descripcion, row.Activo);
    }

    public async Task<CatalogoParametroDto> UpdateAsync(
        long id, UpsertCatalogoParametroRequest request, CancellationToken ct = default)
    {
        var idEmpresa = RequireTenant();
        var row = await _db.CatalogoParametros
                .FirstOrDefaultAsync(p => p.ID == id && p.IDEmpresa == idEmpresa, ct)
            ?? throw new InvalidOperationException("Parámetro no encontrado.");

        var tipo = NormalizeTipo(request.Tipo);
        if (tipo != row.Tipo)
            throw new InvalidOperationException("No se puede cambiar el tipo del parámetro.");

        var desc = RequireDesc(request.Descripcion);
        if (request.Codigo is int c && c != row.Codigo)
        {
            if (await _db.CatalogoParametros.AnyAsync(p =>
                    p.IDEmpresa == idEmpresa && p.Tipo == tipo && p.Codigo == c && p.ID != id, ct))
                throw new InvalidOperationException($"Ya existe código {c} para {tipo}.");
            row.Codigo = c;
        }

        row.Descripcion = desc;
        row.Activo = request.Activo;
        row.UltModificacion = DateTime.UtcNow;
        row.UserName = "admin";
        await _db.SaveChangesAsync(ct);

        // Sync nombres denormalizados en artículos
        if (tipo == CatalogoParametroTipos.Marca)
        {
            await _db.Articulos.Where(a => a.IDEmpresa == idEmpresa && a.IDMarca == id)
                .ExecuteUpdateAsync(s => s.SetProperty(a => a.NombreMarca, desc), ct);
        }
        else if (tipo == CatalogoParametroTipos.Linea)
        {
            await _db.Articulos.Where(a => a.IDEmpresa == idEmpresa && a.IDLinea == id)
                .ExecuteUpdateAsync(s => s.SetProperty(a => a.NombreLinea, desc), ct);
        }
        else if (tipo == CatalogoParametroTipos.Tipo)
        {
            await _db.Articulos.Where(a => a.IDEmpresa == idEmpresa && a.IDTipoArticulo == id)
                .ExecuteUpdateAsync(s => s.SetProperty(a => a.NombreTipoArticulo, desc), ct);
        }
        else if (tipo == CatalogoParametroTipos.Proveedor)
        {
            await _db.Articulos.Where(a => a.IDEmpresa == idEmpresa && a.IDProveedor == id)
                .ExecuteUpdateAsync(s => s.SetProperty(a => a.NombreProveedor, desc), ct);
        }
        else if (tipo == CatalogoParametroTipos.Pais)
        {
            await _db.Articulos.Where(a => a.IDEmpresa == idEmpresa && a.IDPais == id)
                .ExecuteUpdateAsync(s => s.SetProperty(a => a.NombrePais, desc), ct);
        }
        else if (tipo == CatalogoParametroTipos.Regimen)
        {
            await _db.Articulos.Where(a => a.IDEmpresa == idEmpresa && a.IDRegimenImpositivo == id)
                .ExecuteUpdateAsync(s => s.SetProperty(a => a.NombreRegimenImpositivo, desc), ct);
        }

        return new CatalogoParametroDto(row.ID, row.Tipo, row.Codigo, row.Descripcion, row.Activo);
    }

    public async Task DeleteAsync(long id, CancellationToken ct = default)
    {
        var idEmpresa = RequireTenant();
        var row = await _db.CatalogoParametros
                .FirstOrDefaultAsync(p => p.ID == id && p.IDEmpresa == idEmpresa, ct)
            ?? throw new InvalidOperationException("Parámetro no encontrado.");

        row.Activo = false;
        row.UltModificacion = DateTime.UtcNow;
        row.UserName = "admin";
        await _db.SaveChangesAsync(ct);
    }

    private async Task<int> NextCodigoAsync(long idEmpresa, string tipo, CancellationToken ct)
    {
        var max = await _db.CatalogoParametros.AsNoTracking()
            .Where(p => p.IDEmpresa == idEmpresa && p.Tipo == tipo)
            .MaxAsync(p => (int?)p.Codigo, ct);
        return (max ?? 0) + 1;
    }

    private static string NormalizeTipo(string tipo)
    {
        var t = (tipo ?? "").Trim().ToUpperInvariant();
        if (!CatalogoParametroTipos.IsValid(t))
            throw new ArgumentException(
                $"Tipo inválido. Use: {string.Join(", ", CatalogoParametroTipos.All)}");
        return t;
    }

    private static string RequireDesc(string? desc)
    {
        if (string.IsNullOrWhiteSpace(desc))
            throw new ArgumentException("Descripción requerida.");
        var d = desc.Trim();
        if (d.Length > 100)
            throw new ArgumentException("Descripción máx. 100 caracteres.");
        return d;
    }

    private long RequireTenant()
    {
        if (!_tenant.IsResolved || !_tenant.IDEmpresa.HasValue)
            throw new InvalidOperationException("Tenant no resuelto.");
        return _tenant.IDEmpresa.Value;
    }
}
