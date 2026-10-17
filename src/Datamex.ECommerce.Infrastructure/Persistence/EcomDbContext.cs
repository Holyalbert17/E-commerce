using Datamex.ECommerce.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Datamex.ECommerce.Infrastructure.Persistence;

public class EcomDbContext : DbContext
{
    public EcomDbContext(DbContextOptions<EcomDbContext> options) : base(options)
    {
    }

    public DbSet<Empresa> Empresas => Set<Empresa>();
    public DbSet<EmpresaConfiguracionEcommerce> EmpresaConfiguracionesEcommerce => Set<EmpresaConfiguracionEcommerce>();
    public DbSet<EmpresaConfiguracionEcommerceRedSocial> EmpresaConfiguracionEcommerceRedSociales =>
        Set<EmpresaConfiguracionEcommerceRedSocial>();
    public DbSet<TiendaEcommerce> TiendasEcommerce => Set<TiendaEcommerce>();
    public DbSet<Articulo> Articulos => Set<Articulo>();
    public DbSet<ArticuloEmbalaje> ArticuloEmbalajes => Set<ArticuloEmbalaje>();
    public DbSet<ArticuloEmbalajeCodigoBarra> ArticuloEmbalajeCodigosBarra => Set<ArticuloEmbalajeCodigoBarra>();
    public DbSet<CatalogoParametro> CatalogoParametros => Set<CatalogoParametro>();
    public DbSet<UnidadMedidaEcommerce> UnidadesMedidaEcommerce => Set<UnidadMedidaEcommerce>();
    public DbSet<ArticuloImagen> ArticuloImagenes => Set<ArticuloImagen>();
    public DbSet<ChoferEcommerce> ChoferesEcommerce => Set<ChoferEcommerce>();
    public DbSet<ListaPrecio> ListasPrecio => Set<ListaPrecio>();
    public DbSet<DetalleListaPrecio> DetallesListaPrecio => Set<DetalleListaPrecio>();
    public DbSet<DetalleListaPrecioCondicionVenta> DetallesListaPrecioCondicionVenta => Set<DetalleListaPrecioCondicionVenta>();
    public DbSet<ListaPrecioCuenta> ListasPrecioCuenta => Set<ListaPrecioCuenta>();
    public DbSet<UsuarioEcommerce> UsuariosEcommerce => Set<UsuarioEcommerce>();
    public DbSet<UsuarioEcommerceCuenta> UsuariosEcommerceCuenta => Set<UsuarioEcommerceCuenta>();
    public DbSet<CarritoCompra> CarritosCompra => Set<CarritoCompra>();
    public DbSet<DetalleCarritoCompra> DetallesCarritoCompra => Set<DetalleCarritoCompra>();
    public DbSet<GrupoArticuloEcommerce> GruposArticuloEcommerce => Set<GrupoArticuloEcommerce>();
    public DbSet<DetalleGrupoArticuloEcommerce> DetallesGrupoArticuloEcommerce => Set<DetalleGrupoArticuloEcommerce>();
    public DbSet<Deposito> Depositos => Set<Deposito>();
    public DbSet<TipoDeposito> TiposDeposito => Set<TipoDeposito>();
    public DbSet<Stock> Stocks => Set<Stock>();
    public DbSet<PedidoMovil> PedidosMovil => Set<PedidoMovil>();
    public DbSet<DetallePedidoMovil> DetallesPedidoMovil => Set<DetallePedidoMovil>();
    public DbSet<TarifaDelivery> TarifasDelivery => Set<TarifaDelivery>();
    public DbSet<DivisionPolitica> DivisionesPoliticas => Set<DivisionPolitica>();
    public DbSet<Ciudad> Ciudades => Set<Ciudad>();
    public DbSet<Localidad> Localidades => Set<Localidad>();
    public DbSet<Distrito> Distritos => Set<Distrito>();
    public DbSet<Barrio> Barrios => Set<Barrio>();
    public DbSet<TipoDocumento> TiposDocumento => Set<TipoDocumento>();
    public DbSet<Cuenta> Cuentas => Set<Cuenta>();
    public DbSet<Agencia> Agencias => Set<Agencia>();
    public DbSet<HorarioAtencion> HorariosAtencion => Set<HorarioAtencion>();
    public DbSet<LogPagoTarjeta> LogsPagoTarjeta => Set<LogPagoTarjeta>();

    public DbSet<ResenaArticuloEcommerce> ResenasArticuloEcommerce => Set<ResenaArticuloEcommerce>();
    public DbSet<ListaFavoritoEcommerce> ListasFavoritoEcommerce => Set<ListaFavoritoEcommerce>();
    public DbSet<DetalleListaFavoritoEcommerce> DetallesListaFavoritoEcommerce => Set<DetalleListaFavoritoEcommerce>();
    public DbSet<InteraccionArticuloEcommerce> InteraccionesArticuloEcommerce => Set<InteraccionArticuloEcommerce>();
    public DbSet<CuponEcommerce> CuponesEcommerce => Set<CuponEcommerce>();
    public DbSet<UsoCuponEcommerce> UsosCuponEcommerce => Set<UsoCuponEcommerce>();
    public DbSet<ProductoSeguidoEcommerce> ProductosSeguidosEcommerce => Set<ProductoSeguidoEcommerce>();
    public DbSet<SeguimientoPedidoEcommerce> SeguimientosPedidoEcommerce => Set<SeguimientoPedidoEcommerce>();
    public DbSet<ConfiguracionPersonalizacion> ConfiguracionesPersonalizacion => Set<ConfiguracionPersonalizacion>();
    public DbSet<ContenidoBloque> ContenidoBloques => Set<ContenidoBloque>();
    public DbSet<ContenidoBloqueItem> ContenidoBloqueItems => Set<ContenidoBloqueItem>();
    public DbSet<NavegacionMenu> NavegacionMenus => Set<NavegacionMenu>();
    public DbSet<NavegacionMenuItem> NavegacionMenuItems => Set<NavegacionMenuItem>();
    public DbSet<AdminAuditoriaLog> AdminAuditoriaLogs => Set<AdminAuditoriaLog>();
    public DbSet<InventarioMovimiento> InventarioMovimientos => Set<InventarioMovimiento>();
    public DbSet<DevolucionEcommerce> DevolucionesEcommerce => Set<DevolucionEcommerce>();
    public DbSet<ChatConversacionEcommerce> ChatsConversacionEcommerce => Set<ChatConversacionEcommerce>();
    public DbSet<ChatSoporteMensajeEcommerce> ChatsSoporteMensajeEcommerce => Set<ChatSoporteMensajeEcommerce>();
    public DbSet<SyncCursor> SyncCursors => Set<SyncCursor>();
    public DbSet<MigracionPedidoLog> MigracionesPedidoLog => Set<MigracionPedidoLog>();

    public DbSet<AdminUsuario> AdminUsuarios => Set<AdminUsuario>();
    public DbSet<AdminRol> AdminRoles => Set<AdminRol>();
    public DbSet<AdminFuncionalidad> AdminFuncionalidades => Set<AdminFuncionalidad>();
    public DbSet<AdminUsuarioRol> AdminUsuariosRoles => Set<AdminUsuarioRol>();
    public DbSet<AdminRolFuncionalidad> AdminRolFuncionalidades => Set<AdminRolFuncionalidad>();
    public DbSet<AdminUsuarioFuncionalidad> AdminUsuarioFuncionalidades => Set<AdminUsuarioFuncionalidad>();
    public DbSet<ContactoMensaje> ContactoMensajes => Set<ContactoMensaje>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Paridad ERP: tablas en schema dbo (Postgres). SQLite ignora schemas.
        modelBuilder.HasDefaultSchema("dbo");

        foreach (var entity in modelBuilder.Model.GetEntityTypes())
        {
            entity.SetTableName(entity.ClrType.Name.ToLowerInvariant());
        }

        modelBuilder.Entity<Empresa>(e =>
        {
            e.HasKey(x => x.ID);
            e.Property(x => x.ID).ValueGeneratedNever();
            e.Property(x => x.RazonSocial).HasMaxLength(256).IsRequired();
            e.Property(x => x.NombreFantasia).HasMaxLength(256);
            e.Property(x => x.NumeroDocumento).HasMaxLength(15);
            e.Property(x => x.DireccionEmpresa).HasMaxLength(256);
            e.Property(x => x.Telefono).HasMaxLength(15);
            e.Property(x => x.Fax).HasMaxLength(15);
            e.Property(x => x.EMail).HasMaxLength(256);
            e.Property(x => x.RepresentanteLegal).HasMaxLength(256);
            e.HasOne(x => x.ConfiguracionEcommerce)
                .WithOne(x => x.Empresa!)
                .HasForeignKey<EmpresaConfiguracionEcommerce>(x => x.ID);
        });

        modelBuilder.Entity<EmpresaConfiguracionEcommerce>(e =>
        {
            e.HasKey(x => x.ID);
            e.Property(x => x.ID).ValueGeneratedNever();
            e.Property(x => x.MontoPedidoMinimo).HasPrecision(18, 2);
            e.Property(x => x.MontoUmbralMayorista).HasPrecision(18, 2);
            e.Property(x => x.ClavePublicaProcesadoraTarjeta).HasMaxLength(50);
            e.Property(x => x.ClavePrivadaProcesadoraTarjeta).HasMaxLength(40);
            e.Property(x => x.NombreServidorCorreoElectronico).HasMaxLength(255);
            e.Property(x => x.NombreUsuarioCorreoElectronico).HasMaxLength(255);
            e.Property(x => x.PasswordCorreoElectronico).HasMaxLength(255);
            e.Property(x => x.TiempoAproxEntregaPickup).HasColumnType("time without time zone");
            e.Property(x => x.TiempoAproxEntregaDelivery).HasColumnType("time without time zone");
            e.Property(x => x.SlugTienda).HasMaxLength(100);
            e.HasIndex(x => x.SlugTienda).IsUnique().HasFilter("\"SlugTienda\" IS NOT NULL");
            e.Property(x => x.ErpHost).HasMaxLength(200);
            e.Property(x => x.ErpDatabase).HasMaxLength(120);
            e.Property(x => x.ErpUsername).HasMaxLength(120);
            e.Property(x => x.ErpPasswordProtected).HasMaxLength(500);
            e.Property(x => x.ErpSslMode).HasMaxLength(40);
            e.HasMany(x => x.RedesSociales)
                .WithOne(x => x.EmpresaConfiguracionEcommerce!)
                .HasForeignKey(x => x.IDEmpresaConfiguracionEcommerce)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<EmpresaConfiguracionEcommerceRedSocial>(e =>
        {
            e.HasKey(x => x.ID);
            e.Property(x => x.ID).ValueGeneratedNever();
            e.Property(x => x.NombreRedSocial).HasMaxLength(255).IsRequired();
            e.Property(x => x.DireccionURLRedSocial).IsRequired();
            e.Property(x => x.LogoRedSocial).IsRequired();
            e.HasIndex(x => x.IDEmpresaConfiguracionEcommerce);
        });

        modelBuilder.Entity<TiendaEcommerce>(e =>
        {
            e.HasKey(x => x.ID);
            e.HasIndex(x => new { x.IDEmpresa, x.TokenTienda }).IsUnique();
            e.Property(x => x.TokenTienda).HasMaxLength(100).IsRequired();
        });

        modelBuilder.Entity<Articulo>(e =>
        {
            e.HasKey(x => x.ID);
            e.HasIndex(x => new { x.IDEmpresa, x.SKU });
            e.HasIndex(x => new { x.IDEmpresa, x.ActivoEcommerce });
            e.Property(x => x.DescripcionArticulo).HasMaxLength(100).IsRequired();
            e.Property(x => x.DescripcionArticuloAbreviada).HasMaxLength(50).IsRequired();
            e.Property(x => x.SKU).HasMaxLength(100);
            e.Property(x => x.SlugSeo).HasMaxLength(180);
            e.HasIndex(x => new { x.IDEmpresa, x.SlugSeo });
            e.Property(x => x.EstadoEcommerce).HasMaxLength(30).IsRequired();
            e.Property(x => x.NombreMarca).HasMaxLength(100);
            e.Property(x => x.NombreLinea).HasMaxLength(100);
            e.Property(x => x.NombreTipoArticulo).HasMaxLength(100);
            e.Property(x => x.NombreProveedor).HasMaxLength(100);
            e.Property(x => x.NombrePais).HasMaxLength(100);
            e.Property(x => x.NombreRegimenImpositivo).HasMaxLength(100);
            e.HasMany(x => x.Embalajes).WithOne(x => x.Articulo!).HasForeignKey(x => x.IDArticulo);
            e.HasMany(x => x.Imagenes).WithOne(x => x.Articulo!).HasForeignKey(x => x.IDArticulo);
        });

        modelBuilder.Entity<CatalogoParametro>(e =>
        {
            e.ToTable("catalogoparametro");
            e.HasKey(x => x.ID);
            e.HasIndex(x => new { x.IDEmpresa, x.Tipo, x.Codigo }).IsUnique();
            e.Property(x => x.Tipo).HasMaxLength(20).IsRequired();
            e.Property(x => x.Descripcion).HasMaxLength(100).IsRequired();
        });

        modelBuilder.Entity<ArticuloEmbalaje>(e =>
        {
            e.HasKey(x => x.ID);
            e.HasIndex(x => new { x.IDEmpresa, x.IDArticulo });
            e.Property(x => x.CodigoBarras).HasMaxLength(50);
            e.Property(x => x.DescripcionEmbalaje).HasMaxLength(100);
            e.Property(x => x.CodigoUnidadMedida).HasMaxLength(10).IsRequired();
            e.HasMany(x => x.CodigosBarra)
                .WithOne(x => x.ArticuloEmbalaje!)
                .HasForeignKey(x => x.IDArticuloEmbalaje);
        });

        modelBuilder.Entity<ArticuloEmbalajeCodigoBarra>(e =>
        {
            e.ToTable("articuloembalajecodigobarra");
            e.HasKey(x => x.ID);
            e.HasIndex(x => new { x.IDEmpresa, x.IDArticuloEmbalaje });
            e.HasIndex(x => new { x.IDEmpresa, x.CodigoBarra });
            e.Property(x => x.CodigoBarra).HasMaxLength(400).IsRequired();
        });

        modelBuilder.Entity<ChoferEcommerce>(e =>
        {
            e.ToTable("chofer");
            e.HasKey(x => x.ID);
            e.HasIndex(x => new { x.IDEmpresa, x.NroChofer }).IsUnique();
            e.Property(x => x.Nombre).HasMaxLength(200).IsRequired();
            e.Property(x => x.Documento).HasMaxLength(60);
            e.Property(x => x.Telefono).HasMaxLength(40);
            e.Property(x => x.Observacion).HasMaxLength(500);
        });

        modelBuilder.Entity<UnidadMedidaEcommerce>(e =>
        {
            e.HasKey(x => x.ID);
            e.HasIndex(x => new { x.IDEmpresa, x.Codigo }).IsUnique();
            e.Property(x => x.Codigo).HasMaxLength(10).IsRequired();
            e.Property(x => x.Nombre).HasMaxLength(80).IsRequired();
            e.Property(x => x.Simbolo).HasMaxLength(16);
        });

        modelBuilder.Entity<ArticuloImagen>(e =>
        {
            e.HasKey(x => x.ID);
            e.HasIndex(x => new { x.IDEmpresa, x.IDArticulo });
            e.Property(x => x.Url).HasMaxLength(500).IsRequired();
            e.Property(x => x.Titulo).HasMaxLength(100);
        });

        modelBuilder.Entity<ListaPrecio>(e =>
        {
            e.HasKey(x => x.ID);
            e.HasIndex(x => new { x.IDEmpresa, x.EsListaPrecioEcommerce });
            e.HasIndex(x => new { x.IDEmpresa, x.CanalEcommerce });
            e.Property(x => x.Descripcion).HasMaxLength(255).IsRequired();
            e.Property(x => x.CanalEcommerce).HasMaxLength(10).IsRequired();
            e.HasMany(x => x.DetallesCondicionVenta)
                .WithOne(x => x.ListaPrecio!)
                .HasForeignKey(x => x.IDListaPrecio);
            e.HasMany(x => x.DetallesListaPrecio)
                .WithOne(x => x.ListaPrecio!)
                .HasForeignKey(x => x.IDListaPrecio);
        });

        modelBuilder.Entity<DetalleListaPrecio>(e =>
        {
            e.ToTable("detallelistaprecio");
            e.HasKey(x => x.ID);
            e.HasIndex(x => new { x.IDEmpresa, x.IDListaPrecio, x.IDArticuloEmbalaje }).IsUnique();
            e.HasOne(x => x.ArticuloEmbalaje).WithMany().HasForeignKey(x => x.IDArticuloEmbalaje);
        });

        modelBuilder.Entity<DetalleListaPrecioCondicionVenta>(e =>
        {
            e.HasKey(x => x.ID);
            e.HasIndex(x => new { x.IDEmpresa, x.IDListaPrecio, x.IDArticuloEmbalaje }).IsUnique();
            e.HasOne(x => x.ArticuloEmbalaje).WithMany().HasForeignKey(x => x.IDArticuloEmbalaje);
        });

        modelBuilder.Entity<ListaPrecioCuenta>(e =>
        {
            e.ToTable("listapreciocuenta");
            e.HasKey(x => x.ID);
            e.HasIndex(x => new { x.IDEmpresa, x.IDCuenta, x.Activo });
            e.HasIndex(x => new { x.IDEmpresa, x.IDCuenta, x.IDListaPrecio });
            e.HasOne(x => x.ListaPrecio).WithMany(x => x.ListaPrecioCuentas).HasForeignKey(x => x.IDListaPrecio);
            e.HasOne(x => x.Cuenta).WithMany().HasForeignKey(x => x.IDCuenta);
        });

        modelBuilder.Entity<UsuarioEcommerce>(e =>
        {
            e.HasKey(x => x.ID);
            e.HasIndex(x => new { x.IDEmpresa, x.Usuario }).IsUnique();
            e.Property(x => x.Usuario).HasMaxLength(100).IsRequired();
            e.Property(x => x.Password).HasMaxLength(200).IsRequired();
            e.Property(x => x.CodigoUsuario).HasMaxLength(20).IsRequired();
            e.Property(x => x.Nombre).HasMaxLength(100).IsRequired();
            e.Property(x => x.Apellido).HasMaxLength(100).IsRequired();
            e.Property(x => x.Email).HasMaxLength(120);
            e.Property(x => x.NumeroDocumento).HasMaxLength(60);
            e.Property(x => x.NumeroTelefono).HasMaxLength(30);
            e.Property(x => x.NumeroMovil).HasMaxLength(30);
            e.Property(x => x.CodigoActivacion).HasMaxLength(64);
            e.Property(x => x.ChecksumActivacion).HasMaxLength(64);
            e.Property(x => x.CodigoResetPassword).HasMaxLength(64);
            e.Property(x => x.ChecksumResetPassword).HasMaxLength(64);
            e.Property(x => x.CanalPreferido).HasMaxLength(10).IsRequired();
            e.Property(x => x.EstadoMayorista).HasMaxLength(20).IsRequired();
            e.Property(x => x.MotivoSolicitudMayorista).HasMaxLength(500);
            e.HasOne(x => x.TipoDocumento).WithMany().HasForeignKey(x => x.IDTipoDocumento)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<UsuarioEcommerceCuenta>(e =>
        {
            e.HasKey(x => x.ID);
            e.HasIndex(x => new { x.IDEmpresa, x.IDUsuarioEcommerce, x.IDCuenta }).IsUnique();
            e.HasOne(x => x.UsuarioEcommerce)
                .WithMany(x => x.UsuarioEcommerceCuentas)
                .HasForeignKey(x => x.IDUsuarioEcommerce);
        });

        modelBuilder.Entity<CarritoCompra>(e =>
        {
            e.HasKey(x => x.ID);
            e.HasIndex(x => new { x.IDEmpresa, x.NroPedidoCarritoCompra });
            e.HasIndex(x => new { x.IDEmpresa, x.IDSesion });
            e.Property(x => x.RazonSocial).HasMaxLength(255).IsRequired();
            e.Property(x => x.NumeroDocumento).HasMaxLength(25).IsRequired();
            e.Property(x => x.CodigoCuponAplicado).HasMaxLength(40);
            e.HasMany(x => x.DetalleCarritoCompras)
                .WithOne(x => x.CarritoCompra!)
                .HasForeignKey(x => x.IDCarritoCompra);
            e.HasOne(x => x.PedidoMovil)
                .WithMany(x => x.CarritoCompras)
                .HasForeignKey(x => x.IDPedidoMovil)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<DetalleCarritoCompra>(e =>
        {
            e.HasKey(x => x.ID);
            e.HasOne(x => x.Articulo).WithMany().HasForeignKey(x => x.IDArticulo);
        });

        modelBuilder.Entity<GrupoArticuloEcommerce>(e =>
        {
            e.HasKey(x => x.ID);
            e.Property(x => x.DescripcionGrupoArticulo).HasMaxLength(100).IsRequired();
            e.Property(x => x.Imagen).HasMaxLength(500);
            e.Property(x => x.UrlDestino).HasMaxLength(500);
            e.Property(x => x.Subtitulo).HasMaxLength(200);
            e.HasMany(x => x.DetalleGrupoArticuloEcommerces)
                .WithOne(x => x.GrupoArticuloEcommerce!)
                .HasForeignKey(x => x.IDGrupoArticuloEcommerce);
        });

        modelBuilder.Entity<DetalleGrupoArticuloEcommerce>(e =>
        {
            e.HasKey(x => x.ID);
            e.HasIndex(x => new { x.IDEmpresa, x.IDGrupoArticuloEcommerce, x.IDArticulo }).IsUnique();
            e.HasOne(x => x.Articulo).WithMany().HasForeignKey(x => x.IDArticulo);
        });

        modelBuilder.Entity<TipoDeposito>(e =>
        {
            e.HasKey(x => x.ID);
            e.HasIndex(x => new { x.IDEmpresa, x.CodTipoDeposito }).IsUnique();
            e.Property(x => x.DescripcionTipoDeposito).HasMaxLength(100).IsRequired();
            e.HasMany(x => x.Depositos).WithOne(x => x.TipoDeposito!).HasForeignKey(x => x.IDTipoDeposito);
        });

        modelBuilder.Entity<Deposito>(e =>
        {
            e.HasKey(x => x.ID);
            e.HasIndex(x => new { x.IDEmpresa, x.CodDeposito }).IsUnique();
            e.Property(x => x.DescripcionDeposito).HasMaxLength(256).IsRequired();
            e.HasMany(x => x.Stocks).WithOne(x => x.Deposito!).HasForeignKey(x => x.IDDeposito);
        });

        modelBuilder.Entity<Stock>(e =>
        {
            e.HasKey(x => x.ID);
            // Espejo UQ_Stock_LegacyPK (ERP): empresa+depósito+artículo+contenedor+lote
            e.HasIndex(x => new { x.IDEmpresa, x.IDDeposito, x.IDArticulo, x.IDContenedor, x.IDLoteArticulo })
                .IsUnique();
            e.HasIndex(x => x.TotalExistencia);
            e.HasIndex(x => x.ExistenciaCongelada);
            e.HasOne(x => x.Articulo).WithMany().HasForeignKey(x => x.IDArticulo);
        });

        modelBuilder.Entity<PedidoMovil>(e =>
        {
            e.HasKey(x => x.ID);
            e.HasIndex(x => new { x.IDEmpresa, x.NroPedido, x.OrigenPedido });
            e.HasIndex(x => new { x.IDEmpresa, x.Migrado });
            e.Property(x => x.Observacion).HasMaxLength(500);
            e.Property(x => x.Host).HasMaxLength(500);
            e.Property(x => x.NroOrdenCompra).HasMaxLength(50);
            e.Property(x => x.MotivoPendiente).HasMaxLength(512);
            e.Property(x => x.DireccionEntrega).HasMaxLength(500);
            e.Property(x => x.TokenTrackingPublico).HasMaxLength(64);
            e.HasIndex(x => x.TokenTrackingPublico).IsUnique()
                .HasFilter("\"TokenTrackingPublico\" IS NOT NULL");
            e.HasOne(x => x.Chofer).WithMany().HasForeignKey(x => x.IDChofer).OnDelete(DeleteBehavior.SetNull);
            e.HasMany(x => x.DetallePedidoMoviles)
                .WithOne(x => x.PedidoMovil!)
                .HasForeignKey(x => x.IDPedidoMovil);
        });

        modelBuilder.Entity<DetallePedidoMovil>(e =>
        {
            e.HasKey(x => x.ID);
            e.HasIndex(x => x.IDPedidoMovil);
            e.Property(x => x.CodigoBarra).HasMaxLength(20);
            e.Property(x => x.Observacion).HasMaxLength(500);
            e.Property(x => x.Host).HasMaxLength(500);
            e.HasOne(x => x.Articulo).WithMany().HasForeignKey(x => x.IDArticulo);
        });

        modelBuilder.Entity<TarifaDelivery>(e =>
        {
            e.HasKey(x => x.ID);
            e.HasIndex(x => new { x.IDEmpresa, x.IDBarrio, x.FechaInicioVigencia });
            e.HasIndex(x => new { x.IDEmpresa, x.Activo, x.Estado });
            e.Property(x => x.NombreCiudad).HasMaxLength(100).IsRequired();
            e.Property(x => x.NombreBarrio).HasMaxLength(100).IsRequired();
            e.HasOne(x => x.Ciudad).WithMany().HasForeignKey(x => x.IDCiudad).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.Barrio).WithMany().HasForeignKey(x => x.IDBarrio).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.DivisionPolitica).WithMany().HasForeignKey(x => x.IDDivisionPolitica)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<DivisionPolitica>(e =>
        {
            e.HasKey(x => x.ID);
            e.HasIndex(x => x.CodDivisionPolitica);
            e.Property(x => x.DescripcionDivisionPolitica).HasMaxLength(100).IsRequired();
            e.Property(x => x.NombreAbreviado).HasMaxLength(5).IsRequired();
            e.HasMany(x => x.Ciudades).WithOne(x => x.DivisionPolitica!).HasForeignKey(x => x.IDDivisionPolitica);
        });

        modelBuilder.Entity<Ciudad>(e =>
        {
            e.HasKey(x => x.ID);
            e.HasIndex(x => x.CodCiudad);
            e.Property(x => x.DescripcionCiudad).HasMaxLength(100).IsRequired();
            e.Property(x => x.CodigoSipap).HasMaxLength(20);
            e.HasMany(x => x.Localidades).WithOne(x => x.Ciudad!).HasForeignKey(x => x.IDCiudad);
        });

        modelBuilder.Entity<Localidad>(e =>
        {
            e.HasKey(x => x.ID);
            e.Property(x => x.DescripcionLocalidad).HasMaxLength(100).IsRequired();
            e.HasMany(x => x.Distritos).WithOne(x => x.Localidad!).HasForeignKey(x => x.IDLocalidad);
        });

        modelBuilder.Entity<Distrito>(e =>
        {
            e.HasKey(x => x.ID);
            e.Property(x => x.DescripcionDistrito).HasMaxLength(100).IsRequired();
            e.HasMany(x => x.Barrios).WithOne(x => x.Distrito!).HasForeignKey(x => x.IDDistrito);
        });

        modelBuilder.Entity<Barrio>(e =>
        {
            e.HasKey(x => x.ID);
            e.HasIndex(x => x.CodBarrio);
            e.Property(x => x.DescripcionBarrio).HasMaxLength(100).IsRequired();
        });

        modelBuilder.Entity<TipoDocumento>(e =>
        {
            e.HasKey(x => x.ID);
            e.HasIndex(x => new { x.IDEmpresa, x.CodTipoDocumento }).IsUnique();
            e.Property(x => x.DescripcionTipoDocumento).HasMaxLength(100).IsRequired();
        });

        modelBuilder.Entity<Cuenta>(e =>
        {
            e.HasKey(x => x.ID);
            e.HasIndex(x => new { x.IDEmpresa, x.NroCuenta }).IsUnique();
            e.Property(x => x.RazonSocial).HasMaxLength(255).IsRequired();
            e.Property(x => x.Direccion).HasMaxLength(255);
            e.Property(x => x.Email).HasMaxLength(60);
        });

        modelBuilder.Entity<Agencia>(e =>
        {
            e.HasKey(x => x.ID);
            e.HasIndex(x => new { x.IDEmpresa, x.CodAgencia }).IsUnique();
            e.Property(x => x.DescripcionAgencia).HasMaxLength(100).IsRequired();
            e.Property(x => x.Direccion).HasMaxLength(255);
            e.HasMany(x => x.Horarios).WithOne(x => x.Agencia!).HasForeignKey(x => x.IDAgencia);
        });

        modelBuilder.Entity<HorarioAtencion>(e =>
        {
            e.HasKey(x => x.ID);
            e.HasIndex(x => new { x.IDEmpresa, x.IDAgencia, x.DiaSemana });
        });

        modelBuilder.Entity<LogPagoTarjeta>(e =>
        {
            e.HasKey(x => x.ID);
            e.HasIndex(x => new { x.IDEmpresa, x.ShopProcessId });
            e.HasIndex(x => x.IDCarritoCompra);
            e.Property(x => x.Status).HasMaxLength(30).IsRequired();
            e.Property(x => x.ProcessId).HasMaxLength(100);
            e.Property(x => x.AuthorizationCode).HasMaxLength(100);
        });

        modelBuilder.Entity<ResenaArticuloEcommerce>(e =>
        {
            e.HasKey(x => x.ID);
            e.HasIndex(x => new { x.IDEmpresa, x.IDArticulo, x.Aprobado });
            e.HasIndex(x => new { x.IDEmpresa, x.IDUsuarioEcommerce, x.IDArticulo }).IsUnique();
            e.Property(x => x.Email).HasMaxLength(120).IsRequired();
            e.Property(x => x.CanalEcommerce).HasMaxLength(10).IsRequired();
            e.Property(x => x.Titulo).HasMaxLength(120);
            e.Property(x => x.Comentario).HasMaxLength(2000);
            e.HasOne(x => x.Articulo).WithMany().HasForeignKey(x => x.IDArticulo);
            e.HasOne(x => x.UsuarioEcommerce).WithMany().HasForeignKey(x => x.IDUsuarioEcommerce);
        });

        modelBuilder.Entity<ListaFavoritoEcommerce>(e =>
        {
            e.HasKey(x => x.ID);
            e.HasIndex(x => new { x.IDEmpresa, x.IDUsuarioEcommerce, x.CanalEcommerce });
            e.Property(x => x.NombreLista).HasMaxLength(80).IsRequired();
            e.Property(x => x.Email).HasMaxLength(120).IsRequired();
            e.Property(x => x.CanalEcommerce).HasMaxLength(10).IsRequired();
            e.Property(x => x.Color).HasMaxLength(20);
            e.HasOne(x => x.UsuarioEcommerce).WithMany().HasForeignKey(x => x.IDUsuarioEcommerce);
            e.HasMany(x => x.Detalles).WithOne(x => x.ListaFavoritoEcommerce!).HasForeignKey(x => x.IDListaFavoritoEcommerce);
        });

        modelBuilder.Entity<DetalleListaFavoritoEcommerce>(e =>
        {
            e.HasKey(x => x.ID);
            e.HasIndex(x => new { x.IDEmpresa, x.IDListaFavoritoEcommerce, x.IDArticulo }).IsUnique();
            e.Property(x => x.Nota).HasMaxLength(200);
            e.HasOne(x => x.Articulo).WithMany().HasForeignKey(x => x.IDArticulo);
        });

        modelBuilder.Entity<ContactoMensaje>(e =>
        {
            e.HasKey(x => x.ID);
            e.HasIndex(x => new { x.IDEmpresa, x.FechaUtc });
            e.Property(x => x.Nombre).HasMaxLength(120).IsRequired();
            e.Property(x => x.Email).HasMaxLength(256).IsRequired();
            e.Property(x => x.Telefono).HasMaxLength(40);
            e.Property(x => x.Mensaje).HasMaxLength(4000).IsRequired();
        });

        modelBuilder.Entity<InteraccionArticuloEcommerce>(e =>
        {
            e.HasKey(x => x.ID);
            e.HasIndex(x => new { x.IDEmpresa, x.IDArticulo, x.Momento });
            e.Property(x => x.TipoInteraccion).HasMaxLength(30).IsRequired();
            e.Property(x => x.CanalEcommerce).HasMaxLength(10).IsRequired();
            e.Property(x => x.Email).HasMaxLength(120);
            e.Property(x => x.IDSesion).HasMaxLength(100);
        });

        modelBuilder.Entity<CuponEcommerce>(e =>
        {
            e.HasKey(x => x.ID);
            e.HasIndex(x => new { x.IDEmpresa, x.Codigo }).IsUnique();
            e.Property(x => x.Codigo).HasMaxLength(40).IsRequired();
            e.Property(x => x.Descripcion).HasMaxLength(200).IsRequired();
            e.Property(x => x.TipoDescuento).HasMaxLength(20).IsRequired();
            e.Property(x => x.CanalEcommerce).HasMaxLength(10).IsRequired();
        });

        modelBuilder.Entity<UsoCuponEcommerce>(e =>
        {
            e.HasKey(x => x.ID);
            e.HasIndex(x => new { x.IDEmpresa, x.IDCuponEcommerce, x.IDUsuarioEcommerce });
            e.Property(x => x.Email).HasMaxLength(120);
            e.HasOne(x => x.CuponEcommerce).WithMany().HasForeignKey(x => x.IDCuponEcommerce);
        });

        modelBuilder.Entity<ProductoSeguidoEcommerce>(e =>
        {
            e.HasKey(x => x.ID);
            e.HasIndex(x => new { x.IDEmpresa, x.IDUsuarioEcommerce, x.IDArticulo }).IsUnique();
            e.Property(x => x.Email).HasMaxLength(120).IsRequired();
            e.Property(x => x.CanalEcommerce).HasMaxLength(10).IsRequired();
        });

        modelBuilder.Entity<SeguimientoPedidoEcommerce>(e =>
        {
            e.HasKey(x => x.ID);
            e.HasIndex(x => new { x.IDEmpresa, x.IDPedidoMovil, x.FechaEstado });
            e.Property(x => x.Estado).HasMaxLength(40).IsRequired();
            e.Property(x => x.Mensaje).HasMaxLength(500);
            e.Property(x => x.CodigoTracking).HasMaxLength(100);
        });

        modelBuilder.Entity<ConfiguracionPersonalizacion>(e =>
        {
            e.ToTable("configuracionpersonalizacion");
            e.HasKey(x => x.ID);
            e.HasIndex(x => new { x.IDEmpresa, x.Clave, x.Version }).IsUnique();
            e.HasIndex(x => new { x.IDEmpresa, x.Clave, x.Activo });
            e.Property(x => x.Clave).HasMaxLength(80).IsRequired();
            e.Property(x => x.ValorJson).IsRequired();
            e.Property(x => x.CreadoPor).HasMaxLength(80);
            e.Property(x => x.Nota).HasMaxLength(300);
        });

        modelBuilder.Entity<ContenidoBloque>(e =>
        {
            e.ToTable("contenidobloque");
            e.HasKey(x => x.ID);
            e.HasIndex(x => new { x.IDEmpresa, x.PageCode, x.DisplayOrder });
            e.HasIndex(x => new { x.IDEmpresa, x.BlockType });
            e.Property(x => x.PageCode).HasMaxLength(80).IsRequired();
            e.Property(x => x.PageZone).HasMaxLength(50);
            e.Property(x => x.BlockType).HasMaxLength(60).IsRequired();
            e.Property(x => x.InternalName).HasMaxLength(150).IsRequired();
            e.Property(x => x.Title).HasMaxLength(250);
            e.Property(x => x.Subtitle).HasMaxLength(250);
            e.Property(x => x.LayoutVariant).HasMaxLength(50);
            e.Property(x => x.ContentStatus).HasMaxLength(20).IsRequired();
            e.HasMany(x => x.Items).WithOne(x => x.Bloque!).HasForeignKey(x => x.IDContenidoBloque);
        });

        modelBuilder.Entity<ContenidoBloqueItem>(e =>
        {
            e.ToTable("contenidobloqueitem");
            e.HasKey(x => x.ID);
            e.HasIndex(x => new { x.IDContenidoBloque, x.DisplayOrder });
            e.Property(x => x.ItemType).HasMaxLength(50).IsRequired();
            e.Property(x => x.Title).HasMaxLength(250);
            e.Property(x => x.Subtitle).HasMaxLength(250);
            e.Property(x => x.ImageUrl).HasMaxLength(800);
            e.Property(x => x.MobileImageUrl).HasMaxLength(800);
            e.Property(x => x.LinkUrl).HasMaxLength(800);
            e.Property(x => x.LinkTarget).HasMaxLength(20);
            e.Property(x => x.BadgeText).HasMaxLength(50);
            e.Property(x => x.PriceText).HasMaxLength(100);
            e.Property(x => x.ContentStatus).HasMaxLength(20).IsRequired();
        });

        modelBuilder.Entity<NavegacionMenu>(e =>
        {
            e.ToTable("navegacionmenu");
            e.HasKey(x => x.ID);
            e.HasIndex(x => new { x.IDEmpresa, x.Code }).IsUnique();
            e.Property(x => x.Code).HasMaxLength(100).IsRequired();
            e.Property(x => x.Name).HasMaxLength(150).IsRequired();
            e.Property(x => x.MenuType).HasMaxLength(50).IsRequired();
            e.Property(x => x.PageCode).HasMaxLength(80);
            e.Property(x => x.PageZone).HasMaxLength(50);
            e.Property(x => x.ContentStatus).HasMaxLength(20).IsRequired();
            e.HasMany(x => x.Items).WithOne(x => x.Menu!).HasForeignKey(x => x.IDNavegacionMenu);
        });

        modelBuilder.Entity<NavegacionMenuItem>(e =>
        {
            e.ToTable("navegacionmenuitem");
            e.HasKey(x => x.ID);
            e.HasIndex(x => new { x.IDNavegacionMenu, x.DisplayOrder });
            e.Property(x => x.ItemType).HasMaxLength(50).IsRequired();
            e.Property(x => x.Label).HasMaxLength(150).IsRequired();
            e.Property(x => x.Url).HasMaxLength(800);
            e.Property(x => x.Target).HasMaxLength(20);
            e.Property(x => x.BadgeText).HasMaxLength(50);
            e.Property(x => x.ContentStatus).HasMaxLength(20).IsRequired();
        });

        modelBuilder.Entity<AdminAuditoriaLog>(e =>
        {
            e.ToTable("adminauditorialog");
            e.HasKey(x => x.ID);
            e.HasIndex(x => new { x.IDEmpresa, x.Fecha });
            e.Property(x => x.Usuario).HasMaxLength(120).IsRequired();
            e.Property(x => x.Accion).HasMaxLength(80).IsRequired();
            e.Property(x => x.Entidad).HasMaxLength(80).IsRequired();
            e.Property(x => x.EntidadId).HasMaxLength(80);
            e.Property(x => x.Detalle).HasMaxLength(1000);
            e.Property(x => x.Ip).HasMaxLength(64);
        });

        modelBuilder.Entity<InventarioMovimiento>(e =>
        {
            e.ToTable("inventariomovimiento");
            e.HasKey(x => x.ID);
            e.HasIndex(x => new { x.IDEmpresa, x.IDArticulo, x.Fecha });
            e.Property(x => x.TipoMovimiento).HasMaxLength(40).IsRequired();
            e.Property(x => x.Referencia).HasMaxLength(200);
            e.Property(x => x.Cantidad).HasPrecision(18, 4);
            e.Property(x => x.ExistenciaAntes).HasPrecision(18, 4);
            e.Property(x => x.ExistenciaDespues).HasPrecision(18, 4);
        });

        modelBuilder.Entity<DevolucionEcommerce>(e =>
        {
            e.HasKey(x => x.ID);
            e.HasIndex(x => new { x.IDEmpresa, x.IDUsuarioEcommerce });
            e.Property(x => x.Email).HasMaxLength(120).IsRequired();
            e.Property(x => x.Motivo).HasMaxLength(200).IsRequired();
            e.Property(x => x.Detalle).HasMaxLength(1000);
            e.Property(x => x.Estado).HasMaxLength(30).IsRequired();
            e.Property(x => x.UrlAdjunto).HasMaxLength(800);
            e.Property(x => x.MimeType).HasMaxLength(100);
            e.Property(x => x.NombreArchivo).HasMaxLength(260);
            e.Property(x => x.ErpMensaje).HasMaxLength(500);
        });

        modelBuilder.Entity<ChatConversacionEcommerce>(e =>
        {
            e.HasKey(x => x.ID);
            e.HasIndex(x => new { x.IDEmpresa, x.IDUsuarioEcommerce, x.Estado });
            e.HasIndex(x => new { x.IDEmpresa, x.FechaUltimoMensaje });
            e.Property(x => x.Email).HasMaxLength(120).IsRequired();
            e.Property(x => x.NombreCliente).HasMaxLength(200);
            e.Property(x => x.Asunto).HasMaxLength(40).IsRequired();
            e.Property(x => x.Estado).HasMaxLength(30).IsRequired();
            e.Property(x => x.NombreAdminAsignado).HasMaxLength(200);
            e.Property(x => x.PreviewUltimoMensaje).HasMaxLength(240);
            e.HasOne(x => x.UsuarioEcommerce)
                .WithMany()
                .HasForeignKey(x => x.IDUsuarioEcommerce);
            e.HasMany(x => x.Mensajes)
                .WithOne(x => x.Conversacion!)
                .HasForeignKey(x => x.IDConversacion);
        });

        modelBuilder.Entity<ChatSoporteMensajeEcommerce>(e =>
        {
            e.HasKey(x => x.ID);
            e.HasIndex(x => new { x.IDEmpresa, x.IDConversacion, x.FechaMensaje });
            e.HasIndex(x => new { x.IDEmpresa, x.IDUsuarioEcommerce, x.FechaMensaje });
            e.Property(x => x.Email).HasMaxLength(120).IsRequired();
            e.Property(x => x.Origen).HasMaxLength(20).IsRequired();
            e.Property(x => x.TipoMensaje).HasMaxLength(20).IsRequired();
            e.Property(x => x.Mensaje).HasMaxLength(2000).IsRequired();
            e.Property(x => x.UrlAdjunto).HasMaxLength(800);
            e.Property(x => x.MimeType).HasMaxLength(100);
            e.Property(x => x.NombreArchivo).HasMaxLength(260);
        });

        modelBuilder.Entity<SyncCursor>(e =>
        {
            e.HasKey(x => x.ID);
            e.HasIndex(x => new { x.IDEmpresa, x.EntityKey }).IsUnique();
            e.Property(x => x.EntityKey).HasMaxLength(40).IsRequired();
            e.Property(x => x.LastStatus).HasMaxLength(40);
            e.Property(x => x.LastError).HasMaxLength(1000);
        });

        modelBuilder.Entity<MigracionPedidoLog>(e =>
        {
            e.HasKey(x => x.ID);
            e.HasIndex(x => new { x.IDEmpresa, x.IDPedidoMovil, x.FechaIntento });
            e.HasIndex(x => new { x.IDEmpresa, x.Estado });
            e.Property(x => x.Estado).HasMaxLength(20).IsRequired();
            e.Property(x => x.MensajeError).HasMaxLength(500);
            e.Property(x => x.DetalleTecnico).HasMaxLength(2000);
            e.Property(x => x.UserNameOperador).HasMaxLength(100);
        });

        modelBuilder.Entity<AdminUsuario>(e =>
        {
            e.HasKey(x => x.ID);
            e.HasIndex(x => new { x.IDEmpresa, x.Usuario }).IsUnique();
            e.Property(x => x.Usuario).HasMaxLength(100).IsRequired();
            e.Property(x => x.PasswordHash).HasMaxLength(400).IsRequired();
            e.Property(x => x.Nombre).HasMaxLength(200);
            e.Property(x => x.Email).HasMaxLength(200);
        });

        modelBuilder.Entity<AdminRol>(e =>
        {
            e.HasKey(x => x.ID);
            e.HasIndex(x => new { x.IDEmpresa, x.CodigoRol }).IsUnique();
            e.Property(x => x.CodigoRol).HasMaxLength(50).IsRequired();
            e.Property(x => x.NombreRol).HasMaxLength(200).IsRequired();
        });

        modelBuilder.Entity<AdminFuncionalidad>(e =>
        {
            e.HasKey(x => x.ID);
            e.HasIndex(x => new { x.IDEmpresa, x.CodigoPermission }).IsUnique();
            e.HasIndex(x => new { x.IDEmpresa, x.IDFuncionalidadPadre });
            e.Property(x => x.CodigoPermission).HasMaxLength(200).IsRequired();
            e.Property(x => x.Descripcion).HasMaxLength(400);
        });

        modelBuilder.Entity<AdminUsuarioRol>(e =>
        {
            e.HasKey(x => x.ID);
            e.HasIndex(x => new { x.IDEmpresa, x.IDUsuario, x.IDRol }).IsUnique();
        });

        modelBuilder.Entity<AdminRolFuncionalidad>(e =>
        {
            e.HasKey(x => x.ID);
            e.HasIndex(x => new { x.IDEmpresa, x.IDRol, x.IDFuncionalidad }).IsUnique();
        });

        modelBuilder.Entity<AdminUsuarioFuncionalidad>(e =>
        {
            e.ToTable("adminusuariofuncionalidad");
            e.HasKey(x => x.ID);
            e.HasIndex(x => new { x.IDEmpresa, x.IDUsuario, x.IDFuncionalidad }).IsUnique();
        });
    }
}
