using Datamex.ECommerce.Domain.Entities;
using Datamex.ECommerce.Infrastructure.Admin;
using Datamex.ECommerce.Infrastructure.Auth;
using Datamex.ECommerce.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Datamex.ECommerce.Infrastructure.Seed;

public static class DemoSeed
{
    public const string DemoSlug = "demo";
    public const string DemoUser = "demo";
    public const string DemoPassword = "demo123";

    public static async Task EnsureSeedAsync(IServiceProvider services, CancellationToken ct = default)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<EcomDbContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("DemoSeed");

        await db.Database.MigrateAsync(ct);

        // Tenant operativo preferido: Casa Imperial. El seed `demo` solo se crea si no hay
        // ningún tenant (DB vacía); si ya existe casa-imperial, no forzamos recrear demo.
        var config = await db.EmpresaConfiguracionesEcommerce
            .FirstOrDefaultAsync(c => c.SlugTienda == "casa-imperial", ct)
            ?? await db.EmpresaConfiguracionesEcommerce
                .FirstOrDefaultAsync(c => c.SlugTienda == DemoSlug, ct);

        if (config is null)
        {
            await SeedFreshAsync(db, logger, ct);
            config = await db.EmpresaConfiguracionesEcommerce
                .FirstAsync(c => c.SlugTienda == DemoSlug, ct);
        }

        // Enrichments de catálogo/demo solo sobre el tenant seed `demo` (no tocar Casa Imperial ERP).
        if (string.Equals(config.SlugTienda, DemoSlug, StringComparison.OrdinalIgnoreCase))
        {
            await EnsureCatalogEnrichmentAsync(db, config.ID, logger, ct);
            await EnsurePricingEnrichmentAsync(db, config.ID, logger, ct);
            await EnsureInventoryEnrichmentAsync(db, config.ID, logger, ct);
            await EnsureGeographyMastersAsync(db, logger, ct);
            await EnsureShippingAndAccountEnrichmentAsync(db, config.ID, logger, ct);
            await EnsureAuthorizationEstadosAsync(db, config.ID, logger, ct);
            await EnsureBancardDemoConfigAsync(db, config, logger, ct);
            await EnsureUnidadesMedidaAsync(db, config.ID, logger, ct);
            await EnsureMarketingDemoAsync(db, config.ID, logger, ct);
            await EnsureFooterBrandingDemoAsync(db, config, logger, ct);
        }
        else
        {
            // Geo maestros (compartidos) + RBAC del tenant operativo.
            await EnsureGeographyMastersAsync(db, logger, ct);
        }

        await EnsureBackofficeRbacAsync(db, config.ID, logger, ct);
        await EnsureCatalogoParametrosAsync(db, config.ID, logger, ct);

        // Tenants sincronizados adicionales: RBAC + parámetros de producto locales.
        var otherIds = await db.EmpresaConfiguracionesEcommerce.AsNoTracking()
            .Where(c => c.ID != config.ID && c.Activo)
            .Select(c => c.ID)
            .ToListAsync(ct);
        foreach (var id in otherIds)
        {
            await EnsureBackofficeRbacAsync(db, id, logger, ct);
            await EnsureCatalogoParametrosAsync(db, id, logger, ct);
        }
    }

    private static async Task EnsureCatalogoParametrosAsync(
        EcomDbContext db, long idEmpresa, ILogger logger, CancellationToken ct)
    {
        async Task<CatalogoParametro> EnsureOne(string tipo, int codigo, string desc)
        {
            var row = await db.CatalogoParametros
                .FirstOrDefaultAsync(p => p.IDEmpresa == idEmpresa && p.Tipo == tipo && p.Codigo == codigo, ct);
            if (row is not null) return row;
            row = new CatalogoParametro
            {
                IDEmpresa = idEmpresa,
                Tipo = tipo,
                Codigo = codigo,
                Descripcion = desc,
                Activo = true,
                UltModificacion = DateTime.UtcNow,
                UserName = "seed"
            };
            db.CatalogoParametros.Add(row);
            await db.SaveChangesAsync(ct);
            return row;
        }

        var marcaDemo = await EnsureOne(CatalogoParametroTipos.Marca, 1, "DemoBrand");
        await EnsureOne(CatalogoParametroTipos.Marca, 2, "CleanPro");
        await EnsureOne(CatalogoParametroTipos.Marca, 3, "CarePlus");
        var lineaGen = await EnsureOne(CatalogoParametroTipos.Linea, 1, "General");
        var tipoProd = await EnsureOne(CatalogoParametroTipos.Tipo, 1, "Producto");
        var embUn = await EnsureOne(CatalogoParametroTipos.Embalaje, 1, "Unidad");
        await EnsureOne(CatalogoParametroTipos.Embalaje, 2, "Caja");
        var prov = await EnsureOne(CatalogoParametroTipos.Proveedor, 1, "Proveedor Demo");
        var paisPy = await EnsureOne(CatalogoParametroTipos.Pais, 1, "Paraguay");
        var regimen = await EnsureOne(CatalogoParametroTipos.Regimen, 1, "IVA 10%");
        await EnsureOne(CatalogoParametroTipos.Regimen, 2, "Exento");

        var marcas = await db.CatalogoParametros.AsNoTracking()
            .Where(p => p.IDEmpresa == idEmpresa && p.Tipo == CatalogoParametroTipos.Marca && p.Activo)
            .ToListAsync(ct);

        var arts = await db.Articulos.Where(a => a.IDEmpresa == idEmpresa).ToListAsync(ct);
        var changed = 0;
        foreach (var a in arts)
        {
            var marca = marcas.FirstOrDefault(m =>
                            string.Equals(m.Descripcion, a.NombreMarca, StringComparison.OrdinalIgnoreCase))
                        ?? marcaDemo;
            if (a.IDMarca != marca.ID || a.NombreMarca != marca.Descripcion)
            {
                a.IDMarca = marca.ID;
                a.NombreMarca = marca.Descripcion;
                changed++;
            }
            if (a.IDLinea is null or 0)
            {
                a.IDLinea = lineaGen.ID;
                a.NombreLinea = lineaGen.Descripcion;
                changed++;
            }
            if (a.IDTipoArticulo is null or 0)
            {
                a.IDTipoArticulo = tipoProd.ID;
                a.NombreTipoArticulo = tipoProd.Descripcion;
                changed++;
            }
            if (a.IDProveedor is null or 0)
            {
                a.IDProveedor = prov.ID;
                a.NombreProveedor = prov.Descripcion;
                changed++;
            }
            if (a.IDPais is null or 0)
            {
                a.IDPais = paisPy.ID;
                a.NombrePais = paisPy.Descripcion;
                changed++;
            }
            if (a.IDRegimenImpositivo is null or 0)
            {
                a.IDRegimenImpositivo = regimen.ID;
                a.NombreRegimenImpositivo = regimen.Descripcion;
                changed++;
            }
        }

        var embs = await db.ArticuloEmbalajes
            .Where(e => e.IDEmpresa == idEmpresa && (e.IDEmbalaje <= 0 || e.DescripcionEmbalaje == null))
            .ToListAsync(ct);
        foreach (var e in embs)
        {
            e.IDEmbalaje = embUn.ID;
            e.DescripcionEmbalaje ??= embUn.Descripcion;
            changed++;
        }

        if (changed > 0)
        {
            await db.SaveChangesAsync(ct);
            logger.LogInformation("CatalogoParametro seed/backfill ({Changed} updates) IDEmpresa={Id}", changed, idEmpresa);
        }
    }

    private static async Task EnsureFooterBrandingDemoAsync(
        EcomDbContext db, EmpresaConfiguracionEcommerce config, ILogger logger, CancellationToken ct)
    {
        var changed = false;
        if (string.IsNullOrWhiteSpace(config.FooterDescripcion))
        {
            config.FooterDescripcion = "Compra mayorista y minorista con stock real, delivery en Asunción y retiro en sucursal.";
            changed = true;
        }
        if (string.IsNullOrWhiteSpace(config.DireccionLocal))
        {
            config.DireccionLocal = "Av. España 1234";
            changed = true;
        }
        if (string.IsNullOrWhiteSpace(config.CiudadLocal))
        {
            config.CiudadLocal = "Asunción, Paraguay";
            changed = true;
        }
        if (string.IsNullOrWhiteSpace(config.TelefonoContacto))
        {
            config.TelefonoContacto = "+595 21 555-0100";
            changed = true;
        }
        if (string.IsNullOrWhiteSpace(config.EmailContacto))
        {
            config.EmailContacto = "hola@datamex.local";
            changed = true;
        }
        if (string.IsNullOrWhiteSpace(config.HorarioAtencionTexto))
        {
            config.HorarioAtencionTexto = "Lun–Vie 8:00–18:00 · Sáb 8:00–12:00";
            changed = true;
        }
        if (config.LatitudLocal is null || config.LongitudLocal is null)
        {
            config.LatitudLocal = -25.2865m;
            config.LongitudLocal = -57.6470m;
            changed = true;
        }
        if (!config.MostrarMapaFooter)
        {
            config.MostrarMapaFooter = true;
            changed = true;
        }
        if (string.IsNullOrWhiteSpace(config.DireccionURLFacebook))
        {
            config.DireccionURLFacebook = "https://facebook.com";
            changed = true;
        }
        if (string.IsNullOrWhiteSpace(config.DireccionURLInstagram))
        {
            config.DireccionURLInstagram = "https://instagram.com";
            changed = true;
        }
        if (string.IsNullOrWhiteSpace(config.DireccionURLWhatsapp))
        {
            config.DireccionURLWhatsapp = "https://wa.me/595981000000";
            changed = true;
        }

        if (!changed) return;
        config.UltModificacion = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        logger.LogInformation("Demo footer branding (contacto + mapa) aplicado.");
    }

    private static async Task EnsureBancardDemoConfigAsync(
        EcomDbContext db, EmpresaConfiguracionEcommerce config, ILogger logger, CancellationToken ct)
    {
        var changed = false;
        if (string.IsNullOrWhiteSpace(config.ClavePublicaProcesadoraTarjeta))
        {
            config.ClavePublicaProcesadoraTarjeta = "demo-public";
            changed = true;
        }
        if (string.IsNullOrWhiteSpace(config.ClavePrivadaProcesadoraTarjeta))
        {
            config.ClavePrivadaProcesadoraTarjeta = "demo-private";
            changed = true;
        }
        if (string.IsNullOrWhiteSpace(config.DireccionURLCheckout))
        {
            config.DireccionURLCheckout = "http://localhost:4200/pago";
            changed = true;
        }
        if (config.CodigoComercio is null or 0)
        {
            config.CodigoComercio = 1;
            changed = true;
        }
        if (config.CodigoSucursal is null or 0)
        {
            config.CodigoSucursal = 1;
            changed = true;
        }
        if (!config.CobraEnLaEntrega)
        {
            config.CobraEnLaEntrega = true;
            changed = true;
        }
        // No forzar CobraAlHacerPedido: UAT de pedidos va contra entrega (Bancard después).
        if (config.CobraAlHacerPedido)
        {
            config.CobraAlHacerPedido = false;
            changed = true;
        }

        if (!changed) return;
        config.UltModificacion = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        logger.LogInformation("Bancard demo keys/URL ensured for IDEmpresa={Id} (pago online off)", config.ID);
    }

    private static async Task SeedFreshAsync(EcomDbContext db, ILogger logger, CancellationToken ct)
    {
        var now = DateTime.UtcNow;

        var empresa = new Empresa
        {
            Activo = true,
            UltModificacion = now,
            UserName = "seed",
            RazonSocial = "Datamex Demo S.A.",
            NombreFantasia = "Demo Store",
            NumeroDocumento = "80000001-0"
        };
        db.Empresas.Add(empresa);
        await db.SaveChangesAsync(ct);

        db.EmpresaConfiguracionesEcommerce.Add(new EmpresaConfiguracionEcommerce
        {
            ID = empresa.ID,
            Activo = true,
            UltModificacion = now,
            UserName = "seed",
            RealizaDelivery = true,
            RealizaPickup = true,
            ProcesaPedidosERP = true,
            CobraAlHacerPedido = false,
            CobraEnLaEntrega = true,
            ClavePublicaProcesadoraTarjeta = "demo-public",
            ClavePrivadaProcesadoraTarjeta = "demo-private",
            DireccionURLCheckout = "http://localhost:4200/pago",
            CodigoComercio = 1,
            CodigoSucursal = 1,
            IDDeposito = 1,
            SlugTienda = DemoSlug,
            MontoPedidoMinimo = 0,
            ControlaStockEnCarrito = true,
            ModoIntegracion = (int)Datamex.ECommerce.Domain.Enums.IntegrationMode.Standalone
        });

        db.ListasPrecio.Add(new ListaPrecio
        {
            IDEmpresa = empresa.ID,
            Activo = true,
            UltModificacion = now,
            NroListaPrecio = 1,
            Descripcion = "Lista Ecom B2C",
            EsListaPrecioEcommerce = true,
            PrecioConImpuestos = true,
            FechaRegistro = now,
            FechaInicioVigencia = now.AddYears(-1),
            CanalEcommerce = "B2C",
            Estado = 2 // AUTORIZADO
        });
        db.ListasPrecio.Add(new ListaPrecio
        {
            IDEmpresa = empresa.ID,
            Activo = true,
            UltModificacion = now,
            NroListaPrecio = 2,
            Descripcion = "Lista Ecom B2B",
            EsListaPrecioEcommerce = true,
            PrecioConImpuestos = true,
            FechaRegistro = now,
            FechaInicioVigencia = now.AddYears(-1),
            CanalEcommerce = "B2B",
            Estado = 2
        });

        db.UsuariosEcommerce.Add(new UsuarioEcommerce
        {
            IDEmpresa = empresa.ID,
            Activo = true,
            Activado = true,
            Bloqueado = false,
            UltModificacion = now,
            CodigoUsuario = "DEMO",
            Usuario = DemoUser,
            Password = $"plain:{DemoPassword}",
            Nombre = "Usuario",
            Apellido = "Demo",
            Email = "demo@datamex.local",
            FechaRegistro = now,
            CanalPreferido = "B2C",
            CheckSum = JwtAuthService.HashPassword(DemoPassword)
        });

        db.TiendasEcommerce.Add(new TiendaEcommerce
        {
            IDEmpresa = empresa.ID,
            Activo = true,
            UltModificacion = now,
            IDClasificacionArticulo = 1,
            TokenTienda = "demo-token",
            IDDepositoTienda = 1,
            Nombre = "Vitrina Demo",
            HabilitaB2C = true,
            HabilitaB2B = true
        });

        await db.SaveChangesAsync(ct);
        await EnsureCatalogEnrichmentAsync(db, empresa.ID, logger, ct);
        await EnsurePricingEnrichmentAsync(db, empresa.ID, logger, ct);
        await EnsureInventoryEnrichmentAsync(db, empresa.ID, logger, ct);
        await EnsureGeographyMastersAsync(db, logger, ct);
        await EnsureShippingAndAccountEnrichmentAsync(db, empresa.ID, logger, ct);

        logger.LogInformation(
            "Seeded demo tenant: IDEmpresa={Id} slug={Slug} user={User} password={Password}",
            empresa.ID, DemoSlug, DemoUser, DemoPassword);
    }

    private static async Task EnsureCatalogEnrichmentAsync(
        EcomDbContext db, long idEmpresa, ILogger logger, CancellationToken ct)
    {
        var now = DateTime.UtcNow;
        var articulos = await db.Articulos.Where(a => a.IDEmpresa == idEmpresa).ToListAsync(ct);

        if (articulos.Count == 0)
        {
            var names = new[]
            {
                ("Arroz Premium 1kg", "Arroz 1kg", "Alimentos"),
                ("Aceite Girasol 900ml", "Aceite 900ml", "Alimentos"),
                ("Detergente 3L", "Detergente", "Limpieza"),
                ("Shampoo 400ml", "Shampoo", "Cuidado"),
                ("Galletitas Surtidas", "Galletitas", "Alimentos"),
                ("Kit Desayuno Familiar", "Kit Desayuno", "Combos"),
                ("Café Molido 500g", "Café 500g", "Alimentos"),
                ("Jabón en Polvo 2kg", "Jabón 2kg", "Limpieza")
            };

            for (var i = 0; i < names.Length; i++)
            {
                articulos.Add(new Articulo
                {
                    IDEmpresa = idEmpresa,
                    Activo = true,
                    ActivoEcommerce = true,
                    UltModificacion = now,
                    NroArticulo = i + 1,
                    SKU = $"SKU-DEMO-{(i + 1):D3}",
                    DescripcionArticulo = names[i].Item1,
                    DescripcionArticuloAbreviada = names[i].Item2,
                    DescripcionArticuloExtensa =
                        $"{names[i].Item1}. Producto demo Datamex listo para vitrina B2C/B2B.",
                    IDMarca = (i % 3) + 1,
                    NombreMarca = names[i].Item3 == "Limpieza" ? "CleanPro" : names[i].Item3 == "Cuidado" ? "CarePlus" : "DemoBrand",
                    EstadoEcommerce = i == 7 ? "agotado" : "activo",
                    CodReferenciaProveedor = $"PROV-{(i + 1):D3}",
                    FichaTecnicaUrl = i % 2 == 0 ? "https://example.com/ficha-demo.pdf" : null,
                    VideoUrl = i == 0 ? "https://www.youtube.com/embed/dQw4w9WgXcQ" : null
                });
            }

            db.Articulos.AddRange(articulos);
            await db.SaveChangesAsync(ct);
        }
        else
        {
            foreach (var a in articulos)
            {
                a.NombreMarca ??= a.IDMarca switch
                {
                    2 => "CleanPro",
                    3 => "CarePlus",
                    _ => "DemoBrand"
                };
                a.UltModificacion = now;
            }
            await db.SaveChangesAsync(ct);
        }

        if (!await db.ArticuloEmbalajes.AnyAsync(e => e.IDEmpresa == idEmpresa, ct))
        {
            foreach (var a in articulos)
            {
                db.ArticuloEmbalajes.Add(new ArticuloEmbalaje
                {
                    IDEmpresa = idEmpresa,
                    IDArticulo = a.ID,
                    Activo = true,
                    UltModificacion = now,
                    IDEmbalaje = 1,
                    CantidadxEmbalaje = 1,
                    DescripcionEmbalaje = "Unidad",
                    CodigoBarras = $"779{a.NroArticulo:D10}",
                    PrecioDemo = 10000 + (a.NroArticulo * 1500),
                    EmbalajeParaFacturacion = true,
                    EmbalajeParaControlStock = true
                });
                db.ArticuloEmbalajes.Add(new ArticuloEmbalaje
                {
                    IDEmpresa = idEmpresa,
                    IDArticulo = a.ID,
                    Activo = true,
                    UltModificacion = now,
                    IDEmbalaje = 2,
                    CantidadxEmbalaje = 12,
                    DescripcionEmbalaje = "Caja x12",
                    CodigoBarras = $"780{a.NroArticulo:D10}",
                    PrecioDemo = (10000 + (a.NroArticulo * 1500)) * 11,
                    EmbalajeParaFacturacion = true,
                    EmbalajeParaControlStock = true
                });
            }
        }

        if (!await db.ArticuloImagenes.AnyAsync(i => i.IDEmpresa == idEmpresa, ct))
        {
            foreach (var a in articulos)
            {
                db.ArticuloImagenes.Add(new ArticuloImagen
                {
                    IDEmpresa = idEmpresa,
                    IDArticulo = a.ID,
                    Activo = true,
                    UltModificacion = now,
                    Url = $"https://picsum.photos/seed/datamex{a.NroArticulo}/640/640",
                    Titulo = a.DescripcionArticuloAbreviada,
                    Orden = 1,
                    EsPrincipal = true
                });
                db.ArticuloImagenes.Add(new ArticuloImagen
                {
                    IDEmpresa = idEmpresa,
                    IDArticulo = a.ID,
                    Activo = true,
                    UltModificacion = now,
                    Url = $"https://picsum.photos/seed/datamex{a.NroArticulo}b/640/640",
                    Titulo = $"{a.DescripcionArticuloAbreviada} alt",
                    Orden = 2,
                    EsPrincipal = false
                });
            }
        }

        if (!await db.GruposArticuloEcommerce.AnyAsync(g => g.IDEmpresa == idEmpresa, ct))
        {
            var destacados = new GrupoArticuloEcommerce
            {
                IDEmpresa = idEmpresa,
                Activo = true,
                UltModificacion = now,
                CodGrupoArticulo = 1,
                DescripcionGrupoArticulo = "Destacados",
                FechaInicioVigencia = now.AddYears(-1),
                FechaFinVigencia = now.AddYears(5),
                TipoGrupo = 0,
                Orden = 1,
                Estado = 1,
                Imagen = "https://picsum.photos/seed/coleccion1/800/400"
            };
            var combos = new GrupoArticuloEcommerce
            {
                IDEmpresa = idEmpresa,
                Activo = true,
                UltModificacion = now,
                CodGrupoArticulo = 2,
                DescripcionGrupoArticulo = "Combos y kits",
                FechaInicioVigencia = now.AddYears(-1),
                FechaFinVigencia = now.AddYears(5),
                TipoGrupo = 0,
                Orden = 2,
                Estado = 1,
                Imagen = "https://picsum.photos/seed/coleccion2/800/400"
            };
            db.GruposArticuloEcommerce.AddRange(destacados, combos);
            await db.SaveChangesAsync(ct);

            foreach (var a in articulos.Take(4))
            {
                db.DetallesGrupoArticuloEcommerce.Add(new DetalleGrupoArticuloEcommerce
                {
                    IDEmpresa = idEmpresa,
                    IDGrupoArticuloEcommerce = destacados.ID,
                    IDArticulo = a.ID,
                    Activo = true,
                    UltModificacion = now
                });
            }

            var kit = articulos.FirstOrDefault(a => a.SKU == "SKU-DEMO-006") ?? articulos.Last();
            db.DetallesGrupoArticuloEcommerce.Add(new DetalleGrupoArticuloEcommerce
            {
                IDEmpresa = idEmpresa,
                IDGrupoArticuloEcommerce = combos.ID,
                IDArticulo = kit.ID,
                Activo = true,
                UltModificacion = now
            });
        }

        await db.SaveChangesAsync(ct);
        logger.LogInformation("Catalog enrichment ready for IDEmpresa={Id} (articulos={Count})", idEmpresa, articulos.Count);
    }

    private static async Task EnsurePricingEnrichmentAsync(
        EcomDbContext db, long idEmpresa, ILogger logger, CancellationToken ct)
    {
        var now = DateTime.UtcNow;

        // Actualizar listas viejas sin canal
        var listas = await db.ListasPrecio.Where(l => l.IDEmpresa == idEmpresa).ToListAsync(ct);
        foreach (var l in listas.Where(x => string.IsNullOrWhiteSpace(x.CanalEcommerce)))
        {
            l.CanalEcommerce = l.NroListaPrecio == 2 ? "B2B" : "B2C";
            l.EsListaPrecioEcommerce = true;
            l.PrecioConImpuestos = true;
            l.UltModificacion = now;
        }

        if (!listas.Any(l => l.CanalEcommerce == "B2C" && l.EsListaPrecioEcommerce && l.Activo))
        {
            var b2c = new ListaPrecio
            {
                IDEmpresa = idEmpresa,
                Activo = true,
                UltModificacion = now,
                NroListaPrecio = 1,
                Descripcion = "Lista Ecom B2C",
                EsListaPrecioEcommerce = true,
                PrecioConImpuestos = true,
                FechaRegistro = now,
                FechaInicioVigencia = now.AddYears(-1),
                CanalEcommerce = "B2C",
                Estado = 2
            };
            db.ListasPrecio.Add(b2c);
            listas.Add(b2c);
        }

        if (!listas.Any(l => l.CanalEcommerce == "B2B" && l.EsListaPrecioEcommerce && l.Activo))
        {
            var b2b = new ListaPrecio
            {
                IDEmpresa = idEmpresa,
                Activo = true,
                UltModificacion = now,
                NroListaPrecio = 2,
                Descripcion = "Lista Ecom B2B",
                EsListaPrecioEcommerce = true,
                PrecioConImpuestos = true,
                FechaRegistro = now,
                FechaInicioVigencia = now.AddYears(-1),
                CanalEcommerce = "B2B",
                Estado = 2
            };
            db.ListasPrecio.Add(b2b);
            listas.Add(b2b);
        }

        await db.SaveChangesAsync(ct);

        if (await db.DetallesListaPrecioCondicionVenta.AnyAsync(d => d.IDEmpresa == idEmpresa, ct))
        {
            logger.LogInformation("Pricing details already present for IDEmpresa={Id}", idEmpresa);
            return;
        }

        var listaB2c = listas.First(l => l.CanalEcommerce == "B2C" && l.Activo);
        var listaB2b = listas.First(l => l.CanalEcommerce == "B2B" && l.Activo);
        var embalajes = await db.ArticuloEmbalajes
            .Where(e => e.IDEmpresa == idEmpresa && e.Activo)
            .ToListAsync(ct);

        foreach (var emb in embalajes)
        {
            var basePrice = emb.PrecioDemo ?? (10000m + emb.IDArticulo * 1500m);
            if (emb.CantidadxEmbalaje > 1)
                basePrice = Math.Round(basePrice, 0);

            db.DetallesListaPrecioCondicionVenta.Add(new DetalleListaPrecioCondicionVenta
            {
                IDEmpresa = idEmpresa,
                IDListaPrecio = listaB2c.ID,
                IDArticuloEmbalaje = emb.ID,
                Activo = true,
                UltModificacion = now,
                PrecioContado = basePrice,
                PrecioCheque = basePrice,
                PrecioCredito = Math.Round(basePrice * 1.05m, 0),
                CantidadMinimaMayorista = 1,
                EsAutomatico = true
            });

            var mayorista = Math.Round(basePrice * 0.85m, 0);
            db.DetallesListaPrecioCondicionVenta.Add(new DetalleListaPrecioCondicionVenta
            {
                IDEmpresa = idEmpresa,
                IDListaPrecio = listaB2b.ID,
                IDArticuloEmbalaje = emb.ID,
                Activo = true,
                UltModificacion = now,
                PrecioContado = mayorista,
                PrecioCheque = mayorista,
                PrecioCredito = Math.Round(mayorista * 1.03m, 0),
                CantidadMinimaMayorista = emb.CantidadxEmbalaje > 1 ? 1 : 12,
                EsAutomatico = true
            });
        }

        await db.SaveChangesAsync(ct);
        logger.LogInformation(
            "Pricing enrichment ready for IDEmpresa={Id} (detalles={Count})",
            idEmpresa, embalajes.Count * 2);
    }

    private static async Task EnsureInventoryEnrichmentAsync(
        EcomDbContext db, long idEmpresa, ILogger logger, CancellationToken ct)
    {
        var now = DateTime.UtcNow;
        var config = await db.EmpresaConfiguracionesEcommerce.FirstAsync(c => c.ID == idEmpresa, ct);
        config.ControlaStockEnCarrito = true;
        config.ModoIntegracion = (int)Datamex.ECommerce.Domain.Enums.IntegrationMode.Standalone;

        var tipo = await db.TiposDeposito.FirstOrDefaultAsync(t => t.IDEmpresa == idEmpresa && t.Normal, ct);
        if (tipo is null)
        {
            tipo = new TipoDeposito
            {
                IDEmpresa = idEmpresa,
                Activo = true,
                UltModificacion = now,
                UserName = "seed",
                CodTipoDeposito = 1,
                DescripcionTipoDeposito = "Normal / Facturable",
                Normal = true,
                Consolidado = true,
                Facturable = true
            };
            db.TiposDeposito.Add(tipo);
            await db.SaveChangesAsync(ct);
        }

        var deposito = await db.Depositos.FirstOrDefaultAsync(d => d.IDEmpresa == idEmpresa && d.EsDepositoEcommerce, ct);
        if (deposito is null)
        {
            deposito = new Deposito
            {
                IDEmpresa = idEmpresa,
                Activo = true,
                UltModificacion = now,
                UserName = "seed",
                CodDeposito = 1,
                IDTipoDeposito = tipo.ID,
                DescripcionDeposito = "Depósito Ecom Central",
                DepositoEntrega = true,
                EsDepositoEcommerce = true
            };
            db.Depositos.Add(deposito);
            await db.SaveChangesAsync(ct);
        }
        else if (deposito.IDTipoDeposito == 0)
        {
            deposito.IDTipoDeposito = tipo.ID;
            deposito.UltModificacion = now;
        }

        config.IDDeposito = deposito.ID;
        config.UltModificacion = now;

        var articulos = await db.Articulos
            .Where(a => a.IDEmpresa == idEmpresa && a.ActivoEcommerce)
            .ToListAsync(ct);

        var existing = await db.Stocks
            .Where(e => e.IDEmpresa == idEmpresa && e.IDDeposito == deposito.ID && e.IDContenedor == null && e.IDLoteArticulo == null)
            .Select(e => e.IDArticulo)
            .ToListAsync(ct);
        var existingSet = existing.ToHashSet();

        foreach (var a in articulos)
        {
            if (existingSet.Contains(a.ID))
                continue;

            // Igual semántica ERP: TotalExistencia es lo que ve el ecom.
            var qty = a.EstadoEcommerce == "agotado" ? 0d : 50d + (a.NroArticulo * 7d);
            db.Stocks.Add(new Stock
            {
                IDEmpresa = idEmpresa,
                IDArticulo = a.ID,
                IDDeposito = deposito.ID,
                Activo = true,
                UltModificacion = now,
                UserName = "seed",
                TotalExistencia = qty,
                TotalReserva = 0,
                ExistenciaCongelada = false
            });
        }

        await db.SaveChangesAsync(ct);
        logger.LogInformation(
            "Inventory enrichment ready (tabla stock ERP) for IDEmpresa={Id} deposito={Dep} articulos={Count}",
            idEmpresa, deposito.ID, articulos.Count);
    }

    /// <summary>
    /// Maestros geográficos ERP: DivisionPolitica → Ciudad → Localidad → Distrito → Barrio.
    /// </summary>
    private static async Task<Ciudad> ResolveCiudadAsuncionAsync(
        EcomDbContext db, DateTime now, CancellationToken ct)
    {
        var ciudades = await db.Ciudades.AsNoTracking().Take(500).ToListAsync(ct);
        var match = ciudades.FirstOrDefault(c =>
        {
            var n = (c.DescripcionCiudad ?? string.Empty).Trim();
            return n.Equals("Asunción", StringComparison.OrdinalIgnoreCase)
                   || n.Equals("Asuncion", StringComparison.OrdinalIgnoreCase);
        });
        if (match is not null)
            return await db.Ciudades.FirstAsync(c => c.ID == match.ID, ct);

        var division = await db.DivisionesPoliticas.FirstOrDefaultAsync(ct);
        if (division is null)
        {
            division = new DivisionPolitica
            {
                Activo = true, UltModificacion = now, UserName = "seed",
                CodDivisionPolitica = 1, DescripcionDivisionPolitica = "Central", NombreAbreviado = "CENT", IDPais = 1
            };
            db.DivisionesPoliticas.Add(division);
            await db.SaveChangesAsync(ct);
        }

        var asu = new Ciudad
        {
            Activo = true, UltModificacion = now, UserName = "seed",
            CodCiudad = 1, DescripcionCiudad = "Asunción", IDDivisionPolitica = division.ID
        };
        db.Ciudades.Add(asu);
        await db.SaveChangesAsync(ct);
        return asu;
    }

    private static async Task EnsureGeographyMastersAsync(EcomDbContext db, ILogger logger, CancellationToken ct)
    {
        var now = DateTime.UtcNow;
        if (await db.Ciudades.AnyAsync(ct))
        {
            logger.LogInformation("Geography masters already present");
            return;
        }

        var central = new DivisionPolitica
        {
            Activo = true, UltModificacion = now, UserName = "seed",
            CodDivisionPolitica = 1, DescripcionDivisionPolitica = "Central", NombreAbreviado = "CENT", IDPais = 1
        };
        db.DivisionesPoliticas.Add(central);
        await db.SaveChangesAsync(ct);

        var asu = new Ciudad
        {
            Activo = true, UltModificacion = now, UserName = "seed",
            CodCiudad = 1, DescripcionCiudad = "Asunción", IDDivisionPolitica = central.ID
        };
        var luque = new Ciudad
        {
            Activo = true, UltModificacion = now, UserName = "seed",
            CodCiudad = 2, DescripcionCiudad = "Luque", IDDivisionPolitica = central.ID
        };
        var sanLo = new Ciudad
        {
            Activo = true, UltModificacion = now, UserName = "seed",
            CodCiudad = 3, DescripcionCiudad = "San Lorenzo", IDDivisionPolitica = central.ID
        };
        db.Ciudades.AddRange(asu, luque, sanLo);
        await db.SaveChangesAsync(ct);

        async Task<(Localidad loc, Distrito dist)> ChainAsync(Ciudad ciudad, string barrioNamesCsv, int startCod)
        {
            var loc = new Localidad
            {
                Activo = true, UltModificacion = now, UserName = "seed",
                CodLocalidad = ciudad.CodCiudad, DescripcionLocalidad = $"Loc {ciudad.DescripcionCiudad}",
                IDCiudad = ciudad.ID
            };
            db.Localidades.Add(loc);
            await db.SaveChangesAsync(ct);

            var dist = new Distrito
            {
                Activo = true, UltModificacion = now, UserName = "seed",
                CodDistrito = ciudad.CodCiudad, DescripcionDistrito = $"Dist {ciudad.DescripcionCiudad}",
                IDLocalidad = loc.ID
            };
            db.Distritos.Add(dist);
            await db.SaveChangesAsync(ct);

            var names = barrioNamesCsv.Split(',');
            for (var i = 0; i < names.Length; i++)
            {
                db.Barrios.Add(new Barrio
                {
                    Activo = true, UltModificacion = now, UserName = "seed",
                    CodBarrio = startCod + i,
                    DescripcionBarrio = names[i].Trim(),
                    IDDistrito = dist.ID
                });
            }
            await db.SaveChangesAsync(ct);
            return (loc, dist);
        }

        await ChainAsync(asu, "Centro,Villa Morra,Las Mercedes", 1);
        await ChainAsync(luque, "Centro", 10);
        await ChainAsync(sanLo, "Centro", 20);

        logger.LogInformation("Geography masters seeded (ciudades={C} barrios={B})",
            await db.Ciudades.CountAsync(ct), await db.Barrios.CountAsync(ct));
    }

    /// <summary>
    /// Artículo delivery + tarifas ligadas a barrio/ciudad + tipodocumento + agencia + cuenta demo.
    /// </summary>
    private static async Task EnsureShippingAndAccountEnrichmentAsync(
        EcomDbContext db, long idEmpresa, ILogger logger, CancellationToken ct)
    {
        var now = DateTime.UtcNow;
        var config = await db.EmpresaConfiguracionesEcommerce.FirstAsync(c => c.ID == idEmpresa, ct);
        config.RealizaDelivery = true;

        if (!await db.TiposDocumento.AnyAsync(t => t.IDEmpresa == idEmpresa, ct))
        {
            db.TiposDocumento.AddRange(
                new TipoDocumento
                {
                    IDEmpresa = idEmpresa, Activo = true, UltModificacion = now, UserName = "seed",
                    CodTipoDocumento = 1, DescripcionTipoDocumento = "RUC", EsRuc = true
                },
                new TipoDocumento
                {
                    IDEmpresa = idEmpresa, Activo = true, UltModificacion = now, UserName = "seed",
                    CodTipoDocumento = 2, DescripcionTipoDocumento = "Cédula de Identidad", EsRuc = false
                });
            await db.SaveChangesAsync(ct);
        }

        var depositoId = config.IDDeposito;
        // Tras sync ERP las ciudades suelen venir en MAYÚSCULAS / con padding ("ASUNCION   ").
        var asu = await ResolveCiudadAsuncionAsync(db, now, ct);
        if (!await db.Agencias.AnyAsync(a => a.IDEmpresa == idEmpresa, ct))
        {
            db.Agencias.Add(new Agencia
            {
                IDEmpresa = idEmpresa, Activo = true, UltModificacion = now, UserName = "seed",
                CodAgencia = 1,
                DescripcionAgencia = "Casa Central Demo",
                Direccion = "Av. España c/ Brasil",
                IDCiudad = asu.ID,
                IDDeposito = depositoId,
                Latitud = -25.2865m,
                Longitud = -57.6470m,
                EsSucursalEcommerce = true
            });
            await db.SaveChangesAsync(ct);
        }

        var agenciaSeed = await db.Agencias.FirstAsync(a => a.IDEmpresa == idEmpresa, ct);
        if (!await db.HorariosAtencion.AnyAsync(h => h.IDEmpresa == idEmpresa, ct))
        {
            // Lun–Vie 08:00–12:00 / 14:00–18:00; Sáb 08:00–12:00
            for (byte dia = 1; dia <= 5; dia++)
            {
                db.HorariosAtencion.Add(new HorarioAtencion
                {
                    IDEmpresa = idEmpresa,
                    Activo = true,
                    UltModificacion = now,
                    UserName = "seed",
                    IDAgencia = agenciaSeed.ID,
                    DiaSemana = dia,
                    HoraPrimeraApertura = new TimeSpan(8, 0, 0),
                    HoraPrimerCierre = new TimeSpan(12, 0, 0),
                    HoraSegundaApertura = new TimeSpan(14, 0, 0),
                    HoraSegundoCierre = new TimeSpan(18, 0, 0)
                });
            }
            db.HorariosAtencion.Add(new HorarioAtencion
            {
                IDEmpresa = idEmpresa,
                Activo = true,
                UltModificacion = now,
                UserName = "seed",
                IDAgencia = agenciaSeed.ID,
                DiaSemana = 6,
                HoraPrimeraApertura = new TimeSpan(8, 0, 0),
                HoraPrimerCierre = new TimeSpan(12, 0, 0),
                HoraSegundaApertura = TimeSpan.Zero,
                HoraSegundoCierre = TimeSpan.Zero
            });
            await db.SaveChangesAsync(ct);
        }

        var delivery = await db.Articulos.FirstOrDefaultAsync(
            a => a.IDEmpresa == idEmpresa && a.SKU == "SKU-DELIVERY", ct);

        if (delivery is null)
        {
            delivery = new Articulo
            {
                IDEmpresa = idEmpresa,
                Activo = true,
                ActivoEcommerce = false,
                UltModificacion = now,
                UserName = "seed",
                NroArticulo = 9001,
                SKU = "SKU-DELIVERY",
                DescripcionArticulo = "Costo de Delivery",
                DescripcionArticuloAbreviada = "Delivery",
                DescripcionArticuloExtensa = "Artículo de servicio para tarifa de delivery (ERP IDArticuloDelivery).",
                IDMarca = 1,
                NombreMarca = "DemoBrand",
                EstadoEcommerce = "oculto"
            };
            db.Articulos.Add(delivery);
            await db.SaveChangesAsync(ct);

            db.ArticuloEmbalajes.Add(new ArticuloEmbalaje
            {
                IDEmpresa = idEmpresa,
                IDArticulo = delivery.ID,
                Activo = true,
                UltModificacion = now,
                CantidadxEmbalaje = 1,
                DescripcionEmbalaje = "Unidad",
                CodigoBarras = "DELIVERY-1",
                PrecioDemo = 15000m
            });
            await db.SaveChangesAsync(ct);
        }

        config.IDArticuloDelivery = delivery.ID;
        config.UltModificacion = now;

        if (!await db.TarifasDelivery.AnyAsync(t => t.IDEmpresa == idEmpresa, ct))
        {
            var barrioRows = await (
                from b in db.Barrios
                join d in db.Distritos on b.IDDistrito equals d.ID
                join l in db.Localidades on d.IDLocalidad equals l.ID
                join c in db.Ciudades on l.IDCiudad equals c.ID
                where b.Activo
                select new { Barrio = b, Ciudad = c }
            ).ToListAsync(ct);

            var prices = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase)
            {
                ["Asunción|Centro"] = 15000m,
                ["Asunción|Villa Morra"] = 18000m,
                ["Asunción|Las Mercedes"] = 16000m,
                ["Luque|Centro"] = 22000m,
                ["San Lorenzo|Centro"] = 25000m
            };

            var agencia = await db.Agencias.FirstAsync(a => a.IDEmpresa == idEmpresa, ct);

            foreach (var row in barrioRows)
            {
                var key = $"{row.Ciudad.DescripcionCiudad}|{row.Barrio.DescripcionBarrio}";
                if (!prices.TryGetValue(key, out var monto))
                    continue;

                db.TarifasDelivery.Add(new TarifaDelivery
                {
                    IDEmpresa = idEmpresa,
                    Activo = true,
                    UltModificacion = now,
                    UserName = "seed",
                    IDDivisionPolitica = row.Ciudad.IDDivisionPolitica,
                    IDCiudad = row.Ciudad.ID,
                    IDBarrio = row.Barrio.ID,
                    NombreCiudad = row.Ciudad.DescripcionCiudad,
                    NombreBarrio = row.Barrio.DescripcionBarrio,
                    ImporteDelivery = monto,
                    FechaInicioVigencia = now.Date.AddYears(-1),
                    FechaFinVigencia = now.Date.AddYears(5),
                    Estado = 1, // CERRADO / autorizado (vigente en storefront)
                    IDAgencia = agencia.ID
                });
            }
            // Una tarifa pendiente para demo de autorización
            var pendingBarrio = barrioRows.FirstOrDefault(r =>
                r.Ciudad.DescripcionCiudad == "Asunción"
                && !prices.ContainsKey($"{r.Ciudad.DescripcionCiudad}|{r.Barrio.DescripcionBarrio}"));
            // Si todas tienen precio, clonar una como pendiente editando la última agregada no es trivial;
            // forzar una segunda fila ABIERTO sobre un barrio ya cargado con importe distinto.
            var demoPend = barrioRows.FirstOrDefault();
            if (demoPend is not null)
            {
                db.TarifasDelivery.Add(new TarifaDelivery
                {
                    IDEmpresa = idEmpresa,
                    Activo = true,
                    UltModificacion = now,
                    UserName = "seed",
                    IDDivisionPolitica = demoPend.Ciudad.IDDivisionPolitica,
                    IDCiudad = demoPend.Ciudad.ID,
                    IDBarrio = demoPend.Barrio.ID,
                    NombreCiudad = demoPend.Ciudad.DescripcionCiudad,
                    NombreBarrio = $"{demoPend.Barrio.DescripcionBarrio} (pendiente auth)",
                    ImporteDelivery = 35000,
                    FechaInicioVigencia = now.Date.AddYears(-1),
                    FechaFinVigencia = now.Date.AddYears(5),
                    Estado = 0, // ABIERTO pendiente
                    IDAgencia = agencia.ID
                });
            }
            await db.SaveChangesAsync(ct);
        }

        var user = await db.UsuariosEcommerce
            .FirstOrDefaultAsync(u => u.IDEmpresa == idEmpresa && u.Usuario == DemoUser, ct);
        if (user is not null)
        {
            var ci = await db.TiposDocumento.FirstAsync(t => t.IDEmpresa == idEmpresa && t.CodTipoDocumento == 2, ct);
            user.IDTipoDocumento ??= ci.ID;
            user.NumeroDocumento ??= "1234567";
            user.NumeroTelefono ??= "021123456";
            user.NumeroMovil ??= "0981123456";
            user.Activado = true;
            user.Activo = true;

            if (!await db.Cuentas.AnyAsync(c => c.IDEmpresa == idEmpresa && c.NroCuenta == 1, ct))
            {
                var lista = await db.ListasPrecio.AsNoTracking()
                    .Where(l => l.IDEmpresa == idEmpresa && l.CanalEcommerce == "B2C")
                    .Select(l => (long?)l.ID)
                    .FirstOrDefaultAsync(ct);
                var agencia = await db.Agencias.FirstAsync(a => a.IDEmpresa == idEmpresa, ct);
                var barrioCentro = await (
                    from b in db.Barrios
                    join d in db.Distritos on b.IDDistrito equals d.ID
                    join l in db.Localidades on d.IDLocalidad equals l.ID
                    where l.IDCiudad == asu.ID
                    select b
                ).FirstOrDefaultAsync(ct);
                if (barrioCentro is null)
                {
                    // Sync ERP puede traer ciudad sin cadena localidad/distrito/barrio "Centro".
                    var loc = await db.Localidades.FirstOrDefaultAsync(l => l.IDCiudad == asu.ID, ct);
                    if (loc is null)
                    {
                        loc = new Localidad
                        {
                            Activo = true, UltModificacion = now, UserName = "seed",
                            CodLocalidad = 1, DescripcionLocalidad = "Loc Asunción", IDCiudad = asu.ID
                        };
                        db.Localidades.Add(loc);
                        await db.SaveChangesAsync(ct);
                    }

                    var dist = await db.Distritos.FirstOrDefaultAsync(d => d.IDLocalidad == loc.ID, ct);
                    if (dist is null)
                    {
                        dist = new Distrito
                        {
                            Activo = true, UltModificacion = now, UserName = "seed",
                            CodDistrito = 1, DescripcionDistrito = "Dist Asunción", IDLocalidad = loc.ID
                        };
                        db.Distritos.Add(dist);
                        await db.SaveChangesAsync(ct);
                    }

                    barrioCentro = new Barrio
                    {
                        Activo = true, UltModificacion = now, UserName = "seed",
                        CodBarrio = 1, DescripcionBarrio = "Centro", IDDistrito = dist.ID
                    };
                    db.Barrios.Add(barrioCentro);
                    await db.SaveChangesAsync(ct);
                }

                db.Cuentas.Add(new Cuenta
                {
                    IDEmpresa = idEmpresa,
                    Activo = true,
                    UltModificacion = now,
                    UserName = "seed",
                    NroCuenta = 1,
                    RazonSocial = "DEMO USUARIO",
                    NombreCuenta = user.Nombre,
                    ApellidoCuenta = user.Apellido,
                    Email = user.Email,
                    Direccion = "Av. Demo 100",
                    IDCiudad = asu.ID,
                    IDBarrio = barrioCentro.ID,
                    IDDivisionPolitica = asu.IDDivisionPolitica,
                    IDListaPrecio = lista,
                    IDAgencia = agencia.ID,
                    Latitud = -25.2865m,
                    Longitud = -57.6470m
                });
                await db.SaveChangesAsync(ct);
            }

            var cuenta = await db.Cuentas.FirstAsync(c => c.IDEmpresa == idEmpresa && c.NroCuenta == 1, ct);
            if (!await db.UsuariosEcommerceCuenta.AnyAsync(
                    c => c.IDEmpresa == idEmpresa && c.IDUsuarioEcommerce == user.ID, ct))
            {
                db.UsuariosEcommerceCuenta.Add(new UsuarioEcommerceCuenta
                {
                    IDEmpresa = idEmpresa,
                    IDUsuarioEcommerce = user.ID,
                    IDCuenta = cuenta.ID,
                    EsPrincipal = true,
                    Activo = true,
                    UltModificacion = now,
                    UserName = "seed"
                });
            }

            if (cuenta.IDListaPrecio is long idListaSeed and > 0
                && !await db.ListasPrecioCuenta.AnyAsync(
                    x => x.IDEmpresa == idEmpresa && x.IDCuenta == cuenta.ID && x.IDListaPrecio == idListaSeed, ct))
            {
                db.ListasPrecioCuenta.Add(new ListaPrecioCuenta
                {
                    IDEmpresa = idEmpresa,
                    IDCuenta = cuenta.ID,
                    IDListaPrecio = idListaSeed,
                    Activo = true,
                    UltModificacion = now,
                    UserName = "seed"
                });
            }
        }

        await db.SaveChangesAsync(ct);
        logger.LogInformation(
            "Shipping/account enrichment ready for IDEmpresa={Id} IDArticuloDelivery={Art} tarifas={Tar}",
            idEmpresa, delivery.ID, await db.TarifasDelivery.CountAsync(t => t.IDEmpresa == idEmpresa, ct));
    }

    /// <summary>
    /// Alinea estados legacy tras agregar ListaPrecio.Estado y cambio de semántica de tarifas
    /// (storefront usa Estado=1 autorizado).
    /// </summary>
    private static async Task EnsureAuthorizationEstadosAsync(
        EcomDbContext db, long idEmpresa, ILogger logger, CancellationToken ct)
    {
        // One-shot post-migración: columna Estado default=0. No tocar Estado=1 (pendientes intencionales).
        var listas = await db.ListasPrecio
            .Where(l => l.IDEmpresa == idEmpresa && l.EsListaPrecioEcommerce && l.Activo && l.Estado == 0)
            .ToListAsync(ct);
        foreach (var l in listas)
            l.Estado = 2; // AUTORIZADO (vigente)

        var tarifas = await db.TarifasDelivery
            .Where(t => t.IDEmpresa == idEmpresa && t.Activo && t.Estado == 0
                        && !t.NombreBarrio.Contains("pendiente"))
            .ToListAsync(ct);
        foreach (var t in tarifas)
            t.Estado = 1; // CERRADO / autorizado

        if (listas.Count > 0 || tarifas.Count > 0)
        {
            await db.SaveChangesAsync(ct);
            logger.LogInformation(
                "Authorization estados aligned (listas={L}, tarifas={T}) IDEmpresa={Id}",
                listas.Count, tarifas.Count, idEmpresa);
        }
    }

    public static async Task EnsureBackofficeRbacAsync(
        EcomDbContext db,
        long idEmpresa,
        ILogger logger,
        CancellationToken ct)
    {
        const string superRolCodigo = "SUPER";
        const string permAll = "manager:all";
        const string adminUser = "admin";
        const string adminPass = "Admin12!";

        var superRol = await db.AdminRoles
            .FirstOrDefaultAsync(r => r.IDEmpresa == idEmpresa && r.CodigoRol == superRolCodigo, ct);
        if (superRol is null)
        {
            superRol = new AdminRol
            {
                IDEmpresa = idEmpresa,
                CodigoRol = superRolCodigo,
                NombreRol = "Super Admin",
                Activo = true,
                UltModificacion = DateTime.UtcNow,
                UserName = "seed"
            };
            db.AdminRoles.Add(superRol);
            await db.SaveChangesAsync(ct);
        }

        var perm = await db.AdminFuncionalidades
            .FirstOrDefaultAsync(f => f.IDEmpresa == idEmpresa && f.CodigoPermission == permAll, ct);
        if (perm is null)
        {
            perm = new AdminFuncionalidad
            {
                IDEmpresa = idEmpresa,
                CodigoPermission = permAll,
                Descripcion = "Acceso total manager",
                Activo = true,
                UltModificacion = DateTime.UtcNow,
                UserName = "seed"
            };
            db.AdminFuncionalidades.Add(perm);
            await db.SaveChangesAsync(ct);
        }

        var admin = await db.AdminUsuarios
            .FirstOrDefaultAsync(u => u.IDEmpresa == idEmpresa && u.Usuario == adminUser, ct);
        if (admin is null)
        {
            admin = new AdminUsuario
            {
                IDEmpresa = idEmpresa,
                Usuario = adminUser,
                PasswordHash = JwtAuthService.HashPassword(adminPass),
                Nombre = "Admin",
                Email = "admin@demo.local",
                Activo = true,
                UltModificacion = DateTime.UtcNow,
                UserName = "seed"
            };
            db.AdminUsuarios.Add(admin);
            await db.SaveChangesAsync(ct);
        }

        var hasUserRole = await db.AdminUsuariosRoles.AnyAsync(
            ur => ur.IDEmpresa == idEmpresa && ur.IDUsuario == admin.ID && ur.IDRol == superRol.ID,
            ct);
        if (!hasUserRole)
        {
            db.AdminUsuariosRoles.Add(new AdminUsuarioRol
            {
                IDEmpresa = idEmpresa,
                IDUsuario = admin.ID,
                IDRol = superRol.ID,
                Activo = true,
                UltModificacion = DateTime.UtcNow,
                UserName = "seed"
            });
            await db.SaveChangesAsync(ct);
        }

        var hasRolePerm = await db.AdminRolFuncionalidades.AnyAsync(
            rf => rf.IDEmpresa == idEmpresa && rf.IDRol == superRol.ID && rf.IDFuncionalidad == perm.ID,
            ct);
        if (!hasRolePerm)
        {
            db.AdminRolFuncionalidades.Add(new AdminRolFuncionalidad
            {
                IDEmpresa = idEmpresa,
                IDRol = superRol.ID,
                IDFuncionalidad = perm.ID,
                Activo = true,
                UltModificacion = DateTime.UtcNow,
                UserName = "seed"
            });
            await db.SaveChangesAsync(ct);
        }

        // Catálogo de PermissionCodes (espejo Site_App_Ecommerce_* del ERP)
        var catalog = new (string Code, string Desc)[]
        {
            ("Site_App_Ecommerce", "Acceso e-commerce"),
            ("Site_App_Ecommerce_Empresa", "Configuración empresa / tienda"),
            ("Site_App_Ecommerce_Personalizacion", "Personalización marketing (layout/temas/textos)"),
            ("Site_App_Ecommerce_Empresa_Agencia", "Agencias / sucursales"),
            ("Site_App_Ecommerce_Empresa_HorarioAtencion", "Horarios de atención"),
            ("Site_App_Ecommerce_Productos", "Productos / artículos"),
            ("Site_App_Ecommerce_Productos_Add", "Alta de productos"),
            ("Site_App_Ecommerce_Productos_Edit", "Editar productos"),
            ("Site_App_Ecommerce_Productos_Del", "Eliminar productos"),
            ("Site_App_Ecommerce_Productos_Import", "Importar productos"),
            ("Site_App_Ecommerce_Productos_ParametrosProductos", "Parámetros de producto"),
            ("Site_App_Ecommerce_Productos_Stock_Edit", "Editar stock"),
            ("Site_App_Ecommerce_Productos_Imagenes", "Imágenes de productos"),
            ("Site_App_Ecommerce_Productos_ActivarInactivar", "Activar / inactivar productos"),
            ("Site_App_Ecommerce_GrupoArticulo", "Promociones / grupos vitrina"),
            ("Site_App_Ecommerce_TarifaDelivery", "Tarifas delivery"),
            ("Site_App_Ecommerce_TarifaDelivery_AutorizacionAnulacion", "Autorizar / anular tarifas"),
            ("Site_App_Ecommerce_TarifaDelivery_Cancelacion", "Cancelar tarifas autorizadas"),
            ("Site_App_Ecommerce_Pedidos", "Pedidos"),
            ("Site_App_Ecommerce_Pedidos_View", "Ver pedido"),
            ("Site_App_Ecommerce_ListasPrecios", "Listas de precios"),
            ("Site_App_Ecommerce_ListasPrecios_ListaPrecio", "Listas de precio"),
            ("Site_App_Ecommerce_ListasPrecios_ListaPrecio_Add", "Alta lista de precio"),
            ("Site_App_Ecommerce_ListasPrecios_ListaPrecio_Edit", "Editar lista de precio"),
            ("Site_App_Ecommerce_ListasPrecios_ListaPrecio_Del", "Eliminar lista de precio"),
            ("Site_App_Ecommerce_ListasPrecios_ListaPrecio_View", "Ver lista de precio"),
            ("Site_App_Ecommerce_ListasPrecios_DetListPrecioCondicionVenta", "Detalle lista condición venta"),
            ("Site_App_Ecommerce_ListasPrecios_DetListPrecioCondicionVenta_Add", "Alta/editar detalle lista"),
            ("Site_App_Ecommerce_ListasPrecios_AutDetListPrecio", "Autorizar / anular listas de precio"),
            ("Site_App_Ecommerce_Clientes", "Clientes e-commerce"),
            ("Site_App_Ecommerce_Clientes_ClientesCuentas", "Clientes / cuentas"),
            ("Site_App_Ecommerce_Clientes_ClientesCuentas_View", "Ver cliente"),
            ("Site_App_Ecommerce_Clientes_ClientesCuentas_Add", "Alta cliente"),
            ("Site_App_Ecommerce_Clientes_ClientesCuentas_Edit", "Editar cliente"),
            ("Site_App_Ecommerce_Clientes_ClientesCuentas_Del", "Eliminar cliente"),
            ("Site_App_Ecommerce_Clientes_ClientesCuentas_AsigListaPrecio", "Asignar lista de precio"),
            ("Site_App_Ecommerce_CarritoCompras", "Carritos de compra"),
            ("Site_App_Ecommerce_CarritoCompras_Trazabilidad", "Trazabilidad carrito"),
            ("Site_App_Ecommerce_CarritoCompras_Exportacion", "Exportación carritos"),
            ("Site_App_Ecommerce_Resenas", "Reseñas / calificaciones"),
            ("Site_App_Ecommerce_Cupones", "Cupones promocionales"),
            ("Site_App_Ecommerce_Devoluciones", "Devoluciones / RMA"),
            ("Site_App_Ecommerce_Chat", "Chat / soporte in-app"),
            ("manager:roles", "Manager roles"),
            ("manager:funcionalidades", "Manager funcionalidades"),
            ("manager:usuarios", "Manager usuarios"),
            ("manager:tenants", "Alta empresas"),
        };

        foreach (var (code, desc) in catalog)
        {
            if (await db.AdminFuncionalidades.AnyAsync(
                    f => f.IDEmpresa == idEmpresa && f.CodigoPermission == code, ct))
                continue;

            db.AdminFuncionalidades.Add(new AdminFuncionalidad
            {
                IDEmpresa = idEmpresa,
                CodigoPermission = code,
                Descripcion = desc,
                Activo = true,
                UltModificacion = DateTime.UtcNow,
                UserName = "seed"
            });
        }
        await db.SaveChangesAsync(ct);

        // Jerarquía IDFuncionalidadPadre (paridad Operacion.IDOperacionPadre)
        var allFuncs = await db.AdminFuncionalidades
            .Where(f => f.IDEmpresa == idEmpresa && f.Activo)
            .ToListAsync(ct);
        AdminPermissionTreeBuilder.WireParentsByCodePrefix(allFuncs);
        await db.SaveChangesAsync(ct);

        logger.LogInformation(
            "Backoffice RBAC ready (IDEmpresa={Id} admin={User} ops={Ops})",
            idEmpresa,
            adminUser,
            allFuncs.Count);
    }

    private static async Task EnsureUnidadesMedidaAsync(
        EcomDbContext db, long idEmpresa, ILogger logger, CancellationToken ct)
    {
        var defaults = new (string Codigo, string Nombre, string Simbolo, bool Decimales, decimal Inc, int Orden)[]
        {
            ("UN", "Unidad", "u", false, 1, 1),
            ("M", "Metro", "m", true, 0.5m, 2),
            ("M2", "Metro cuadrado", "m²", true, 0.25m, 3),
            ("KG", "Kilogramo", "kg", true, 0.1m, 4),
            ("L", "Litro", "L", true, 0.25m, 5),
            ("CAJA", "Caja", "cj", false, 1, 6),
            ("ROLLO", "Rollo", "rl", false, 1, 7),
        };

        var existing = await db.UnidadesMedidaEcommerce.AsNoTracking()
            .Where(u => u.IDEmpresa == idEmpresa)
            .Select(u => u.Codigo)
            .ToListAsync(ct);
        var set = existing.ToHashSet(StringComparer.OrdinalIgnoreCase);
        var now = DateTime.UtcNow;
        foreach (var d in defaults)
        {
            if (set.Contains(d.Codigo)) continue;
            db.UnidadesMedidaEcommerce.Add(new UnidadMedidaEcommerce
            {
                IDEmpresa = idEmpresa,
                Codigo = d.Codigo,
                Nombre = d.Nombre,
                Simbolo = d.Simbolo,
                PermiteDecimales = d.Decimales,
                IncrementoDefault = d.Inc,
                Orden = d.Orden,
                Activo = true,
                UltModificacion = now,
                UserName = "seed"
            });
        }
        await db.SaveChangesAsync(ct);

        // Ejemplo: marcar un embalaje de tela/alfombra como metro si existe descripción
        var metroEmb = await db.ArticuloEmbalajes
            .Where(e => e.IDEmpresa == idEmpresa && e.Activo
                        && (e.DescripcionEmbalaje != null && (
                            e.DescripcionEmbalaje.ToLower().Contains("metro")
                            || e.DescripcionEmbalaje.ToLower().Contains("tela")
                            || e.DescripcionEmbalaje.ToLower().Contains("alfombra"))))
            .ToListAsync(ct);
        foreach (var e in metroEmb)
        {
            e.CodigoUnidadMedida = "M";
            e.PermiteFraccion = true;
            e.IncrementoVenta = 0.5m;
            e.CantidadMinimaVenta = 1;
            e.UltModificacion = now;
        }
        if (metroEmb.Count == 0)
        {
            // Si no hay match, toma el primer embalaje de algún artículo demo y deja UN; crea uno M de ejemplo
            var anyArt = await db.Articulos.AsNoTracking()
                .Where(a => a.IDEmpresa == idEmpresa && a.ActivoEcommerce)
                .Select(a => a.ID)
                .FirstOrDefaultAsync(ct);
            if (anyArt > 0)
            {
                var hasM = await db.ArticuloEmbalajes.AnyAsync(
                    e => e.IDEmpresa == idEmpresa && e.IDArticulo == anyArt && e.CodigoUnidadMedida == "M", ct);
                if (!hasM)
                {
                    var next = ((await db.ArticuloEmbalajes.AsNoTracking()
                        .Where(e => e.IDEmpresa == idEmpresa && e.IDArticulo == anyArt)
                        .MaxAsync(e => (long?)e.IDEmbalaje, ct)) ?? 0) + 1;
                    db.ArticuloEmbalajes.Add(new ArticuloEmbalaje
                    {
                        IDEmpresa = idEmpresa,
                        IDArticulo = anyArt,
                        IDEmbalaje = next,
                        CantidadxEmbalaje = 1,
                        DescripcionEmbalaje = "Por metro",
                        CodigoUnidadMedida = "M",
                        PermiteFraccion = true,
                        IncrementoVenta = 0.5m,
                        CantidadMinimaVenta = 1,
                        EmbalajeParaFacturacion = true,
                        EmbalajeParaControlStock = true,
                        PrecioDemo = 45000,
                        Activo = true,
                        UltModificacion = now,
                        UserName = "seed"
                    });
                }
            }
        }
        await db.SaveChangesAsync(ct);
        logger.LogInformation("Unidades de medida ensured for IDEmpresa={Id}", idEmpresa);
    }

    private static async Task EnsureMarketingDemoAsync(
        EcomDbContext db, long idEmpresa, ILogger logger, CancellationToken ct)
    {
        var now = DateTime.UtcNow;
        // Rol Marketing
        var mkt = await db.AdminRoles.FirstOrDefaultAsync(
            r => r.IDEmpresa == idEmpresa && r.CodigoRol == "MARKETING", ct);
        if (mkt is null)
        {
            mkt = new AdminRol
            {
                IDEmpresa = idEmpresa,
                CodigoRol = "MARKETING",
                NombreRol = "Marketing",
                Activo = true,
                UltModificacion = now,
                UserName = "seed"
            };
            db.AdminRoles.Add(mkt);
            await db.SaveChangesAsync(ct);
        }

        var perms = new[]
        {
            "Site_App_Ecommerce_GrupoArticulo",
            "Site_App_Ecommerce_Cupones",
            "Site_App_Ecommerce_Empresa",
            "Site_App_Ecommerce_Personalizacion"
        };
        foreach (var code in perms)
        {
            var f = await db.AdminFuncionalidades.FirstOrDefaultAsync(
                x => x.IDEmpresa == idEmpresa && x.CodigoPermission == code, ct);
            if (f is null) continue;
            var has = await db.AdminRolFuncionalidades.AnyAsync(
                rf => rf.IDEmpresa == idEmpresa && rf.IDRol == mkt.ID && rf.IDFuncionalidad == f.ID, ct);
            if (has) continue;
            db.AdminRolFuncionalidades.Add(new AdminRolFuncionalidad
            {
                IDEmpresa = idEmpresa,
                IDRol = mkt.ID,
                IDFuncionalidad = f.ID,
                Activo = true,
                UltModificacion = now,
                UserName = "seed"
            });
        }
        await db.SaveChangesAsync(ct);

        var hasFlyer = await db.GruposArticuloEcommerce.AnyAsync(
            g => g.IDEmpresa == idEmpresa && g.TipoGrupo == 3 && g.Activo, ct);
        if (!hasFlyer)
        {
            var maxCod = await db.GruposArticuloEcommerce.AsNoTracking()
                .Where(g => g.IDEmpresa == idEmpresa)
                .MaxAsync(g => (int?)g.CodGrupoArticulo, ct) ?? 0;
            db.GruposArticuloEcommerce.Add(new GrupoArticuloEcommerce
            {
                IDEmpresa = idEmpresa,
                CodGrupoArticulo = maxCod + 1,
                DescripcionGrupoArticulo = "Oferta de la semana",
                Subtitulo = "Descuentos activos en productos seleccionados",
                TipoGrupo = 3,
                Imagen = "https://images.unsplash.com/photo-1607082348824-0a96f2a4b9da?w=1200&q=80",
                UrlDestino = "/catalogo",
                FechaInicioVigencia = now.AddDays(-1),
                FechaFinVigencia = now.AddDays(30),
                Estado = 1,
                Orden = 1,
                Activo = true,
                UltModificacion = now,
                UserName = "seed"
            });
        }

        var hasAd = await db.GruposArticuloEcommerce.AnyAsync(
            g => g.IDEmpresa == idEmpresa && g.TipoGrupo == 2 && g.Activo, ct);
        if (!hasAd)
        {
            var maxCod = await db.GruposArticuloEcommerce.AsNoTracking()
                .Where(g => g.IDEmpresa == idEmpresa)
                .MaxAsync(g => (int?)g.CodGrupoArticulo, ct) ?? 0;
            db.GruposArticuloEcommerce.Add(new GrupoArticuloEcommerce
            {
                IDEmpresa = idEmpresa,
                CodGrupoArticulo = maxCod + 1,
                DescripcionGrupoArticulo = "Novedades de temporada",
                Subtitulo = "Publicidad marketing",
                TipoGrupo = 2,
                Imagen = "https://images.unsplash.com/photo-1441986300917-64674bd600d8?w=1200&q=80",
                UrlDestino = "/catalogo",
                FechaInicioVigencia = now.AddDays(-1),
                FechaFinVigencia = now.AddDays(60),
                Estado = 1,
                Orden = 2,
                Activo = true,
                UltModificacion = now,
                UserName = "seed"
            });
        }
        await db.SaveChangesAsync(ct);
        logger.LogInformation("Marketing role + flyer/ad slots ensured for IDEmpresa={Id}", idEmpresa);
    }
}
