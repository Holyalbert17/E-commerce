using Datamex.ECommerce.Application.Abstractions;
using Datamex.ECommerce.Application.Favorites;
using Datamex.ECommerce.Application.Pricing;
using Datamex.ECommerce.Domain.Entities;
using Datamex.ECommerce.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Datamex.ECommerce.Infrastructure.Favorites;

public sealed class FavoriteService : IFavoriteService
{
    private readonly EcomDbContext _db;
    private readonly ITenantContext _tenant;
    private readonly IPricingService _pricing;

    public FavoriteService(EcomDbContext db, ITenantContext tenant, IPricingService pricing)
    {
        _db = db;
        _tenant = tenant;
        _pricing = pricing;
    }

    public async Task<IReadOnlyList<FavoritoSeccionDto>> ListSeccionesAsync(
        long idUsuario, string? canal = null, CancellationToken ct = default)
    {
        var idEmpresa = RequireTenant();
        await EnsureDefaultSectionAsync(idUsuario, ct);

        var q = _db.ListasFavoritoEcommerce.AsNoTracking()
            .Include(l => l.Detalles.Where(d => d.Activo))
            .Where(l => l.IDEmpresa == idEmpresa && l.IDUsuarioEcommerce == idUsuario && l.Activo);

        if (!string.IsNullOrWhiteSpace(canal))
        {
            var c = NormalizeCanal(canal);
            q = q.Where(l => l.CanalEcommerce == c);
        }

        var listas = await q.OrderBy(l => l.Orden).ThenBy(l => l.NombreLista).ToListAsync(ct);
        return await MapListasAsync(listas, ct);
    }

    public async Task<FavoritoSeccionDto> CrearSeccionAsync(
        long idUsuario, CrearSeccionFavoritoRequest request, CancellationToken ct = default)
    {
        var idEmpresa = RequireTenant();
        var user = await RequireUserAsync(idEmpresa, idUsuario, ct);
        var nombre = (request.NombreLista ?? "").Trim();
        if (nombre.Length < 2)
            throw new ArgumentException("El nombre de la sección debe tener al menos 2 caracteres.");

        var canal = NormalizeCanal(request.Canal ?? user.CanalPreferido);
        var cuenta = user.UsuarioEcommerceCuentas.OrderByDescending(c => c.EsPrincipal).FirstOrDefault();
        var maxOrden = await _db.ListasFavoritoEcommerce
            .Where(l => l.IDEmpresa == idEmpresa && l.IDUsuarioEcommerce == idUsuario)
            .Select(l => (int?)l.Orden).MaxAsync(ct) ?? 0;

        var entity = new ListaFavoritoEcommerce
        {
            IDEmpresa = idEmpresa,
            IDUsuarioEcommerce = idUsuario,
            IDCuenta = cuenta?.IDCuenta,
            Email = user.Email ?? user.Usuario,
            NombreLista = Truncate(nombre, 80)!,
            CanalEcommerce = canal,
            Orden = maxOrden + 1,
            Color = Truncate(request.Color, 20),
            Activo = true,
            UltModificacion = DateTime.UtcNow,
            UserName = user.Email ?? user.Usuario
        };
        _db.ListasFavoritoEcommerce.Add(entity);
        await _db.SaveChangesAsync(ct);
        return (await MapListasAsync(new[] { entity }, ct))[0];
    }

    public async Task<FavoritoSeccionDto> RenombrarSeccionAsync(
        long idUsuario, long idLista, RenombrarSeccionRequest request, CancellationToken ct = default)
    {
        var idEmpresa = RequireTenant();
        var lista = await GetOwnedListaAsync(idEmpresa, idUsuario, idLista, ct);
        var nombre = (request.NombreLista ?? "").Trim();
        if (nombre.Length < 2)
            throw new ArgumentException("Nombre inválido.");
        lista.NombreLista = Truncate(nombre, 80)!;
        if (request.Color is not null)
            lista.Color = Truncate(request.Color, 20);
        lista.UltModificacion = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);
        await _db.Entry(lista).Collection(l => l.Detalles).LoadAsync(ct);
        return (await MapListasAsync(new[] { lista }, ct))[0];
    }

    public async Task EliminarSeccionAsync(long idUsuario, long idLista, CancellationToken ct = default)
    {
        var idEmpresa = RequireTenant();
        var lista = await GetOwnedListaAsync(idEmpresa, idUsuario, idLista, ct);
        lista.Activo = false;
        lista.UltModificacion = DateTime.UtcNow;
        foreach (var d in await _db.DetallesListaFavoritoEcommerce
                     .Where(x => x.IDListaFavoritoEcommerce == idLista).ToListAsync(ct))
        {
            d.Activo = false;
            d.UltModificacion = DateTime.UtcNow;
        }
        await _db.SaveChangesAsync(ct);
    }

    public async Task<FavoritoSeccionDto> AgregarAsync(
        long idUsuario, AgregarFavoritoRequest request, CancellationToken ct = default)
    {
        var idEmpresa = RequireTenant();
        var user = await RequireUserAsync(idEmpresa, idUsuario, ct);

        var articuloExists = await _db.Articulos.AsNoTracking()
            .AnyAsync(a => a.ID == request.IdArticulo && a.IDEmpresa == idEmpresa && a.Activo, ct);
        if (!articuloExists)
            throw new InvalidOperationException("Producto no encontrado.");

        ListaFavoritoEcommerce lista;
        if (request.IdLista.HasValue)
        {
            lista = await GetOwnedListaAsync(idEmpresa, idUsuario, request.IdLista.Value, ct);
        }
        else if (!string.IsNullOrWhiteSpace(request.NombreLista))
        {
            var created = await CrearSeccionAsync(idUsuario, new CrearSeccionFavoritoRequest(request.NombreLista!), ct);
            lista = await GetOwnedListaAsync(idEmpresa, idUsuario, created.Id, ct);
        }
        else
        {
            lista = await EnsureDefaultSectionAsync(idUsuario, ct);
        }

        var existing = await _db.DetallesListaFavoritoEcommerce
            .FirstOrDefaultAsync(d => d.IDListaFavoritoEcommerce == lista.ID
                                      && d.IDArticulo == request.IdArticulo
                                      && d.Activo, ct);
        if (existing is null)
        {
            _db.DetallesListaFavoritoEcommerce.Add(new DetalleListaFavoritoEcommerce
            {
                IDEmpresa = idEmpresa,
                IDListaFavoritoEcommerce = lista.ID,
                IDArticulo = request.IdArticulo,
                FechaAgregado = DateTime.UtcNow,
                Nota = Truncate(request.Nota, 200),
                Activo = true,
                UltModificacion = DateTime.UtcNow,
                UserName = user.Email ?? user.Usuario
            });
            await _db.SaveChangesAsync(ct);
        }

        await _db.Entry(lista).Collection(l => l.Detalles).Query().Where(d => d.Activo).LoadAsync(ct);
        return (await MapListasAsync(new[] { lista }, ct))[0];
    }

    public async Task QuitarAsync(long idUsuario, long idDetalle, CancellationToken ct = default)
    {
        var idEmpresa = RequireTenant();
        var detalle = await _db.DetallesListaFavoritoEcommerce
            .Include(d => d.ListaFavoritoEcommerce)
            .FirstOrDefaultAsync(d => d.ID == idDetalle && d.IDEmpresa == idEmpresa, ct)
            ?? throw new InvalidOperationException("Ítem no encontrado.");
        if (detalle.ListaFavoritoEcommerce?.IDUsuarioEcommerce != idUsuario)
            throw new InvalidOperationException("No autorizado.");
        detalle.Activo = false;
        detalle.UltModificacion = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);
    }

    public async Task QuitarPorArticuloAsync(long idUsuario, long idArticulo, CancellationToken ct = default)
    {
        var idEmpresa = RequireTenant();
        var rows = await _db.DetallesListaFavoritoEcommerce
            .Include(d => d.ListaFavoritoEcommerce)
            .Where(d => d.IDEmpresa == idEmpresa
                        && d.IDArticulo == idArticulo
                        && d.Activo
                        && d.ListaFavoritoEcommerce!.IDUsuarioEcommerce == idUsuario
                        && d.ListaFavoritoEcommerce.Activo)
            .ToListAsync(ct);
        if (rows.Count == 0)
            return;
        var now = DateTime.UtcNow;
        foreach (var d in rows)
        {
            d.Activo = false;
            d.UltModificacion = now;
        }
        await _db.SaveChangesAsync(ct);
    }

    public async Task<bool> EstaEnFavoritosAsync(long idUsuario, long idArticulo, CancellationToken ct = default)
    {
        var idEmpresa = RequireTenant();
        return await _db.DetallesListaFavoritoEcommerce.AsNoTracking()
            .AnyAsync(d => d.IDEmpresa == idEmpresa
                           && d.IDArticulo == idArticulo
                           && d.Activo
                           && d.ListaFavoritoEcommerce!.IDUsuarioEcommerce == idUsuario
                           && d.ListaFavoritoEcommerce.Activo, ct);
    }

    public async Task<FavoritoSeccionDto> CrearDesdeCarritoAsync(
        long idUsuario, string sessionId, FavoritosDesdeCarritoRequest request, CancellationToken ct = default)
    {
        var idEmpresa = RequireTenant();
        var user = await RequireUserAsync(idEmpresa, idUsuario, ct);

        var cart = await _db.CarritosCompra.AsNoTracking()
            .Include(c => c.DetalleCarritoCompras.Where(d => d.Activo))
            .FirstOrDefaultAsync(c =>
                c.IDEmpresa == idEmpresa
                && c.IDSesion == sessionId
                && c.Estado == EstadoCarrito.Abierto
                && c.Activo, ct)
            ?? throw new InvalidOperationException("No hay carrito abierto.");

        var config = await _db.EmpresaConfiguracionesEcommerce.AsNoTracking()
            .FirstOrDefaultAsync(c => c.ID == idEmpresa, ct);
        var deliveryArtId = config?.IDArticuloDelivery;

        var artIds = cart.DetalleCarritoCompras
            .Where(d => d.Activo && (deliveryArtId is null or 0 || d.IDArticulo != deliveryArtId))
            .Select(d => d.IDArticulo)
            .Distinct()
            .ToList();
        if (artIds.Count == 0)
            throw new InvalidOperationException("El carrito no tiene productos para guardar.");

        var nombre = string.IsNullOrWhiteSpace(request.NombreLista)
            ? $"Carrito {DateTime.UtcNow:dd/MM HH:mm}"
            : request.NombreLista.Trim();
        var seccion = await CrearSeccionAsync(
            idUsuario,
            new CrearSeccionFavoritoRequest(nombre, null, request.Canal),
            ct);

        var lista = await GetOwnedListaAsync(idEmpresa, idUsuario, seccion.Id, ct);
        var existing = await _db.DetallesListaFavoritoEcommerce
            .Where(d => d.IDListaFavoritoEcommerce == lista.ID && d.Activo)
            .Select(d => d.IDArticulo)
            .ToListAsync(ct);
        var have = existing.ToHashSet();
        var now = DateTime.UtcNow;
        foreach (var idArt in artIds)
        {
            if (have.Contains(idArt)) continue;
            _db.DetallesListaFavoritoEcommerce.Add(new DetalleListaFavoritoEcommerce
            {
                IDEmpresa = idEmpresa,
                IDListaFavoritoEcommerce = lista.ID,
                IDArticulo = idArt,
                FechaAgregado = now,
                Activo = true,
                UltModificacion = now,
                UserName = user.Email ?? user.Usuario
            });
        }
        await _db.SaveChangesAsync(ct);
        await _db.Entry(lista).Collection(l => l.Detalles).Query().Where(d => d.Activo).LoadAsync(ct);
        return (await MapListasAsync(new[] { lista }, ct))[0];
    }

    private async Task<ListaFavoritoEcommerce> EnsureDefaultSectionAsync(long idUsuario, CancellationToken ct)
    {
        var idEmpresa = RequireTenant();
        var user = await RequireUserAsync(idEmpresa, idUsuario, ct);
        var existing = await _db.ListasFavoritoEcommerce
            .Include(l => l.Detalles.Where(d => d.Activo))
            .FirstOrDefaultAsync(l => l.IDEmpresa == idEmpresa
                                      && l.IDUsuarioEcommerce == idUsuario
                                      && l.Activo
                                      && l.NombreLista == "Mis favoritos", ct);
        if (existing is not null)
            return existing;

        return await CrearSeccionEntityAsync(user, "Mis favoritos", null, ct);
    }

    private async Task<ListaFavoritoEcommerce> CrearSeccionEntityAsync(
        UsuarioEcommerce user, string nombre, string? color, CancellationToken ct)
    {
        var cuenta = user.UsuarioEcommerceCuentas.OrderByDescending(c => c.EsPrincipal).FirstOrDefault();
        var entity = new ListaFavoritoEcommerce
        {
            IDEmpresa = user.IDEmpresa,
            IDUsuarioEcommerce = user.ID,
            IDCuenta = cuenta?.IDCuenta,
            Email = user.Email ?? user.Usuario,
            NombreLista = nombre,
            CanalEcommerce = NormalizeCanal(user.CanalPreferido),
            Orden = 0,
            Color = color,
            Activo = true,
            UltModificacion = DateTime.UtcNow,
            UserName = user.Email ?? user.Usuario
        };
        _db.ListasFavoritoEcommerce.Add(entity);
        await _db.SaveChangesAsync(ct);
        return entity;
    }

    private async Task<IReadOnlyList<FavoritoSeccionDto>> MapListasAsync(
        IEnumerable<ListaFavoritoEcommerce> listas, CancellationToken ct)
    {
        var list = listas.ToList();
        var artIds = list.SelectMany(l => l.Detalles.Where(d => d.Activo).Select(d => d.IDArticulo)).Distinct().ToList();
        var arts = artIds.Count == 0
            ? new Dictionary<long, (string? Sku, string Nombre)>()
            : await _db.Articulos.AsNoTracking()
                .Where(a => artIds.Contains(a.ID))
                .ToDictionaryAsync(a => a.ID, a => (Sku: a.SKU, Nombre: a.DescripcionArticulo), ct);

        var imgs = artIds.Count == 0
            ? new Dictionary<long, string>()
            : (await _db.ArticuloImagenes.AsNoTracking()
                .Where(i => artIds.Contains(i.IDArticulo) && i.Activo)
                .OrderByDescending(i => i.EsPrincipal).ThenBy(i => i.Orden)
                .Select(i => new { i.IDArticulo, i.Url })
                .ToListAsync(ct))
            .GroupBy(x => x.IDArticulo).ToDictionary(g => g.Key, g => g.First().Url);

        var priceMap = new Dictionary<long, decimal?>();
        foreach (var id in artIds)
        {
            var prices = await _pricing.GetEmbalajePricesAsync(id, "B2C", ct);
            priceMap[id] = prices.OrderBy(p => p.PrecioContado).Select(p => (decimal?)p.PrecioContado).FirstOrDefault();
        }

        return list.Select(l => new FavoritoSeccionDto(
            l.ID, l.NombreLista, l.CanalEcommerce, l.Email, l.IDCuenta, l.Orden, l.Color,
            l.Detalles.Count(d => d.Activo),
            l.Detalles.Where(d => d.Activo).OrderByDescending(d => d.FechaAgregado).Select(d =>
            {
                arts.TryGetValue(d.IDArticulo, out var a);
                imgs.TryGetValue(d.IDArticulo, out var img);
                priceMap.TryGetValue(d.IDArticulo, out var px);
                return new FavoritoItemDto(
                    d.ID, d.IDArticulo, a.Sku, a.Nombre ?? $"#{d.IDArticulo}", img, px, d.FechaAgregado, d.Nota);
            }).ToList())).ToList();
    }

    private async Task<ListaFavoritoEcommerce> GetOwnedListaAsync(long idEmpresa, long idUsuario, long idLista, CancellationToken ct)
    {
        var lista = await _db.ListasFavoritoEcommerce
            .FirstOrDefaultAsync(l => l.ID == idLista && l.IDEmpresa == idEmpresa && l.IDUsuarioEcommerce == idUsuario && l.Activo, ct)
            ?? throw new InvalidOperationException("Sección no encontrada.");
        return lista;
    }

    private async Task<UsuarioEcommerce> RequireUserAsync(long idEmpresa, long idUsuario, CancellationToken ct) =>
        await _db.UsuariosEcommerce
            .Include(u => u.UsuarioEcommerceCuentas)
            .FirstOrDefaultAsync(u => u.ID == idUsuario && u.IDEmpresa == idEmpresa, ct)
        ?? throw new InvalidOperationException("Usuario no encontrado.");

    private static string NormalizeCanal(string? canal) =>
        string.Equals(canal, "B2B", StringComparison.OrdinalIgnoreCase) ? "B2B" : "B2C";

    private static string? Truncate(string? v, int max) =>
        string.IsNullOrWhiteSpace(v) ? null : (v.Trim().Length <= max ? v.Trim() : v.Trim()[..max]);

    private long RequireTenant()
    {
        if (!_tenant.IsResolved || !_tenant.IDEmpresa.HasValue)
            throw new InvalidOperationException("Tenant no resuelto.");
        return _tenant.IDEmpresa.Value;
    }
}
