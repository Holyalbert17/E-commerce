using Datamex.ECommerce.Application.Abstractions;
using Datamex.ECommerce.Application.Content;
using Datamex.ECommerce.Domain.Entities;
using Datamex.ECommerce.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Datamex.ECommerce.Infrastructure.Content;

public sealed class ContentCmsService : IContentCmsService
{
    private readonly EcomDbContext _db;
    private readonly ITenantContext _tenant;

    public ContentCmsService(EcomDbContext db, ITenantContext tenant)
    {
        _db = db;
        _tenant = tenant;
    }

    public async Task<StorefrontHomeContentDto> GetPublicHomeAsync(CancellationToken ct = default)
    {
        var idEmpresa = RequireTenant();
        await SeedDefaultsIfEmptyAsync(ct);
        var now = DateTime.UtcNow;

        var blocks = await _db.ContenidoBloques.AsNoTracking()
            .Include(b => b.Items)
            .Where(b => b.IDEmpresa == idEmpresa
                        && b.Activo
                        && b.PageCode == "home"
                        && b.ContentStatus == ContenidoEstadoEditorial.Published
                        && (b.StartDate == null || b.StartDate <= now)
                        && (b.EndDate == null || b.EndDate >= now))
            .OrderBy(b => b.DisplayOrder)
            .ToListAsync(ct);

        var menus = await _db.NavegacionMenus.AsNoTracking()
            .Include(m => m.Items)
            .Where(m => m.IDEmpresa == idEmpresa
                        && m.Activo
                        && m.ContentStatus == ContenidoEstadoEditorial.Published)
            .OrderBy(m => m.DisplayOrder)
            .ToListAsync(ct);

        return new StorefrontHomeContentDto(
            blocks.Select(MapBlock).ToList(),
            menus.Select(m => MapMenu(m, onlyActiveItems: true)).ToList());
    }

    public async Task<IReadOnlyList<ContenidoBloqueDto>> ListBlocksAsync(string? pageCode, CancellationToken ct = default)
    {
        var idEmpresa = RequireTenant();
        await SeedDefaultsIfEmptyAsync(ct);
        var q = _db.ContenidoBloques.AsNoTracking()
            .Include(b => b.Items)
            .Where(b => b.IDEmpresa == idEmpresa);
        if (!string.IsNullOrWhiteSpace(pageCode))
            q = q.Where(b => b.PageCode == pageCode);
        var rows = await q.OrderBy(b => b.DisplayOrder).ThenBy(b => b.ID).ToListAsync(ct);
        return rows.Select(MapBlock).ToList();
    }

    public async Task<ContenidoBloqueDto?> GetBlockAsync(long id, CancellationToken ct = default)
    {
        var idEmpresa = RequireTenant();
        var b = await _db.ContenidoBloques.AsNoTracking()
            .Include(x => x.Items)
            .FirstOrDefaultAsync(x => x.ID == id && x.IDEmpresa == idEmpresa, ct);
        return b is null ? null : MapBlock(b);
    }

    public async Task<ContenidoBloqueDto> UpsertBlockAsync(
        long? id, UpsertContenidoBloqueRequest request, long? adminUserId, CancellationToken ct = default)
    {
        var idEmpresa = RequireTenant();
        ContenidoBloque entity;
        if (id is > 0)
        {
            entity = await _db.ContenidoBloques
                .Include(x => x.Items)
                .FirstOrDefaultAsync(x => x.ID == id && x.IDEmpresa == idEmpresa, ct)
                ?? throw new InvalidOperationException("Bloque no encontrado.");
        }
        else
        {
            entity = new ContenidoBloque { IDEmpresa = idEmpresa, FechaCreacion = DateTime.UtcNow };
            _db.ContenidoBloques.Add(entity);
        }

        entity.PageCode = Trunc(request.PageCode, 80);
        entity.PageZone = TruncOpt(request.PageZone, 50);
        entity.BlockType = Trunc(request.BlockType, 60);
        entity.InternalName = Trunc(request.InternalName, 150);
        entity.Title = TruncOpt(request.Title, 250);
        entity.Subtitle = TruncOpt(request.Subtitle, 250);
        entity.Description = request.Description;
        entity.DisplayOrder = request.DisplayOrder;
        entity.ContentStatus = Trunc(request.ContentStatus, 20);
        entity.Activo = request.Activo;
        entity.IsReadOnlyWhenIntegrated = request.IsReadOnlyWhenIntegrated;
        entity.StartDate = request.StartDate;
        entity.EndDate = request.EndDate;
        entity.SettingsJson = request.SettingsJson;
        entity.UpdatedByAdminUserId = adminUserId;
        entity.UltModificacion = DateTime.UtcNow;
        entity.UserName = "cms-admin";
        if (entity.ContentStatus == ContenidoEstadoEditorial.Published && entity.PublishedAt is null)
            entity.PublishedAt = DateTime.UtcNow;

        if (request.Items is not null)
        {
            var keep = new HashSet<long>();
            foreach (var it in request.Items)
            {
                ContenidoBloqueItem row;
                if (it.Id is > 0)
                {
                    row = entity.Items.FirstOrDefault(x => x.ID == it.Id)
                          ?? new ContenidoBloqueItem { IDEmpresa = idEmpresa, IDContenidoBloque = entity.ID };
                    if (row.ID == 0) entity.Items.Add(row);
                    keep.Add(it.Id.Value);
                }
                else
                {
                    row = new ContenidoBloqueItem { IDEmpresa = idEmpresa };
                    entity.Items.Add(row);
                }

                row.ItemType = Trunc(it.ItemType, 50);
                row.Title = TruncOpt(it.Title, 250);
                row.Subtitle = TruncOpt(it.Subtitle, 250);
                row.Description = it.Description;
                row.ImageUrl = TruncOpt(it.ImageUrl, 800);
                row.MobileImageUrl = TruncOpt(it.MobileImageUrl, 800);
                row.LinkUrl = TruncOpt(it.LinkUrl, 800);
                row.LinkTarget = TruncOpt(it.LinkTarget, 20) ?? "_self";
                row.BadgeText = TruncOpt(it.BadgeText, 50);
                row.PriceText = TruncOpt(it.PriceText, 100);
                row.DisplayOrder = it.DisplayOrder;
                row.Activo = it.Activo;
                row.ContentStatus = ContenidoEstadoEditorial.Published;
                row.UltModificacion = DateTime.UtcNow;
                row.UserName = "cms-admin";
            }

            var remove = entity.Items.Where(x => x.ID > 0 && !keep.Contains(x.ID)).ToList();
            foreach (var r in remove)
                _db.ContenidoBloqueItems.Remove(r);
        }

        await _db.SaveChangesAsync(ct);
        return (await GetBlockAsync(entity.ID, ct))!;
    }

    public async Task DeleteBlockAsync(long id, CancellationToken ct = default)
    {
        var idEmpresa = RequireTenant();
        var b = await _db.ContenidoBloques.Include(x => x.Items)
            .FirstOrDefaultAsync(x => x.ID == id && x.IDEmpresa == idEmpresa, ct)
            ?? throw new InvalidOperationException("Bloque no encontrado.");
        _db.ContenidoBloqueItems.RemoveRange(b.Items);
        _db.ContenidoBloques.Remove(b);
        await _db.SaveChangesAsync(ct);
    }

    public async Task SeedDefaultsIfEmptyAsync(CancellationToken ct = default)
    {
        var idEmpresa = RequireTenant();
        if (!await _db.ContenidoBloques.AnyAsync(b => b.IDEmpresa == idEmpresa, ct))
        {
            _db.ContenidoBloques.AddRange(BuildStandardHomeBlocks(idEmpresa, DateTime.UtcNow));
            await _db.SaveChangesAsync(ct);
        }
        else
        {
            await EnsureMissingHomeBlocksAsync(ct);
        }

        if (!await _db.NavegacionMenus.AnyAsync(m => m.IDEmpresa == idEmpresa, ct))
        {
            _db.NavegacionMenus.Add(BuildDefaultHeaderMenu(idEmpresa));
            await _db.SaveChangesAsync(ct);
        }
    }

    /// <summary>Agrega bloques home estándar que falten (cupón, categorías, carrusel, etc.).</summary>
    public async Task<int> EnsureMissingHomeBlocksAsync(CancellationToken ct = default)
    {
        var idEmpresa = RequireTenant();
        var existing = await _db.ContenidoBloques.AsNoTracking()
            .Where(b => b.IDEmpresa == idEmpresa && b.PageCode == "home")
            .Select(b => b.BlockType)
            .ToListAsync(ct);
        var have = existing.ToHashSet(StringComparer.OrdinalIgnoreCase);
        var now = DateTime.UtcNow;
        var add = BuildStandardHomeBlocks(idEmpresa, now)
            .Where(b => !have.Contains(b.BlockType))
            .ToList();
        if (add.Count == 0) return 0;
        _db.ContenidoBloques.AddRange(add);
        await _db.SaveChangesAsync(ct);
        return add.Count;
    }

    private static List<ContenidoBloque> BuildStandardHomeBlocks(long idEmpresa, DateTime now)
    {
        return new List<ContenidoBloque>
        {
            new()
            {
                IDEmpresa = idEmpresa,
                PageCode = "home",
                PageZone = "topbar",
                BlockType = ContenidoBloqueTipo.TopInfoBar,
                InternalName = "Top bar",
                Title = "Envíos a todo el país · Pagá seguro",
                DisplayOrder = 10,
                ContentStatus = ContenidoEstadoEditorial.Published,
                PublishedAt = now,
                Activo = true,
                Items =
                {
                    new ContenidoBloqueItem
                    {
                        IDEmpresa = idEmpresa,
                        ItemType = "link",
                        Title = "Ayuda",
                        LinkUrl = "/mi-cuenta",
                        DisplayOrder = 1,
                        Activo = true,
                        ContentStatus = ContenidoEstadoEditorial.Published
                    }
                }
            },
            new()
            {
                IDEmpresa = idEmpresa,
                PageCode = "home",
                PageZone = "home_main",
                BlockType = ContenidoBloqueTipo.HeroBanner,
                InternalName = "Hero principal",
                Title = "Ofertas claras. Stock real. Compra fácil.",
                Subtitle = "Explorá categorías, aprovechá promociones y llevá tu pedido en minutos.",
                DisplayOrder = 20,
                ContentStatus = ContenidoEstadoEditorial.Published,
                PublishedAt = now,
                Activo = true
            },
            new()
            {
                IDEmpresa = idEmpresa,
                PageCode = "home",
                PageZone = "home_main",
                BlockType = ContenidoBloqueTipo.FeaturedCategories,
                InternalName = "Categorías destacadas",
                Title = "Categorías",
                Subtitle = "Atajos a rubros (opcional; si vacío usa el catálogo)",
                DisplayOrder = 25,
                ContentStatus = ContenidoEstadoEditorial.Published,
                PublishedAt = now,
                Activo = true,
                Items = { }
            },
            new()
            {
                IDEmpresa = idEmpresa,
                PageCode = "home",
                PageZone = "home_main",
                BlockType = ContenidoBloqueTipo.PromoBannerGrid,
                InternalName = "Promos / flyers CMS",
                Title = "Ofertas de la semana",
                Subtitle = "Si los ítems no tienen imagen, la tienda usa flyers de Promociones",
                DisplayOrder = 30,
                ContentStatus = ContenidoEstadoEditorial.Published,
                PublishedAt = now,
                Activo = true,
                Items =
                {
                    new ContenidoBloqueItem
                    {
                        IDEmpresa = idEmpresa, ItemType = "banner", Title = "Ofertas de la semana",
                        BadgeText = "Promo", LinkUrl = "/catalogo#ofertas", DisplayOrder = 1, Activo = true,
                        ContentStatus = ContenidoEstadoEditorial.Published
                    },
                    new ContenidoBloqueItem
                    {
                        IDEmpresa = idEmpresa, ItemType = "banner", Title = "Novedades",
                        BadgeText = "Nuevo", LinkUrl = "/buscar", DisplayOrder = 2, Activo = true,
                        ContentStatus = ContenidoEstadoEditorial.Published
                    }
                }
            },
            new()
            {
                IDEmpresa = idEmpresa,
                PageCode = "home",
                PageZone = "home_main",
                BlockType = ContenidoBloqueTipo.CouponStrip,
                InternalName = "Cupón home",
                Title = "Cupón de bienvenida",
                Subtitle = "Aplicá el código en el checkout",
                Description = "Debe coincidir con un cupón activo en Marketplace",
                DisplayOrder = 35,
                ContentStatus = ContenidoEstadoEditorial.Published,
                PublishedAt = now,
                Activo = true,
                Items =
                {
                    new ContenidoBloqueItem
                    {
                        IDEmpresa = idEmpresa,
                        ItemType = "coupon",
                        Title = "Bienvenida",
                        BadgeText = "BIENVENIDO10",
                        Description = "Código del cupón (Marketplace)",
                        LinkUrl = "/checkout",
                        DisplayOrder = 1,
                        Activo = true,
                        ContentStatus = ContenidoEstadoEditorial.Published
                    }
                }
            },
            new()
            {
                IDEmpresa = idEmpresa,
                PageCode = "home",
                PageZone = "home_main",
                BlockType = ContenidoBloqueTipo.ProductCarousel,
                InternalName = "Carrusel recomendados",
                Title = "Recomendados para vos",
                Subtitle = "Título de la sección de recomendaciones",
                DisplayOrder = 40,
                ContentStatus = ContenidoEstadoEditorial.Published,
                PublishedAt = now,
                Activo = true
            },
            new()
            {
                IDEmpresa = idEmpresa,
                PageCode = "home",
                PageZone = "pre_footer",
                BlockType = ContenidoBloqueTipo.FeatureList,
                InternalName = "Beneficios",
                Title = "Por qué comprar con nosotros",
                DisplayOrder = 80,
                ContentStatus = ContenidoEstadoEditorial.Published,
                PublishedAt = now,
                Activo = true,
                Items =
                {
                    new() { IDEmpresa = idEmpresa, ItemType = "feature", Title = "Stock real", Description = "Disponibilidad al confirmar", DisplayOrder = 1, Activo = true, ContentStatus = ContenidoEstadoEditorial.Published },
                    new() { IDEmpresa = idEmpresa, ItemType = "feature", Title = "Pago seguro", Description = "Bancard y pago en entrega", DisplayOrder = 2, Activo = true, ContentStatus = ContenidoEstadoEditorial.Published },
                    new() { IDEmpresa = idEmpresa, ItemType = "feature", Title = "Seguimiento", Description = "Estado de tu pedido en vivo", DisplayOrder = 3, Activo = true, ContentStatus = ContenidoEstadoEditorial.Published }
                }
            },
            new()
            {
                IDEmpresa = idEmpresa,
                PageCode = "home",
                PageZone = "pre_footer",
                BlockType = ContenidoBloqueTipo.Newsletter,
                InternalName = "Newsletter",
                Title = "Recibí novedades",
                Subtitle = "Promos y reposición en tu correo",
                DisplayOrder = 90,
                ContentStatus = ContenidoEstadoEditorial.Published,
                PublishedAt = now,
                Activo = true
            }
        };
    }

    private static NavegacionMenu BuildDefaultHeaderMenu(long idEmpresa) =>
        new()
        {
            IDEmpresa = idEmpresa,
            Code = "main_header",
            Name = "Menú principal",
            MenuType = NavegacionMenuTipo.Header,
            PageCode = "global",
            PageZone = "header",
            DisplayOrder = 1,
            ContentStatus = ContenidoEstadoEditorial.Published,
            Activo = true,
            Items =
            {
                new NavegacionMenuItem { IDEmpresa = idEmpresa, ItemType = "external_link", Label = "Inicio", Url = "/catalogo", DisplayOrder = 1, Activo = true, ContentStatus = ContenidoEstadoEditorial.Published },
                new NavegacionMenuItem { IDEmpresa = idEmpresa, ItemType = "external_link", Label = "Catálogo", Url = "/catalogo?browse=1", DisplayOrder = 2, Activo = true, ContentStatus = ContenidoEstadoEditorial.Published },
                new NavegacionMenuItem { IDEmpresa = idEmpresa, ItemType = "external_link", Label = "Ofertas", Url = "/catalogo#ofertas", DisplayOrder = 3, Activo = true, ContentStatus = ContenidoEstadoEditorial.Published },
                new NavegacionMenuItem { IDEmpresa = idEmpresa, ItemType = "external_link", Label = "Sucursales", Url = "/sucursales", DisplayOrder = 4, Activo = true, ContentStatus = ContenidoEstadoEditorial.Published },
                new NavegacionMenuItem { IDEmpresa = idEmpresa, ItemType = "external_link", Label = "Mis listas", Url = "/favoritos", DisplayOrder = 5, Activo = true, ContentStatus = ContenidoEstadoEditorial.Published },
                new NavegacionMenuItem { IDEmpresa = idEmpresa, ItemType = "external_link", Label = "Seguidos", Url = "/seguidos", DisplayOrder = 6, Activo = true, ContentStatus = ContenidoEstadoEditorial.Published },
                new NavegacionMenuItem { IDEmpresa = idEmpresa, ItemType = "external_link", Label = "Mi cuenta", Url = "/mi-cuenta", DisplayOrder = 7, Activo = true, ContentStatus = ContenidoEstadoEditorial.Published },
                new NavegacionMenuItem { IDEmpresa = idEmpresa, ItemType = "external_link", Label = "Mis pedidos", Url = "/mis-pedidos", DisplayOrder = 8, Activo = true, ContentStatus = ContenidoEstadoEditorial.Published }
            }
        };

    public async Task<IReadOnlyList<NavegacionMenuDto>> ListMenusAsync(CancellationToken ct = default)
    {
        var idEmpresa = RequireTenant();
        await SeedDefaultsIfEmptyAsync(ct);
        var rows = await _db.NavegacionMenus.AsNoTracking()
            .Include(m => m.Items)
            .Where(m => m.IDEmpresa == idEmpresa)
            .OrderBy(m => m.DisplayOrder)
            .ToListAsync(ct);
        return rows.Select(m => MapMenu(m, onlyActiveItems: false)).ToList();
    }

    public async Task<NavegacionMenuDto> UpsertMenuAsync(long? id, UpsertNavegacionMenuRequest request, CancellationToken ct = default)
    {
        var idEmpresa = RequireTenant();
        NavegacionMenu entity;
        if (id is > 0)
        {
            entity = await _db.NavegacionMenus.Include(m => m.Items)
                .FirstOrDefaultAsync(m => m.ID == id && m.IDEmpresa == idEmpresa, ct)
                ?? throw new InvalidOperationException("Menú no encontrado.");
        }
        else
        {
            entity = new NavegacionMenu { IDEmpresa = idEmpresa };
            _db.NavegacionMenus.Add(entity);
        }

        entity.Code = Trunc(request.Code, 100);
        entity.Name = Trunc(request.Name, 150);
        entity.MenuType = Trunc(request.MenuType, 50);
        entity.PageCode = TruncOpt(request.PageCode, 80);
        entity.PageZone = TruncOpt(request.PageZone, 50);
        entity.DisplayOrder = request.DisplayOrder;
        entity.ContentStatus = Trunc(request.ContentStatus, 20);
        entity.Activo = request.Activo;
        entity.UltModificacion = DateTime.UtcNow;
        entity.UserName = "cms-admin";

        if (request.Items is not null)
        {
            var keep = new HashSet<long>();
            foreach (var it in request.Items)
            {
                NavegacionMenuItem row;
                if (it.Id is > 0)
                {
                    row = entity.Items.FirstOrDefault(x => x.ID == it.Id)
                          ?? new NavegacionMenuItem { IDEmpresa = idEmpresa };
                    if (row.ID == 0) entity.Items.Add(row);
                    keep.Add(it.Id.Value);
                }
                else
                {
                    row = new NavegacionMenuItem { IDEmpresa = idEmpresa };
                    entity.Items.Add(row);
                }

                row.ItemType = Trunc(it.ItemType, 50);
                row.Label = Trunc(it.Label, 150);
                row.Url = TruncOpt(it.Url, 800);
                row.Target = TruncOpt(it.Target, 20) ?? "_self";
                row.BadgeText = TruncOpt(it.BadgeText, 50);
                row.DisplayOrder = it.DisplayOrder;
                row.IDGrupoArticulo = it.IDGrupoArticulo;
                row.Activo = it.Activo;
                row.ContentStatus = ContenidoEstadoEditorial.Published;
                row.UltModificacion = DateTime.UtcNow;
                row.UserName = "cms-admin";
            }

            foreach (var r in entity.Items.Where(x => x.ID > 0 && !keep.Contains(x.ID)).ToList())
                _db.NavegacionMenuItems.Remove(r);
        }

        await _db.SaveChangesAsync(ct);
        var saved = await _db.NavegacionMenus.AsNoTracking().Include(m => m.Items)
            .FirstAsync(m => m.ID == entity.ID, ct);
        return MapMenu(saved, onlyActiveItems: false);
    }

    public async Task DeleteMenuAsync(long id, CancellationToken ct = default)
    {
        var idEmpresa = RequireTenant();
        var m = await _db.NavegacionMenus.Include(x => x.Items)
            .FirstOrDefaultAsync(x => x.ID == id && x.IDEmpresa == idEmpresa, ct)
            ?? throw new InvalidOperationException("Menú no encontrado.");
        _db.NavegacionMenuItems.RemoveRange(m.Items);
        _db.NavegacionMenus.Remove(m);
        await _db.SaveChangesAsync(ct);
    }

    private static ContenidoBloqueDto MapBlock(ContenidoBloque b) => new(
        b.ID, b.PageCode, b.PageZone, b.BlockType, b.InternalName,
        b.Title, b.Subtitle, b.Description, b.DisplayOrder, b.ContentStatus, b.Activo,
        b.IsReadOnlyWhenIntegrated, b.StartDate, b.EndDate, b.SettingsJson,
        b.Items.Where(i => i.Activo).OrderBy(i => i.DisplayOrder)
            .Select(i => new ContenidoBloqueItemDto(
                i.ID, i.ItemType, i.Title, i.Subtitle, i.Description,
                i.ImageUrl, i.MobileImageUrl, i.LinkUrl, i.LinkTarget,
                i.BadgeText, i.PriceText, i.DisplayOrder, i.Activo))
            .ToList());

    private static NavegacionMenuDto MapMenu(NavegacionMenu m, bool onlyActiveItems) => new(
        m.ID, m.Code, m.Name, m.MenuType, m.PageCode, m.PageZone, m.DisplayOrder, m.ContentStatus, m.Activo,
        m.Items
            .Where(i => !onlyActiveItems || i.Activo)
            .OrderBy(i => i.DisplayOrder)
            .Select(i => new NavegacionMenuItemDto(
                i.ID, i.ItemType, i.Label, i.Url, i.Target, i.BadgeText, i.DisplayOrder, i.IDGrupoArticulo, i.Activo))
            .ToList());

    private long RequireTenant()
    {
        if (!_tenant.IsResolved || !_tenant.IDEmpresa.HasValue)
            throw new InvalidOperationException("Tenant no resuelto.");
        return _tenant.IDEmpresa.Value;
    }

    private static string Trunc(string s, int max)
    {
        s = s.Trim();
        return s.Length <= max ? s : s[..max];
    }

    private static string? TruncOpt(string? s, int max) =>
        string.IsNullOrWhiteSpace(s) ? null : Trunc(s, max);
}
