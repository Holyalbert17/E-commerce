using Datamex.ECommerce.Application.Abstractions;
using Datamex.ECommerce.Application.Reviews;
using Datamex.ECommerce.Domain.Entities;
using Datamex.ECommerce.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Datamex.ECommerce.Infrastructure.Reviews;

public sealed class ReviewService : IReviewService
{
    private const int EstadoCarritoCerradoMin = 1;

    private readonly EcomDbContext _db;
    private readonly ITenantContext _tenant;

    public ReviewService(EcomDbContext db, ITenantContext tenant)
    {
        _db = db;
        _tenant = tenant;
    }

    public async Task<ResumenCalificacionDto> GetResumenAsync(long idArticulo, CancellationToken ct = default)
    {
        var idEmpresa = RequireTenant();
        var scores = await _db.ResenasArticuloEcommerce.AsNoTracking()
            .Where(r => r.IDEmpresa == idEmpresa && r.IDArticulo == idArticulo && r.Activo && r.Aprobado)
            .Select(r => r.Puntuacion)
            .ToListAsync(ct);

        return BuildResumen(idArticulo, scores);
    }

    public async Task<IReadOnlyList<ResenaDto>> ListByProductAsync(long idArticulo, int take = 50, CancellationToken ct = default)
    {
        var idEmpresa = RequireTenant();
        var uid = _tenant.IDUsuarioEcommerce;

        var rows = await _db.ResenasArticuloEcommerce.AsNoTracking()
            .Where(r => r.IDEmpresa == idEmpresa && r.IDArticulo == idArticulo && r.Activo && r.Aprobado)
            .OrderByDescending(r => r.FechaResena)
            .Take(Math.Clamp(take, 1, 100))
            .ToListAsync(ct);

        var userIds = rows.Select(r => r.IDUsuarioEcommerce).Distinct().ToList();
        var names = await _db.UsuariosEcommerce.AsNoTracking()
            .Where(u => userIds.Contains(u.ID))
            .Select(u => new { u.ID, u.Nombre, u.Apellido })
            .ToDictionaryAsync(x => x.ID, x => $"{x.Nombre} {x.Apellido}".Trim(), ct);

        return rows.Select(r => new ResenaDto(
            r.ID, r.IDArticulo, MaskEmail(r.Email), r.CanalEcommerce, r.Puntuacion,
            r.Titulo, r.Comentario, r.FechaResena,
            names.GetValueOrDefault(r.IDUsuarioEcommerce),
            uid.HasValue && uid.Value == r.IDUsuarioEcommerce)).ToList();
    }

    public async Task<IReadOnlyList<ProductoElegibleResenaDto>> ListElegiblesAsync(long idUsuario, CancellationToken ct = default)
    {
        var idEmpresa = RequireTenant();
        var cuentaIds = await GetCuentaIdsAsync(idEmpresa, idUsuario, ct);
        if (cuentaIds.Count == 0)
            return Array.Empty<ProductoElegibleResenaDto>();

        var carts = await _db.CarritosCompra.AsNoTracking()
            .Where(c => c.IDEmpresa == idEmpresa
                        && c.Estado >= EstadoCarritoCerradoMin
                        && c.IDCuenta != null
                        && cuentaIds.Contains(c.IDCuenta.Value))
            .OrderByDescending(c => c.FechaPedido)
            .Take(40)
            .Select(c => new { c.ID, c.NroPedidoCarritoCompra, c.IDPedidoMovil })
            .ToListAsync(ct);

        if (carts.Count == 0)
            return Array.Empty<ProductoElegibleResenaDto>();

        var cartIds = carts.Select(c => c.ID).ToList();
        var lines = await _db.DetallesCarritoCompra.AsNoTracking()
            .Where(d => cartIds.Contains(d.IDCarritoCompra) && d.Activo)
            .Select(d => new { d.IDCarritoCompra, d.IDArticulo })
            .ToListAsync(ct);

        var artIds = lines.Select(l => l.IDArticulo).Distinct().ToList();
        var ya = await _db.ResenasArticuloEcommerce.AsNoTracking()
            .Where(r => r.IDEmpresa == idEmpresa && r.IDUsuarioEcommerce == idUsuario && artIds.Contains(r.IDArticulo) && r.Activo)
            .Select(r => r.IDArticulo)
            .ToListAsync(ct);
        var yaSet = ya.ToHashSet();

        var arts = await _db.Articulos.AsNoTracking()
            .Where(a => artIds.Contains(a.ID))
            .Select(a => new { a.ID, a.SKU, a.DescripcionArticulo })
            .ToDictionaryAsync(a => a.ID, ct);

        var imgs = await _db.ArticuloImagenes.AsNoTracking()
            .Where(i => artIds.Contains(i.IDArticulo) && i.Activo)
            .OrderByDescending(i => i.EsPrincipal)
            .ThenBy(i => i.Orden)
            .Select(i => new { i.IDArticulo, i.Url })
            .ToListAsync(ct);
        var imgMap = imgs.GroupBy(g => g.IDArticulo).ToDictionary(g => g.Key, g => g.First().Url);

        var cartMap = carts.ToDictionary(c => c.ID);
        var result = new List<ProductoElegibleResenaDto>();
        var seen = new HashSet<long>();

        foreach (var line in lines)
        {
            if (!seen.Add(line.IDArticulo) || !arts.TryGetValue(line.IDArticulo, out var a))
                continue;
            var cart = cartMap[line.IDCarritoCompra];
            imgMap.TryGetValue(a.ID, out var img);
            result.Add(new ProductoElegibleResenaDto(
                a.ID, a.DescripcionArticulo, a.SKU, img,
                cart.IDPedidoMovil, cart.ID, cart.NroPedidoCarritoCompra,
                yaSet.Contains(a.ID)));
        }

        return result;
    }

    public async Task<ResenaDto> CreateAsync(long idUsuario, CrearResenaRequest request, CancellationToken ct = default)
    {
        var idEmpresa = RequireTenant();
        if (request.Puntuacion is < 1 or > 5)
            throw new ArgumentException("La puntuación debe ser entre 1 y 5.");

        var user = await _db.UsuariosEcommerce
            .Include(u => u.UsuarioEcommerceCuentas)
            .FirstOrDefaultAsync(u => u.ID == idUsuario && u.IDEmpresa == idEmpresa, ct)
            ?? throw new InvalidOperationException("Usuario no encontrado.");

        var email = user.Email?.Trim() ?? "";
        if (string.IsNullOrWhiteSpace(email))
            throw new InvalidOperationException("Tu cuenta no tiene email. Completá el perfil para reseñar.");

        var cuentaIds = user.UsuarioEcommerceCuentas.Where(c => c.Activo).Select(c => c.IDCuenta).ToList();
        var bought = await _db.DetallesCarritoCompra.AsNoTracking()
            .Where(d => d.IDEmpresa == idEmpresa && d.IDArticulo == request.IdArticulo && d.Activo)
            .Join(_db.CarritosCompra.AsNoTracking(),
                d => d.IDCarritoCompra,
                c => c.ID,
                (d, c) => c)
            .AnyAsync(c => c.Estado >= EstadoCarritoCerradoMin
                           && c.IDCuenta != null
                           && cuentaIds.Contains(c.IDCuenta.Value), ct);

        if (!bought)
            throw new InvalidOperationException("Solo podés reseñar productos que hayas comprado con esta cuenta.");

        var existing = await _db.ResenasArticuloEcommerce
            .FirstOrDefaultAsync(r => r.IDEmpresa == idEmpresa
                                      && r.IDUsuarioEcommerce == idUsuario
                                      && r.IDArticulo == request.IdArticulo
                                      && r.Activo, ct);
        if (existing is not null)
            throw new InvalidOperationException("Ya dejaste una reseña para este producto.");

        var cuenta = user.UsuarioEcommerceCuentas.OrderByDescending(c => c.EsPrincipal).FirstOrDefault();
        var entity = new ResenaArticuloEcommerce
        {
            IDEmpresa = idEmpresa,
            IDArticulo = request.IdArticulo,
            IDUsuarioEcommerce = idUsuario,
            IDCuenta = cuenta?.IDCuenta,
            IDPedidoMovil = request.IdPedidoMovil,
            IDCarritoCompra = request.IdCarritoCompra,
            Email = email,
            CanalEcommerce = user.CanalPreferido,
            Puntuacion = request.Puntuacion,
            Titulo = Truncate(request.Titulo, 120),
            Comentario = Truncate(request.Comentario, 2000),
            Aprobado = true,
            FechaResena = DateTime.UtcNow,
            Activo = true,
            UltModificacion = DateTime.UtcNow,
            UserName = email
        };
        _db.ResenasArticuloEcommerce.Add(entity);
        await _db.SaveChangesAsync(ct);

        return new ResenaDto(
            entity.ID, entity.IDArticulo, MaskEmail(email), entity.CanalEcommerce,
            entity.Puntuacion, entity.Titulo, entity.Comentario, entity.FechaResena,
            $"{user.Nombre} {user.Apellido}".Trim(), true);
    }

    public async Task<IReadOnlyList<ResenaDto>> AdminListPendientesAsync(CancellationToken ct = default)
    {
        var idEmpresa = RequireTenant();
        var rows = await _db.ResenasArticuloEcommerce.AsNoTracking()
            .Where(r => r.IDEmpresa == idEmpresa && r.Activo)
            .OrderByDescending(r => r.FechaResena)
            .Take(200)
            .ToListAsync(ct);

        return rows.Select(r => new ResenaDto(
            r.ID, r.IDArticulo, r.Email, r.CanalEcommerce, r.Puntuacion,
            r.Titulo, r.Comentario, r.FechaResena, null, false)).ToList();
    }

    public async Task AprobarAsync(long idResena, bool aprobar, CancellationToken ct = default)
    {
        var idEmpresa = RequireTenant();
        var r = await _db.ResenasArticuloEcommerce
            .FirstOrDefaultAsync(x => x.ID == idResena && x.IDEmpresa == idEmpresa, ct)
            ?? throw new InvalidOperationException("Reseña no encontrada.");
        r.Aprobado = aprobar;
        r.Activo = aprobar || r.Activo;
        if (!aprobar)
            r.Activo = false;
        r.UltModificacion = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);
    }

    private async Task<List<long>> GetCuentaIdsAsync(long idEmpresa, long idUsuario, CancellationToken ct) =>
        await _db.UsuariosEcommerceCuenta.AsNoTracking()
            .Where(c => c.IDEmpresa == idEmpresa && c.IDUsuarioEcommerce == idUsuario && c.Activo)
            .Select(c => c.IDCuenta)
            .ToListAsync(ct);

    private static ResumenCalificacionDto BuildResumen(long idArticulo, List<int> scores)
    {
        if (scores.Count == 0)
            return new ResumenCalificacionDto(idArticulo, 0, 0, 0, 0, 0, 0, 0);
        return new ResumenCalificacionDto(
            idArticulo,
            Math.Round(scores.Average(), 2),
            scores.Count,
            scores.Count(s => s == 5),
            scores.Count(s => s == 4),
            scores.Count(s => s == 3),
            scores.Count(s => s == 2),
            scores.Count(s => s == 1));
    }

    private static string MaskEmail(string email)
    {
        if (string.IsNullOrWhiteSpace(email) || !email.Contains('@'))
            return "cliente";
        var parts = email.Split('@');
        var local = parts[0];
        var masked = local.Length <= 2 ? local[0] + "*" : local[..2] + new string('*', Math.Min(4, local.Length - 2));
        return $"{masked}@{parts[1]}";
    }

    private static string? Truncate(string? v, int max) =>
        string.IsNullOrWhiteSpace(v) ? null : (v.Trim().Length <= max ? v.Trim() : v.Trim()[..max]);

    private long RequireTenant()
    {
        if (!_tenant.IsResolved || !_tenant.IDEmpresa.HasValue)
            throw new InvalidOperationException("Tenant no resuelto.");
        return _tenant.IDEmpresa.Value;
    }
}
