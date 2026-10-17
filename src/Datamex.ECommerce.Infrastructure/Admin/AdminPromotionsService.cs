using Datamex.ECommerce.Application.Abstractions;
using Datamex.ECommerce.Application.Admin;
using Datamex.ECommerce.Application.Media;
using Datamex.ECommerce.Domain.Entities;
using Datamex.ECommerce.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Datamex.ECommerce.Infrastructure.Admin;

public sealed class AdminPromotionsService : IAdminPromotionsService
{
    private readonly EcomDbContext _db;
    private readonly ITenantContext _tenant;
    private readonly IMediaStorage _media;

    public AdminPromotionsService(EcomDbContext db, ITenantContext tenant, IMediaStorage media)
    {
        _db = db;
        _tenant = tenant;
        _media = media;
    }

    private long EmpresaId => _tenant.IDEmpresa
        ?? throw new InvalidOperationException("Tenant no resuelto.");

    public async Task<IReadOnlyList<AdminPromotionListItemDto>> ListAsync(CancellationToken ct = default)
    {
        var idEmpresa = EmpresaId;
        var rows = await _db.GruposArticuloEcommerce.AsNoTracking()
            .Where(g => g.IDEmpresa == idEmpresa)
            .OrderBy(g => g.Orden).ThenBy(g => g.DescripcionGrupoArticulo)
            .Select(g => new AdminPromotionListItemDto(
                g.ID,
                g.CodGrupoArticulo,
                g.DescripcionGrupoArticulo,
                g.FechaInicioVigencia,
                g.FechaFinVigencia,
                g.TipoGrupo,
                g.Estado,
                g.Orden,
                g.Imagen,
                g.Activo,
                g.DetalleGrupoArticuloEcommerces.Count(d => d.Activo),
                g.UrlDestino,
                g.Subtitulo))
            .ToListAsync(ct);
        return rows;
    }

    public async Task<AdminPromotionDetailDto?> GetAsync(long id, CancellationToken ct = default)
    {
        var idEmpresa = EmpresaId;
        var g = await _db.GruposArticuloEcommerce.AsNoTracking()
            .FirstOrDefaultAsync(x => x.ID == id && x.IDEmpresa == idEmpresa, ct);
        if (g is null) return null;

        var arts = await (
            from d in _db.DetallesGrupoArticuloEcommerce.AsNoTracking()
            join a in _db.Articulos.AsNoTracking() on d.IDArticulo equals a.ID
            where d.IDEmpresa == idEmpresa && d.IDGrupoArticuloEcommerce == id && d.Activo
            select new AdminPromotionArticleDto(a.ID, a.SKU, a.DescripcionArticulo)
        ).ToListAsync(ct);

        return MapDetail(g, arts);
    }

    public async Task<AdminPromotionDetailDto> CreateAsync(UpsertPromotionRequest request, CancellationToken ct = default)
    {
        var idEmpresa = EmpresaId;
        Validate(request);
        var now = DateTime.UtcNow;
        var maxCod = await _db.GruposArticuloEcommerce.AsNoTracking()
            .Where(g => g.IDEmpresa == idEmpresa)
            .MaxAsync(g => (int?)g.CodGrupoArticulo, ct) ?? 0;

        var g = new GrupoArticuloEcommerce
        {
            IDEmpresa = idEmpresa,
            Activo = request.Activo,
            UltModificacion = now,
            UserName = "admin",
            CodGrupoArticulo = maxCod + 1,
            DescripcionGrupoArticulo = request.Nombre.Trim(),
            FechaInicioVigencia = request.FechaInicioVigencia,
            FechaFinVigencia = request.FechaFinVigencia,
            TipoGrupo = request.TipoGrupo,
            Estado = request.Estado,
            Orden = request.Orden,
            Imagen = string.IsNullOrWhiteSpace(request.Imagen) ? null : request.Imagen.Trim(),
            UrlDestino = string.IsNullOrWhiteSpace(request.UrlDestino) ? null : request.UrlDestino.Trim(),
            Subtitulo = string.IsNullOrWhiteSpace(request.Subtitulo) ? null : request.Subtitulo.Trim()
        };
        _db.GruposArticuloEcommerce.Add(g);
        await _db.SaveChangesAsync(ct);
        await ReplaceArticlesAsync(g.ID, request.ArticuloIds, now, ct);
        return (await GetAsync(g.ID, ct))!;
    }

    public async Task<AdminPromotionDetailDto> UpdateAsync(long id, UpsertPromotionRequest request, CancellationToken ct = default)
    {
        var idEmpresa = EmpresaId;
        Validate(request);
        var g = await _db.GruposArticuloEcommerce
            .FirstOrDefaultAsync(x => x.ID == id && x.IDEmpresa == idEmpresa, ct)
            ?? throw new InvalidOperationException("Promoción no encontrada.");

        var now = DateTime.UtcNow;
        g.DescripcionGrupoArticulo = request.Nombre.Trim();
        g.FechaInicioVigencia = request.FechaInicioVigencia;
        g.FechaFinVigencia = request.FechaFinVigencia;
        g.TipoGrupo = request.TipoGrupo;
        g.Estado = request.Estado;
        g.Orden = request.Orden;
        g.Imagen = string.IsNullOrWhiteSpace(request.Imagen) ? null : request.Imagen.Trim();
        g.UrlDestino = string.IsNullOrWhiteSpace(request.UrlDestino) ? null : request.UrlDestino.Trim();
        g.Subtitulo = string.IsNullOrWhiteSpace(request.Subtitulo) ? null : request.Subtitulo.Trim();
        g.Activo = request.Activo;
        g.UltModificacion = now;
        g.UserName = "admin";
        await _db.SaveChangesAsync(ct);
        await ReplaceArticlesAsync(g.ID, request.ArticuloIds, now, ct);
        return (await GetAsync(g.ID, ct))!;
    }

    public async Task DeleteAsync(long id, CancellationToken ct = default)
    {
        var idEmpresa = EmpresaId;
        var g = await _db.GruposArticuloEcommerce
            .FirstOrDefaultAsync(x => x.ID == id && x.IDEmpresa == idEmpresa, ct)
            ?? throw new InvalidOperationException("Promoción no encontrada.");
        g.Activo = false;
        g.Estado = 0;
        g.UltModificacion = DateTime.UtcNow;
        g.UserName = "admin";
        await _db.SaveChangesAsync(ct);
    }

    public async Task<PromoImageUploadResult> UploadImageAsync(
        Stream content,
        string contentType,
        string originalFileName,
        long? idPromocion = null,
        CancellationToken ct = default)
    {
        var idEmpresa = EmpresaId;
        var stored = await _media.SaveBrandingAssetAsync(
            idEmpresa, "promo", content, contentType, originalFileName, ct);

        if (idPromocion is > 0)
        {
            var g = await _db.GruposArticuloEcommerce
                .FirstOrDefaultAsync(x => x.ID == idPromocion && x.IDEmpresa == idEmpresa, ct)
                ?? throw new InvalidOperationException("Promoción no encontrada.");
            await _media.DeleteIfOwnedAsync(g.Imagen, ct);
            g.Imagen = stored.PublicUrl;
            g.UltModificacion = DateTime.UtcNow;
            g.UserName = "admin";
            await _db.SaveChangesAsync(ct);
            return new PromoImageUploadResult(stored.PublicUrl, g.ID);
        }

        return new PromoImageUploadResult(stored.PublicUrl);
    }

    private async Task ReplaceArticlesAsync(long grupoId, IReadOnlyList<long>? articuloIds, DateTime now, CancellationToken ct)
    {
        var idEmpresa = EmpresaId;
        var existing = await _db.DetallesGrupoArticuloEcommerce
            .Where(d => d.IDEmpresa == idEmpresa && d.IDGrupoArticuloEcommerce == grupoId)
            .ToListAsync(ct);

        foreach (var d in existing)
        {
            d.Activo = false;
            d.UltModificacion = now;
        }

        var ids = (articuloIds ?? Array.Empty<long>()).Distinct().ToList();
        if (ids.Count == 0)
        {
            await _db.SaveChangesAsync(ct);
            return;
        }

        var valid = await _db.Articulos.AsNoTracking()
            .Where(a => a.IDEmpresa == idEmpresa && ids.Contains(a.ID))
            .Select(a => a.ID)
            .ToListAsync(ct);

        foreach (var artId in valid)
        {
            var row = existing.FirstOrDefault(d => d.IDArticulo == artId);
            if (row is null)
            {
                _db.DetallesGrupoArticuloEcommerce.Add(new DetalleGrupoArticuloEcommerce
                {
                    IDEmpresa = idEmpresa,
                    IDGrupoArticuloEcommerce = grupoId,
                    IDArticulo = artId,
                    Activo = true,
                    UltModificacion = now,
                    UserName = "admin"
                });
            }
            else
            {
                row.Activo = true;
                row.UltModificacion = now;
                row.UserName = "admin";
            }
        }

        await _db.SaveChangesAsync(ct);
    }

    private static void Validate(UpsertPromotionRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Nombre))
            throw new ArgumentException("Nombre obligatorio.");
        if (request.FechaFinVigencia < request.FechaInicioVigencia)
            throw new ArgumentException("Vigencia inválida.");
    }

    private static AdminPromotionDetailDto MapDetail(GrupoArticuloEcommerce g, IReadOnlyList<AdminPromotionArticleDto> arts) =>
        new(g.ID, g.CodGrupoArticulo, g.DescripcionGrupoArticulo, g.FechaInicioVigencia, g.FechaFinVigencia,
            g.TipoGrupo, g.Estado, g.Orden, g.Imagen, g.Activo, arts, g.UrlDestino, g.Subtitulo);
}

public sealed class AdminShippingAdminService : IAdminShippingAdminService
{
    private readonly EcomDbContext _db;
    private readonly ITenantContext _tenant;

    public AdminShippingAdminService(EcomDbContext db, ITenantContext tenant)
    {
        _db = db;
        _tenant = tenant;
    }

    public async Task<IReadOnlyList<AdminTarifaDeliveryDto>> ListTarifasAsync(CancellationToken ct = default)
    {
        var idEmpresa = _tenant.IDEmpresa ?? throw new InvalidOperationException("Tenant no resuelto.");
        return await _db.TarifasDelivery.AsNoTracking()
            .Where(t => t.IDEmpresa == idEmpresa)
            .OrderBy(t => t.NombreCiudad).ThenBy(t => t.NombreBarrio)
            .Select(t => new AdminTarifaDeliveryDto(
                t.ID, t.IDCiudad, t.IDBarrio, t.NombreCiudad, t.NombreBarrio,
                t.ImporteDelivery, t.FechaInicioVigencia, t.FechaFinVigencia, t.Estado, t.Activo))
            .ToListAsync(ct);
    }

    public async Task<AdminTarifaDeliveryDto> CreateTarifaAsync(
        CreateTarifaDeliveryRequest request, CancellationToken ct = default)
    {
        var idEmpresa = _tenant.IDEmpresa ?? throw new InvalidOperationException("Tenant no resuelto.");
        if (request.ImporteDelivery < 0)
            throw new ArgumentException("ImporteDelivery inválido.");

        var ciudad = await _db.Ciudades.AsNoTracking()
            .FirstOrDefaultAsync(c => c.ID == request.IDCiudad, ct)
            ?? throw new InvalidOperationException("Ciudad no encontrada.");

        var barrio = await _db.Barrios.AsNoTracking()
            .FirstOrDefaultAsync(b => b.ID == request.IDBarrio, ct)
            ?? throw new InvalidOperationException("Barrio no encontrado.");

        var now = DateTime.UtcNow;
        var t = new TarifaDelivery
        {
            IDEmpresa = idEmpresa,
            Activo = true,
            UltModificacion = now,
            UserName = "admin",
            IDDivisionPolitica = ciudad.IDDivisionPolitica,
            IDCiudad = ciudad.ID,
            IDBarrio = barrio.ID,
            NombreCiudad = ciudad.DescripcionCiudad,
            NombreBarrio = barrio.DescripcionBarrio,
            ImporteDelivery = request.ImporteDelivery,
            FechaInicioVigencia = now.Date,
            FechaFinVigencia = now.Date.AddYears(1),
            Estado = 0,
            IDAgencia = request.IDAgencia
        };

        _db.TarifasDelivery.Add(t);
        await _db.SaveChangesAsync(ct);
        return MapTarifa(t);
    }

    public async Task<AdminTarifaDeliveryDto> UpdateTarifaAsync(long id, UpdateTarifaDeliveryRequest request, CancellationToken ct = default)
    {
        var idEmpresa = _tenant.IDEmpresa ?? throw new InvalidOperationException("Tenant no resuelto.");
        var t = await _db.TarifasDelivery.FirstOrDefaultAsync(x => x.ID == id && x.IDEmpresa == idEmpresa, ct)
                ?? throw new InvalidOperationException("Tarifa no encontrada.");

        // Solo editar importe/vigencia si está pendiente (ABIERTO)
        if (t.Estado != 0 && (request.ImporteDelivery.HasValue || request.FechaInicioVigencia.HasValue || request.FechaFinVigencia.HasValue))
            throw new InvalidOperationException("Solo se pueden editar tarifas en estado ABIERTO (pendiente).");

        if (request.ImporteDelivery.HasValue) t.ImporteDelivery = request.ImporteDelivery.Value;
        if (request.FechaInicioVigencia.HasValue)
            t.FechaInicioVigencia = ToUtcDate(request.FechaInicioVigencia.Value);
        if (request.FechaFinVigencia.HasValue)
            t.FechaFinVigencia = ToUtcDate(request.FechaFinVigencia.Value);
        if (request.Estado.HasValue) t.Estado = request.Estado.Value;
        if (request.Activo.HasValue) t.Activo = request.Activo.Value;
        t.UltModificacion = DateTime.UtcNow;
        t.UserName = "admin";
        await _db.SaveChangesAsync(ct);

        return MapTarifa(t);
    }

    private static DateTime ToUtcDate(DateTime value)
    {
        var d = value.Date;
        return d.Kind switch
        {
            DateTimeKind.Utc => d,
            DateTimeKind.Local => d.ToUniversalTime().Date,
            _ => DateTime.SpecifyKind(d, DateTimeKind.Utc)
        };
    }

    public Task<BulkEstadoResult> AutorizarTarifasAsync(IReadOnlyList<long> ids, CancellationToken ct = default)
        => TransitionTarifasAsync(ids, fromEstado: 0, toEstado: 1, ct);

    public Task<BulkEstadoResult> AnularTarifasAsync(IReadOnlyList<long> ids, CancellationToken ct = default)
        => TransitionTarifasAsync(ids, fromEstado: 0, toEstado: 2, ct);

    public async Task<BulkEstadoResult> CancelarTarifasAsync(IReadOnlyList<long> ids, CancellationToken ct = default)
    {
        var idEmpresa = _tenant.IDEmpresa ?? throw new InvalidOperationException("Tenant no resuelto.");
        if (ids is null || ids.Count == 0)
            throw new ArgumentException("Ids vacío.");

        var set = ids.Distinct().ToList();
        var rows = await _db.TarifasDelivery
            .Where(t => t.IDEmpresa == idEmpresa && set.Contains(t.ID) && t.Estado == 1)
            .ToListAsync(ct);

        var now = DateTime.UtcNow;
        foreach (var t in rows)
        {
            t.Estado = 3; // CANCELADO
            t.FechaFinVigencia = now.Date;
            t.Activo = true;
            t.UltModificacion = now;
            t.UserName = "admin";
        }

        await _db.SaveChangesAsync(ct);
        return new BulkEstadoResult(rows.Count);
    }

    private async Task<BulkEstadoResult> TransitionTarifasAsync(
        IReadOnlyList<long> ids, int fromEstado, int toEstado, CancellationToken ct)
    {
        var idEmpresa = _tenant.IDEmpresa ?? throw new InvalidOperationException("Tenant no resuelto.");
        if (ids is null || ids.Count == 0)
            throw new ArgumentException("Ids vacío.");

        var set = ids.Distinct().ToList();
        var rows = await _db.TarifasDelivery
            .Where(t => t.IDEmpresa == idEmpresa && set.Contains(t.ID) && t.Estado == fromEstado)
            .ToListAsync(ct);

        var now = DateTime.UtcNow;
        foreach (var t in rows)
        {
            t.Estado = toEstado;
            t.Activo = true;
            t.UltModificacion = now;
            t.UserName = "admin";
        }

        await _db.SaveChangesAsync(ct);
        return new BulkEstadoResult(rows.Count);
    }

    private static AdminTarifaDeliveryDto MapTarifa(Domain.Entities.TarifaDelivery t) =>
        new(t.ID, t.IDCiudad, t.IDBarrio, t.NombreCiudad, t.NombreBarrio,
            t.ImporteDelivery, t.FechaInicioVigencia, t.FechaFinVigencia, t.Estado, t.Activo);
}
