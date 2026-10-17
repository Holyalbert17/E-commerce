using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Datamex.ECommerce.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialPostgres : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "dbo");

            migrationBuilder.CreateTable(
                name: "adminauditorialog",
                schema: "dbo",
                columns: table => new
                {
                    ID = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    IDAdminUsuario = table.Column<long>(type: "bigint", nullable: true),
                    Usuario = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    Accion = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    Entidad = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    EntidadId = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true),
                    Detalle = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    Fecha = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Ip = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    Activo = table.Column<bool>(type: "boolean", nullable: false),
                    UltModificacion = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UserName = table.Column<string>(type: "text", nullable: true),
                    IDEmpresa = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_adminauditorialog", x => x.ID);
                });

            migrationBuilder.CreateTable(
                name: "adminfuncionalidad",
                schema: "dbo",
                columns: table => new
                {
                    ID = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CodigoPermission = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Descripcion = table.Column<string>(type: "character varying(400)", maxLength: 400, nullable: false),
                    Activo = table.Column<bool>(type: "boolean", nullable: false),
                    UltModificacion = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UserName = table.Column<string>(type: "text", nullable: true),
                    IDEmpresa = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_adminfuncionalidad", x => x.ID);
                });

            migrationBuilder.CreateTable(
                name: "adminrol",
                schema: "dbo",
                columns: table => new
                {
                    ID = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CodigoRol = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    NombreRol = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Activo = table.Column<bool>(type: "boolean", nullable: false),
                    UltModificacion = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UserName = table.Column<string>(type: "text", nullable: true),
                    IDEmpresa = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_adminrol", x => x.ID);
                });

            migrationBuilder.CreateTable(
                name: "adminrolfuncionalidad",
                schema: "dbo",
                columns: table => new
                {
                    ID = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    IDRol = table.Column<long>(type: "bigint", nullable: false),
                    IDFuncionalidad = table.Column<long>(type: "bigint", nullable: false),
                    Activo = table.Column<bool>(type: "boolean", nullable: false),
                    UltModificacion = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UserName = table.Column<string>(type: "text", nullable: true),
                    IDEmpresa = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_adminrolfuncionalidad", x => x.ID);
                });

            migrationBuilder.CreateTable(
                name: "adminusuario",
                schema: "dbo",
                columns: table => new
                {
                    ID = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Usuario = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    PasswordHash = table.Column<string>(type: "character varying(400)", maxLength: 400, nullable: false),
                    Nombre = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    Email = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    Activo = table.Column<bool>(type: "boolean", nullable: false),
                    UltModificacion = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UserName = table.Column<string>(type: "text", nullable: true),
                    IDEmpresa = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_adminusuario", x => x.ID);
                });

            migrationBuilder.CreateTable(
                name: "adminusuariorol",
                schema: "dbo",
                columns: table => new
                {
                    ID = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    IDUsuario = table.Column<long>(type: "bigint", nullable: false),
                    IDRol = table.Column<long>(type: "bigint", nullable: false),
                    Activo = table.Column<bool>(type: "boolean", nullable: false),
                    UltModificacion = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UserName = table.Column<string>(type: "text", nullable: true),
                    IDEmpresa = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_adminusuariorol", x => x.ID);
                });

            migrationBuilder.CreateTable(
                name: "agencia",
                schema: "dbo",
                columns: table => new
                {
                    ID = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CodAgencia = table.Column<int>(type: "integer", nullable: false),
                    DescripcionAgencia = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Direccion = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    IDCiudad = table.Column<long>(type: "bigint", nullable: true),
                    IDDeposito = table.Column<long>(type: "bigint", nullable: true),
                    Latitud = table.Column<decimal>(type: "numeric", nullable: true),
                    Longitud = table.Column<decimal>(type: "numeric", nullable: true),
                    EsSucursalEcommerce = table.Column<bool>(type: "boolean", nullable: false),
                    Activo = table.Column<bool>(type: "boolean", nullable: false),
                    UltModificacion = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UserName = table.Column<string>(type: "text", nullable: true),
                    IDEmpresa = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_agencia", x => x.ID);
                });

            migrationBuilder.CreateTable(
                name: "articulo",
                schema: "dbo",
                columns: table => new
                {
                    ID = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    NroArticulo = table.Column<int>(type: "integer", nullable: false),
                    SKU = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    DescripcionArticulo = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    DescripcionArticuloAbreviada = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    DescripcionArticuloExtensa = table.Column<string>(type: "text", nullable: false),
                    CodReferenciaProveedor = table.Column<string>(type: "text", nullable: true),
                    IDMarca = table.Column<long>(type: "bigint", nullable: false),
                    NombreMarca = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    IDLinea = table.Column<long>(type: "bigint", nullable: true),
                    NombreLinea = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    IDTipoArticulo = table.Column<long>(type: "bigint", nullable: true),
                    NombreTipoArticulo = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    IDProveedor = table.Column<long>(type: "bigint", nullable: true),
                    NombreProveedor = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    IDPais = table.Column<long>(type: "bigint", nullable: true),
                    NombrePais = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    IDRegimenImpositivo = table.Column<long>(type: "bigint", nullable: true),
                    NombreRegimenImpositivo = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    ActivoEcommerce = table.Column<bool>(type: "boolean", nullable: false),
                    SlugSeo = table.Column<string>(type: "character varying(180)", maxLength: 180, nullable: true),
                    EstadoEcommerce = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    FichaTecnicaUrl = table.Column<string>(type: "text", nullable: true),
                    VideoUrl = table.Column<string>(type: "text", nullable: true),
                    Activo = table.Column<bool>(type: "boolean", nullable: false),
                    UltModificacion = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UserName = table.Column<string>(type: "text", nullable: true),
                    IDEmpresa = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_articulo", x => x.ID);
                });

            migrationBuilder.CreateTable(
                name: "catalogoparametro",
                schema: "dbo",
                columns: table => new
                {
                    ID = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Tipo = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Codigo = table.Column<int>(type: "integer", nullable: false),
                    Descripcion = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Activo = table.Column<bool>(type: "boolean", nullable: false),
                    UltModificacion = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UserName = table.Column<string>(type: "text", nullable: true),
                    IDEmpresa = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_catalogoparametro", x => x.ID);
                });

            migrationBuilder.CreateTable(
                name: "chofer",
                schema: "dbo",
                columns: table => new
                {
                    ID = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    NroChofer = table.Column<int>(type: "integer", nullable: false),
                    Nombre = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Documento = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: true),
                    Telefono = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true),
                    Observacion = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Activo = table.Column<bool>(type: "boolean", nullable: false),
                    UltModificacion = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UserName = table.Column<string>(type: "text", nullable: true),
                    IDEmpresa = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_chofer", x => x.ID);
                });

            migrationBuilder.CreateTable(
                name: "configuracionpersonalizacion",
                schema: "dbo",
                columns: table => new
                {
                    ID = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Clave = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    ValorJson = table.Column<string>(type: "text", nullable: false),
                    Version = table.Column<int>(type: "integer", nullable: false),
                    FechaInicioVigencia = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    FechaFinVigencia = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreadoPor = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true),
                    FechaCreacion = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Nota = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    Activo = table.Column<bool>(type: "boolean", nullable: false),
                    UltModificacion = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UserName = table.Column<string>(type: "text", nullable: true),
                    IDEmpresa = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_configuracionpersonalizacion", x => x.ID);
                });

            migrationBuilder.CreateTable(
                name: "contenidobloque",
                schema: "dbo",
                columns: table => new
                {
                    ID = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    PageCode = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    PageZone = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    BlockType = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false),
                    InternalName = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    Title = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: true),
                    Subtitle = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: true),
                    Description = table.Column<string>(type: "text", nullable: true),
                    LayoutVariant = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    DisplayOrder = table.Column<int>(type: "integer", nullable: false),
                    ContentStatus = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    IsReadOnlyWhenIntegrated = table.Column<bool>(type: "boolean", nullable: false),
                    StartDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    EndDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    SettingsJson = table.Column<string>(type: "text", nullable: true),
                    PublishedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    UpdatedByAdminUserId = table.Column<long>(type: "bigint", nullable: true),
                    FechaCreacion = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Activo = table.Column<bool>(type: "boolean", nullable: false),
                    UltModificacion = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UserName = table.Column<string>(type: "text", nullable: true),
                    IDEmpresa = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_contenidobloque", x => x.ID);
                });

            migrationBuilder.CreateTable(
                name: "cuenta",
                schema: "dbo",
                columns: table => new
                {
                    ID = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    NroCuenta = table.Column<long>(type: "bigint", nullable: false),
                    RazonSocial = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    NombreCuenta = table.Column<string>(type: "text", nullable: true),
                    ApellidoCuenta = table.Column<string>(type: "text", nullable: true),
                    Direccion = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    Email = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: true),
                    IDCiudad = table.Column<long>(type: "bigint", nullable: true),
                    IDBarrio = table.Column<long>(type: "bigint", nullable: true),
                    IDDivisionPolitica = table.Column<long>(type: "bigint", nullable: true),
                    IDListaPrecio = table.Column<long>(type: "bigint", nullable: true),
                    IDAgencia = table.Column<long>(type: "bigint", nullable: true),
                    Latitud = table.Column<decimal>(type: "numeric", nullable: true),
                    Longitud = table.Column<decimal>(type: "numeric", nullable: true),
                    Activo = table.Column<bool>(type: "boolean", nullable: false),
                    UltModificacion = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UserName = table.Column<string>(type: "text", nullable: true),
                    IDEmpresa = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_cuenta", x => x.ID);
                });

            migrationBuilder.CreateTable(
                name: "cuponecommerce",
                schema: "dbo",
                columns: table => new
                {
                    ID = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Codigo = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    Descripcion = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    TipoDescuento = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    ValorDescuento = table.Column<decimal>(type: "numeric", nullable: false),
                    MontoMinimo = table.Column<decimal>(type: "numeric", nullable: false),
                    MontoMaximoDescuento = table.Column<decimal>(type: "numeric", nullable: true),
                    CanalEcommerce = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    FechaInicio = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    FechaFin = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UsosMaximos = table.Column<int>(type: "integer", nullable: true),
                    UsosActuales = table.Column<int>(type: "integer", nullable: false),
                    UsosPorUsuario = table.Column<int>(type: "integer", nullable: false),
                    Activo = table.Column<bool>(type: "boolean", nullable: false),
                    UltModificacion = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UserName = table.Column<string>(type: "text", nullable: true),
                    IDEmpresa = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_cuponecommerce", x => x.ID);
                });

            migrationBuilder.CreateTable(
                name: "devolucionecommerce",
                schema: "dbo",
                columns: table => new
                {
                    ID = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    IDUsuarioEcommerce = table.Column<long>(type: "bigint", nullable: false),
                    IDCuenta = table.Column<long>(type: "bigint", nullable: true),
                    Email = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    IDPedidoMovil = table.Column<long>(type: "bigint", nullable: false),
                    IDArticulo = table.Column<long>(type: "bigint", nullable: true),
                    Motivo = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Detalle = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    Estado = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    FechaSolicitud = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Activo = table.Column<bool>(type: "boolean", nullable: false),
                    UltModificacion = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UserName = table.Column<string>(type: "text", nullable: true),
                    IDEmpresa = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_devolucionecommerce", x => x.ID);
                });

            migrationBuilder.CreateTable(
                name: "divisionpolitica",
                schema: "dbo",
                columns: table => new
                {
                    ID = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CodDivisionPolitica = table.Column<int>(type: "integer", nullable: false),
                    DescripcionDivisionPolitica = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    NombreAbreviado = table.Column<string>(type: "character varying(5)", maxLength: 5, nullable: false),
                    IDPais = table.Column<long>(type: "bigint", nullable: true),
                    Activo = table.Column<bool>(type: "boolean", nullable: false),
                    UltModificacion = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UserName = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_divisionpolitica", x => x.ID);
                });

            migrationBuilder.CreateTable(
                name: "empresa",
                schema: "dbo",
                columns: table => new
                {
                    ID = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    RazonSocial = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    NombreFantasia = table.Column<string>(type: "text", nullable: true),
                    NumeroDocumento = table.Column<string>(type: "text", nullable: true),
                    Activo = table.Column<bool>(type: "boolean", nullable: false),
                    UltModificacion = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UserName = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_empresa", x => x.ID);
                });

            migrationBuilder.CreateTable(
                name: "grupoarticuloecommerce",
                schema: "dbo",
                columns: table => new
                {
                    ID = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CodGrupoArticulo = table.Column<int>(type: "integer", nullable: false),
                    DescripcionGrupoArticulo = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    FechaInicioVigencia = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    FechaFinVigencia = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    TipoGrupo = table.Column<int>(type: "integer", nullable: false),
                    Imagen = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Estado = table.Column<int>(type: "integer", nullable: false),
                    Orden = table.Column<int>(type: "integer", nullable: false),
                    UrlDestino = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Subtitulo = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    Activo = table.Column<bool>(type: "boolean", nullable: false),
                    UltModificacion = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UserName = table.Column<string>(type: "text", nullable: true),
                    IDEmpresa = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_grupoarticuloecommerce", x => x.ID);
                });

            migrationBuilder.CreateTable(
                name: "interaccionarticuloecommerce",
                schema: "dbo",
                columns: table => new
                {
                    ID = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    IDArticulo = table.Column<long>(type: "bigint", nullable: false),
                    IDUsuarioEcommerce = table.Column<long>(type: "bigint", nullable: true),
                    Email = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    IDSesion = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    TipoInteraccion = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    CanalEcommerce = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    Momento = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Activo = table.Column<bool>(type: "boolean", nullable: false),
                    UltModificacion = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UserName = table.Column<string>(type: "text", nullable: true),
                    IDEmpresa = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_interaccionarticuloecommerce", x => x.ID);
                });

            migrationBuilder.CreateTable(
                name: "inventariomovimiento",
                schema: "dbo",
                columns: table => new
                {
                    ID = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    IDArticulo = table.Column<long>(type: "bigint", nullable: false),
                    IDEmbalaje = table.Column<long>(type: "bigint", nullable: true),
                    IDDeposito = table.Column<long>(type: "bigint", nullable: true),
                    IDPedidoMovil = table.Column<long>(type: "bigint", nullable: true),
                    TipoMovimiento = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    Cantidad = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    ExistenciaAntes = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    ExistenciaDespues = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    Referencia = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    Fecha = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Activo = table.Column<bool>(type: "boolean", nullable: false),
                    UltModificacion = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UserName = table.Column<string>(type: "text", nullable: true),
                    IDEmpresa = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_inventariomovimiento", x => x.ID);
                });

            migrationBuilder.CreateTable(
                name: "listaprecio",
                schema: "dbo",
                columns: table => new
                {
                    ID = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    NroListaPrecio = table.Column<int>(type: "integer", nullable: false),
                    Descripcion = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    EsListaPrecioEcommerce = table.Column<bool>(type: "boolean", nullable: false),
                    PrecioConImpuestos = table.Column<bool>(type: "boolean", nullable: false),
                    FechaRegistro = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    FechaInicioVigencia = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    FechaFinVigencia = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CanalEcommerce = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    Estado = table.Column<int>(type: "integer", nullable: false),
                    CantidadMinimaMayorista = table.Column<decimal>(type: "numeric", nullable: false),
                    PorDescuentoMinorista = table.Column<decimal>(type: "numeric", nullable: false),
                    PorDescuentoMayorista = table.Column<decimal>(type: "numeric", nullable: false),
                    Activo = table.Column<bool>(type: "boolean", nullable: false),
                    UltModificacion = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UserName = table.Column<string>(type: "text", nullable: true),
                    IDEmpresa = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_listaprecio", x => x.ID);
                });

            migrationBuilder.CreateTable(
                name: "logpagotarjeta",
                schema: "dbo",
                columns: table => new
                {
                    ID = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    IDCarritoCompra = table.Column<long>(type: "bigint", nullable: false),
                    ShopProcessId = table.Column<long>(type: "bigint", nullable: false),
                    ProcessId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Monto = table.Column<decimal>(type: "numeric", nullable: false),
                    Status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    AuthorizationCode = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    RawBody = table.Column<string>(type: "text", nullable: true),
                    Fecha = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Activo = table.Column<bool>(type: "boolean", nullable: false),
                    UltModificacion = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UserName = table.Column<string>(type: "text", nullable: true),
                    IDEmpresa = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_logpagotarjeta", x => x.ID);
                });

            migrationBuilder.CreateTable(
                name: "migracionpedidolog",
                schema: "dbo",
                columns: table => new
                {
                    ID = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    IDPedidoMovil = table.Column<long>(type: "bigint", nullable: false),
                    IDCarritoCompra = table.Column<long>(type: "bigint", nullable: true),
                    NroPedido = table.Column<long>(type: "bigint", nullable: false),
                    Estado = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    MensajeError = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    DetalleTecnico = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    IDPedidoMovilErp = table.Column<long>(type: "bigint", nullable: true),
                    Intentos = table.Column<int>(type: "integer", nullable: false),
                    FechaIntento = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UserNameOperador = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Activo = table.Column<bool>(type: "boolean", nullable: false),
                    UltModificacion = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UserName = table.Column<string>(type: "text", nullable: true),
                    IDEmpresa = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_migracionpedidolog", x => x.ID);
                });

            migrationBuilder.CreateTable(
                name: "navegacionmenu",
                schema: "dbo",
                columns: table => new
                {
                    ID = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Code = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    MenuType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    PageCode = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true),
                    PageZone = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    DisplayOrder = table.Column<int>(type: "integer", nullable: false),
                    ContentStatus = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    SettingsJson = table.Column<string>(type: "text", nullable: true),
                    Activo = table.Column<bool>(type: "boolean", nullable: false),
                    UltModificacion = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UserName = table.Column<string>(type: "text", nullable: true),
                    IDEmpresa = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_navegacionmenu", x => x.ID);
                });

            migrationBuilder.CreateTable(
                name: "productoseguidoecommerce",
                schema: "dbo",
                columns: table => new
                {
                    ID = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    IDUsuarioEcommerce = table.Column<long>(type: "bigint", nullable: false),
                    IDArticulo = table.Column<long>(type: "bigint", nullable: false),
                    Email = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    CanalEcommerce = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    FechaSeguimiento = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Activo = table.Column<bool>(type: "boolean", nullable: false),
                    UltModificacion = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UserName = table.Column<string>(type: "text", nullable: true),
                    IDEmpresa = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_productoseguidoecommerce", x => x.ID);
                });

            migrationBuilder.CreateTable(
                name: "seguimientopedidoecommerce",
                schema: "dbo",
                columns: table => new
                {
                    ID = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    IDPedidoMovil = table.Column<long>(type: "bigint", nullable: false),
                    IDCarritoCompra = table.Column<long>(type: "bigint", nullable: true),
                    Estado = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    Mensaje = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    FechaEstado = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CodigoTracking = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Activo = table.Column<bool>(type: "boolean", nullable: false),
                    UltModificacion = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UserName = table.Column<string>(type: "text", nullable: true),
                    IDEmpresa = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_seguimientopedidoecommerce", x => x.ID);
                });

            migrationBuilder.CreateTable(
                name: "synccursor",
                schema: "dbo",
                columns: table => new
                {
                    ID = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    EntityKey = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    LastWatermarkUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastId = table.Column<long>(type: "bigint", nullable: true),
                    LastRunUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastStatus = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true),
                    LastError = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    RowsProcessed = table.Column<int>(type: "integer", nullable: false),
                    RowsRead = table.Column<int>(type: "integer", nullable: false),
                    RowsInserted = table.Column<int>(type: "integer", nullable: false),
                    RowsUpdated = table.Column<int>(type: "integer", nullable: false),
                    DurationMs = table.Column<long>(type: "bigint", nullable: false),
                    Activo = table.Column<bool>(type: "boolean", nullable: false),
                    UltModificacion = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UserName = table.Column<string>(type: "text", nullable: true),
                    IDEmpresa = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_synccursor", x => x.ID);
                });

            migrationBuilder.CreateTable(
                name: "tiendaecommerce",
                schema: "dbo",
                columns: table => new
                {
                    ID = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    IDClasificacionArticulo = table.Column<long>(type: "bigint", nullable: false),
                    TokenTienda = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    IDDepositoTienda = table.Column<long>(type: "bigint", nullable: false),
                    Nombre = table.Column<string>(type: "text", nullable: true),
                    HabilitaB2C = table.Column<bool>(type: "boolean", nullable: false),
                    HabilitaB2B = table.Column<bool>(type: "boolean", nullable: false),
                    Activo = table.Column<bool>(type: "boolean", nullable: false),
                    UltModificacion = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UserName = table.Column<string>(type: "text", nullable: true),
                    IDEmpresa = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tiendaecommerce", x => x.ID);
                });

            migrationBuilder.CreateTable(
                name: "tipodeposito",
                schema: "dbo",
                columns: table => new
                {
                    ID = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CodTipoDeposito = table.Column<int>(type: "integer", nullable: false),
                    DescripcionTipoDeposito = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Normal = table.Column<bool>(type: "boolean", nullable: false),
                    Consolidado = table.Column<bool>(type: "boolean", nullable: false),
                    Facturable = table.Column<bool>(type: "boolean", nullable: false),
                    PermiteFacturarBajoCosto = table.Column<bool>(type: "boolean", nullable: false),
                    Oferta = table.Column<bool>(type: "boolean", nullable: false),
                    Remate = table.Column<bool>(type: "boolean", nullable: false),
                    PuntoVenta = table.Column<bool>(type: "boolean", nullable: false),
                    Activo = table.Column<bool>(type: "boolean", nullable: false),
                    UltModificacion = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UserName = table.Column<string>(type: "text", nullable: true),
                    IDEmpresa = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tipodeposito", x => x.ID);
                });

            migrationBuilder.CreateTable(
                name: "tipodocumento",
                schema: "dbo",
                columns: table => new
                {
                    ID = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CodTipoDocumento = table.Column<int>(type: "integer", nullable: false),
                    DescripcionTipoDocumento = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    EsRuc = table.Column<bool>(type: "boolean", nullable: false),
                    Activo = table.Column<bool>(type: "boolean", nullable: false),
                    UltModificacion = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UserName = table.Column<string>(type: "text", nullable: true),
                    IDEmpresa = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tipodocumento", x => x.ID);
                });

            migrationBuilder.CreateTable(
                name: "unidadmedidaecommerce",
                schema: "dbo",
                columns: table => new
                {
                    ID = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Codigo = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    Nombre = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    Simbolo = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: true),
                    PermiteDecimales = table.Column<bool>(type: "boolean", nullable: false),
                    IncrementoDefault = table.Column<decimal>(type: "numeric", nullable: false),
                    Orden = table.Column<int>(type: "integer", nullable: false),
                    Activo = table.Column<bool>(type: "boolean", nullable: false),
                    UltModificacion = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UserName = table.Column<string>(type: "text", nullable: true),
                    IDEmpresa = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_unidadmedidaecommerce", x => x.ID);
                });

            migrationBuilder.CreateTable(
                name: "horarioatencion",
                schema: "dbo",
                columns: table => new
                {
                    ID = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    IDAgencia = table.Column<long>(type: "bigint", nullable: false),
                    DiaSemana = table.Column<byte>(type: "smallint", nullable: false),
                    HoraPrimeraApertura = table.Column<TimeSpan>(type: "interval", nullable: false),
                    HoraPrimerCierre = table.Column<TimeSpan>(type: "interval", nullable: false),
                    HoraSegundaApertura = table.Column<TimeSpan>(type: "interval", nullable: false),
                    HoraSegundoCierre = table.Column<TimeSpan>(type: "interval", nullable: false),
                    Activo = table.Column<bool>(type: "boolean", nullable: false),
                    UltModificacion = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UserName = table.Column<string>(type: "text", nullable: true),
                    IDEmpresa = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_horarioatencion", x => x.ID);
                    table.ForeignKey(
                        name: "FK_horarioatencion_agencia_IDAgencia",
                        column: x => x.IDAgencia,
                        principalSchema: "dbo",
                        principalTable: "agencia",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "articuloembalaje",
                schema: "dbo",
                columns: table => new
                {
                    ID = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    IDArticulo = table.Column<long>(type: "bigint", nullable: false),
                    IDEmbalaje = table.Column<long>(type: "bigint", nullable: false),
                    CantidadxEmbalaje = table.Column<double>(type: "double precision", nullable: false),
                    EmbalajeParaFacturacion = table.Column<bool>(type: "boolean", nullable: false),
                    EmbalajeParaControlStock = table.Column<bool>(type: "boolean", nullable: false),
                    CodigoBarras = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    DescripcionEmbalaje = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    PrecioDemo = table.Column<decimal>(type: "numeric", nullable: true),
                    CodigoUnidadMedida = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    PermiteFraccion = table.Column<bool>(type: "boolean", nullable: false),
                    IncrementoVenta = table.Column<decimal>(type: "numeric", nullable: false),
                    CantidadMinimaVenta = table.Column<decimal>(type: "numeric", nullable: false),
                    Activo = table.Column<bool>(type: "boolean", nullable: false),
                    UltModificacion = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UserName = table.Column<string>(type: "text", nullable: true),
                    IDEmpresa = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_articuloembalaje", x => x.ID);
                    table.ForeignKey(
                        name: "FK_articuloembalaje_articulo_IDArticulo",
                        column: x => x.IDArticulo,
                        principalSchema: "dbo",
                        principalTable: "articulo",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "articuloimagen",
                schema: "dbo",
                columns: table => new
                {
                    ID = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    IDArticulo = table.Column<long>(type: "bigint", nullable: false),
                    Url = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Titulo = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Orden = table.Column<int>(type: "integer", nullable: false),
                    EsPrincipal = table.Column<bool>(type: "boolean", nullable: false),
                    Activo = table.Column<bool>(type: "boolean", nullable: false),
                    UltModificacion = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UserName = table.Column<string>(type: "text", nullable: true),
                    IDEmpresa = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_articuloimagen", x => x.ID);
                    table.ForeignKey(
                        name: "FK_articuloimagen_articulo_IDArticulo",
                        column: x => x.IDArticulo,
                        principalSchema: "dbo",
                        principalTable: "articulo",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "pedidomovil",
                schema: "dbo",
                columns: table => new
                {
                    ID = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    NroPedido = table.Column<long>(type: "bigint", nullable: false),
                    NroFactura = table.Column<long>(type: "bigint", nullable: true),
                    IDCuenta = table.Column<long>(type: "bigint", nullable: false),
                    IDVendedor = table.Column<long>(type: "bigint", nullable: false),
                    IDDeposito = table.Column<long>(type: "bigint", nullable: false),
                    IndiceComision = table.Column<double>(type: "double precision", nullable: false),
                    IDCotizacionComercial = table.Column<long>(type: "bigint", nullable: false),
                    IDCondicionVenta = table.Column<long>(type: "bigint", nullable: false),
                    IDListaPrecioCuenta = table.Column<long>(type: "bigint", nullable: true),
                    IDUsuario = table.Column<long>(type: "bigint", nullable: false),
                    FechaPedido = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    MontoCambio = table.Column<decimal>(type: "numeric", nullable: false),
                    FechaEmision = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Plazo = table.Column<int>(type: "integer", nullable: false),
                    PorcentajeDescuento = table.Column<double>(type: "double precision", nullable: false),
                    TipoFactura = table.Column<int>(type: "integer", nullable: false),
                    TipoPedido = table.Column<int>(type: "integer", nullable: true),
                    TipoPrecio = table.Column<int>(type: "integer", nullable: false),
                    FechaPrevistaEntrega = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Estado = table.Column<int>(type: "integer", nullable: false),
                    NroPedidoDispositivoMovil = table.Column<long>(type: "bigint", nullable: false),
                    Migrado = table.Column<bool>(type: "boolean", nullable: false),
                    Momento = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Host = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Observacion = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Latitud = table.Column<decimal>(type: "numeric", nullable: true),
                    Longitud = table.Column<decimal>(type: "numeric", nullable: true),
                    Precision = table.Column<decimal>(type: "numeric", nullable: true),
                    NroOrdenCompra = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    OrigenPedido = table.Column<int>(type: "integer", nullable: false),
                    MotivoPendiente = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    IDChofer = table.Column<long>(type: "bigint", nullable: true),
                    DireccionEntrega = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    TokenTrackingPublico = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    Activo = table.Column<bool>(type: "boolean", nullable: false),
                    UltModificacion = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UserName = table.Column<string>(type: "text", nullable: true),
                    IDEmpresa = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_pedidomovil", x => x.ID);
                    table.ForeignKey(
                        name: "FK_pedidomovil_chofer_IDChofer",
                        column: x => x.IDChofer,
                        principalSchema: "dbo",
                        principalTable: "chofer",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "contenidobloqueitem",
                schema: "dbo",
                columns: table => new
                {
                    ID = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    IDContenidoBloque = table.Column<long>(type: "bigint", nullable: false),
                    ParentItemId = table.Column<long>(type: "bigint", nullable: true),
                    ItemType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Title = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: true),
                    Subtitle = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: true),
                    Description = table.Column<string>(type: "text", nullable: true),
                    ImageUrl = table.Column<string>(type: "character varying(800)", maxLength: 800, nullable: true),
                    MobileImageUrl = table.Column<string>(type: "character varying(800)", maxLength: 800, nullable: true),
                    IconUrl = table.Column<string>(type: "text", nullable: true),
                    LinkUrl = table.Column<string>(type: "character varying(800)", maxLength: 800, nullable: true),
                    LinkTarget = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    BadgeText = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    BadgeType = table.Column<string>(type: "text", nullable: true),
                    PriceText = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    CountdownEndAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DisplayOrder = table.Column<int>(type: "integer", nullable: false),
                    ContentStatus = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    StartDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    EndDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    SettingsJson = table.Column<string>(type: "text", nullable: true),
                    Activo = table.Column<bool>(type: "boolean", nullable: false),
                    UltModificacion = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UserName = table.Column<string>(type: "text", nullable: true),
                    IDEmpresa = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_contenidobloqueitem", x => x.ID);
                    table.ForeignKey(
                        name: "FK_contenidobloqueitem_contenidobloque_IDContenidoBloque",
                        column: x => x.IDContenidoBloque,
                        principalSchema: "dbo",
                        principalTable: "contenidobloque",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "usocuponecommerce",
                schema: "dbo",
                columns: table => new
                {
                    ID = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    IDCuponEcommerce = table.Column<long>(type: "bigint", nullable: false),
                    IDUsuarioEcommerce = table.Column<long>(type: "bigint", nullable: true),
                    IDCarritoCompra = table.Column<long>(type: "bigint", nullable: true),
                    Email = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    MontoDescontado = table.Column<decimal>(type: "numeric", nullable: false),
                    FechaUso = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Activo = table.Column<bool>(type: "boolean", nullable: false),
                    UltModificacion = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UserName = table.Column<string>(type: "text", nullable: true),
                    IDEmpresa = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_usocuponecommerce", x => x.ID);
                    table.ForeignKey(
                        name: "FK_usocuponecommerce_cuponecommerce_IDCuponEcommerce",
                        column: x => x.IDCuponEcommerce,
                        principalSchema: "dbo",
                        principalTable: "cuponecommerce",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ciudad",
                schema: "dbo",
                columns: table => new
                {
                    ID = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CodCiudad = table.Column<int>(type: "integer", nullable: false),
                    DescripcionCiudad = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    IDDivisionPolitica = table.Column<long>(type: "bigint", nullable: false),
                    CodigoSipap = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    Activo = table.Column<bool>(type: "boolean", nullable: false),
                    UltModificacion = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UserName = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ciudad", x => x.ID);
                    table.ForeignKey(
                        name: "FK_ciudad_divisionpolitica_IDDivisionPolitica",
                        column: x => x.IDDivisionPolitica,
                        principalSchema: "dbo",
                        principalTable: "divisionpolitica",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "empresaconfiguracionecommerce",
                schema: "dbo",
                columns: table => new
                {
                    ID = table.Column<long>(type: "bigint", nullable: false),
                    RealizaDelivery = table.Column<bool>(type: "boolean", nullable: false),
                    RealizaPickup = table.Column<bool>(type: "boolean", nullable: false),
                    CobraEnLaEntrega = table.Column<bool>(type: "boolean", nullable: false),
                    CobraAlHacerPedido = table.Column<bool>(type: "boolean", nullable: false),
                    MontoPedidoMinimo = table.Column<decimal>(type: "numeric", nullable: false),
                    ProcesaPedidosPorPlanilla = table.Column<bool>(type: "boolean", nullable: false),
                    ProcesaPedidosERP = table.Column<bool>(type: "boolean", nullable: false),
                    NroUltPedidoCarritoCompra = table.Column<long>(type: "bigint", nullable: false),
                    IDArticuloDelivery = table.Column<long>(type: "bigint", nullable: true),
                    ControlaStockEnCarrito = table.Column<bool>(type: "boolean", nullable: false),
                    ModoIntegracion = table.Column<int>(type: "integer", nullable: false),
                    ClavePublicaProcesadoraTarjeta = table.Column<string>(type: "text", nullable: true),
                    ClavePrivadaProcesadoraTarjeta = table.Column<string>(type: "text", nullable: true),
                    DireccionURLCheckout = table.Column<string>(type: "text", nullable: true),
                    CodigoComercio = table.Column<long>(type: "bigint", nullable: true),
                    CodigoSucursal = table.Column<long>(type: "bigint", nullable: true),
                    UltIDProcesoNegocio = table.Column<long>(type: "bigint", nullable: false),
                    IDDeposito = table.Column<long>(type: "bigint", nullable: false),
                    DireccionURLFacebook = table.Column<string>(type: "text", nullable: true),
                    DireccionURLTwitter = table.Column<string>(type: "text", nullable: true),
                    DireccionURLInstagram = table.Column<string>(type: "text", nullable: true),
                    DireccionURLYoutube = table.Column<string>(type: "text", nullable: true),
                    DireccionURLWhatsapp = table.Column<string>(type: "text", nullable: true),
                    SlugTienda = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    DominioCustom = table.Column<string>(type: "text", nullable: true),
                    MontoUmbralMayorista = table.Column<decimal>(type: "numeric", nullable: false),
                    UsarListaPrecioCuentaErp = table.Column<bool>(type: "boolean", nullable: false),
                    IDListaPrecioMayoristaDefault = table.Column<long>(type: "bigint", nullable: true),
                    LogoUrl = table.Column<string>(type: "text", nullable: true),
                    FaviconUrl = table.Column<string>(type: "text", nullable: true),
                    BannerUrl = table.Column<string>(type: "text", nullable: true),
                    ColorPrimario = table.Column<string>(type: "text", nullable: true),
                    ColorSecundario = table.Column<string>(type: "text", nullable: true),
                    ColorAcento = table.Column<string>(type: "text", nullable: true),
                    NombreTiendaDisplay = table.Column<string>(type: "text", nullable: true),
                    TaglineTienda = table.Column<string>(type: "text", nullable: true),
                    LogoMark = table.Column<string>(type: "text", nullable: true),
                    FooterDescripcion = table.Column<string>(type: "text", nullable: true),
                    DireccionLocal = table.Column<string>(type: "text", nullable: true),
                    CiudadLocal = table.Column<string>(type: "text", nullable: true),
                    TelefonoContacto = table.Column<string>(type: "text", nullable: true),
                    EmailContacto = table.Column<string>(type: "text", nullable: true),
                    HorarioAtencionTexto = table.Column<string>(type: "text", nullable: true),
                    LatitudLocal = table.Column<decimal>(type: "numeric", nullable: true),
                    LongitudLocal = table.Column<decimal>(type: "numeric", nullable: true),
                    MostrarMapaFooter = table.Column<bool>(type: "boolean", nullable: false),
                    DireccionURLTikTok = table.Column<string>(type: "text", nullable: true),
                    Activo = table.Column<bool>(type: "boolean", nullable: false),
                    UltModificacion = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UserName = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_empresaconfiguracionecommerce", x => x.ID);
                    table.ForeignKey(
                        name: "FK_empresaconfiguracionecommerce_empresa_ID",
                        column: x => x.ID,
                        principalSchema: "dbo",
                        principalTable: "empresa",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "detallegrupoarticuloecommerce",
                schema: "dbo",
                columns: table => new
                {
                    ID = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    IDGrupoArticuloEcommerce = table.Column<long>(type: "bigint", nullable: false),
                    IDArticulo = table.Column<long>(type: "bigint", nullable: false),
                    Activo = table.Column<bool>(type: "boolean", nullable: false),
                    UltModificacion = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UserName = table.Column<string>(type: "text", nullable: true),
                    IDEmpresa = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_detallegrupoarticuloecommerce", x => x.ID);
                    table.ForeignKey(
                        name: "FK_detallegrupoarticuloecommerce_articulo_IDArticulo",
                        column: x => x.IDArticulo,
                        principalSchema: "dbo",
                        principalTable: "articulo",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_detallegrupoarticuloecommerce_grupoarticuloecommerce_IDGrup~",
                        column: x => x.IDGrupoArticuloEcommerce,
                        principalSchema: "dbo",
                        principalTable: "grupoarticuloecommerce",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "listapreciocuenta",
                schema: "dbo",
                columns: table => new
                {
                    ID = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    IDListaPrecio = table.Column<long>(type: "bigint", nullable: false),
                    IDCuenta = table.Column<long>(type: "bigint", nullable: false),
                    Activo = table.Column<bool>(type: "boolean", nullable: false),
                    UltModificacion = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UserName = table.Column<string>(type: "text", nullable: true),
                    IDEmpresa = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_listapreciocuenta", x => x.ID);
                    table.ForeignKey(
                        name: "FK_listapreciocuenta_cuenta_IDCuenta",
                        column: x => x.IDCuenta,
                        principalSchema: "dbo",
                        principalTable: "cuenta",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_listapreciocuenta_listaprecio_IDListaPrecio",
                        column: x => x.IDListaPrecio,
                        principalSchema: "dbo",
                        principalTable: "listaprecio",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "navegacionmenuitem",
                schema: "dbo",
                columns: table => new
                {
                    ID = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    IDNavegacionMenu = table.Column<long>(type: "bigint", nullable: false),
                    ParentItemId = table.Column<long>(type: "bigint", nullable: true),
                    IDGrupoArticulo = table.Column<long>(type: "bigint", nullable: true),
                    ItemType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Label = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    Url = table.Column<string>(type: "character varying(800)", maxLength: 800, nullable: true),
                    Target = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    IconUrl = table.Column<string>(type: "text", nullable: true),
                    BadgeText = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    DisplayOrder = table.Column<int>(type: "integer", nullable: false),
                    ContentStatus = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Activo = table.Column<bool>(type: "boolean", nullable: false),
                    UltModificacion = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UserName = table.Column<string>(type: "text", nullable: true),
                    IDEmpresa = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_navegacionmenuitem", x => x.ID);
                    table.ForeignKey(
                        name: "FK_navegacionmenuitem_navegacionmenu_IDNavegacionMenu",
                        column: x => x.IDNavegacionMenu,
                        principalSchema: "dbo",
                        principalTable: "navegacionmenu",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "deposito",
                schema: "dbo",
                columns: table => new
                {
                    ID = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CodDeposito = table.Column<int>(type: "integer", nullable: false),
                    IDTipoDeposito = table.Column<long>(type: "bigint", nullable: false),
                    DescripcionDeposito = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    DepositoEntrega = table.Column<bool>(type: "boolean", nullable: false),
                    FacturacionMoviles = table.Column<bool>(type: "boolean", nullable: false),
                    DepositoDelCliente = table.Column<bool>(type: "boolean", nullable: false),
                    EsDepositoEcommerce = table.Column<bool>(type: "boolean", nullable: false),
                    Activo = table.Column<bool>(type: "boolean", nullable: false),
                    UltModificacion = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UserName = table.Column<string>(type: "text", nullable: true),
                    IDEmpresa = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_deposito", x => x.ID);
                    table.ForeignKey(
                        name: "FK_deposito_tipodeposito_IDTipoDeposito",
                        column: x => x.IDTipoDeposito,
                        principalSchema: "dbo",
                        principalTable: "tipodeposito",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "usuarioecommerce",
                schema: "dbo",
                columns: table => new
                {
                    ID = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CodigoUsuario = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Usuario = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Password = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Nombre = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Apellido = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    IDTipoDocumento = table.Column<long>(type: "bigint", nullable: true),
                    Email = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: true),
                    NumeroTelefono = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    NumeroMovil = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    NumeroDocumento = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: true),
                    FechaRegistro = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Activado = table.Column<bool>(type: "boolean", nullable: false),
                    Bloqueado = table.Column<bool>(type: "boolean", nullable: false),
                    CodigoActivacion = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    ChecksumActivacion = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    CheckSum = table.Column<string>(type: "text", nullable: true),
                    CodigoResetPassword = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    ChecksumResetPassword = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    FechaExpiraResetPassword = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CanalPreferido = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    EstadoMayorista = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    FechaSolicitudMayorista = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    MotivoSolicitudMayorista = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    MontoComprasAcumulado = table.Column<decimal>(type: "numeric", nullable: false),
                    Activo = table.Column<bool>(type: "boolean", nullable: false),
                    UltModificacion = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UserName = table.Column<string>(type: "text", nullable: true),
                    IDEmpresa = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_usuarioecommerce", x => x.ID);
                    table.ForeignKey(
                        name: "FK_usuarioecommerce_tipodocumento_IDTipoDocumento",
                        column: x => x.IDTipoDocumento,
                        principalSchema: "dbo",
                        principalTable: "tipodocumento",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "articuloembalajecodigobarra",
                schema: "dbo",
                columns: table => new
                {
                    ID = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    IDArticuloEmbalaje = table.Column<long>(type: "bigint", nullable: false),
                    CodigoBarra = table.Column<string>(type: "character varying(400)", maxLength: 400, nullable: false),
                    ParaSupermercado = table.Column<bool>(type: "boolean", nullable: false),
                    Activo = table.Column<bool>(type: "boolean", nullable: false),
                    UltModificacion = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UserName = table.Column<string>(type: "text", nullable: true),
                    IDEmpresa = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_articuloembalajecodigobarra", x => x.ID);
                    table.ForeignKey(
                        name: "FK_articuloembalajecodigobarra_articuloembalaje_IDArticuloEmba~",
                        column: x => x.IDArticuloEmbalaje,
                        principalSchema: "dbo",
                        principalTable: "articuloembalaje",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "detallelistaprecio",
                schema: "dbo",
                columns: table => new
                {
                    ID = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    IDListaPrecio = table.Column<long>(type: "bigint", nullable: false),
                    IDArticuloEmbalaje = table.Column<long>(type: "bigint", nullable: false),
                    Precio = table.Column<decimal>(type: "numeric", nullable: false),
                    Activo = table.Column<bool>(type: "boolean", nullable: false),
                    UltModificacion = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UserName = table.Column<string>(type: "text", nullable: true),
                    IDEmpresa = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_detallelistaprecio", x => x.ID);
                    table.ForeignKey(
                        name: "FK_detallelistaprecio_articuloembalaje_IDArticuloEmbalaje",
                        column: x => x.IDArticuloEmbalaje,
                        principalSchema: "dbo",
                        principalTable: "articuloembalaje",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_detallelistaprecio_listaprecio_IDListaPrecio",
                        column: x => x.IDListaPrecio,
                        principalSchema: "dbo",
                        principalTable: "listaprecio",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "detallelistapreciocondicionventa",
                schema: "dbo",
                columns: table => new
                {
                    ID = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    IDListaPrecio = table.Column<long>(type: "bigint", nullable: false),
                    IDArticuloEmbalaje = table.Column<long>(type: "bigint", nullable: false),
                    PrecioContado = table.Column<decimal>(type: "numeric", nullable: false),
                    PrecioCheque = table.Column<decimal>(type: "numeric", nullable: false),
                    PrecioCredito = table.Column<decimal>(type: "numeric", nullable: false),
                    CantidadMinimaMayorista = table.Column<decimal>(type: "numeric", nullable: false),
                    PorDescuentoMinorista = table.Column<decimal>(type: "numeric", nullable: false),
                    PorDescuentoMayorista = table.Column<decimal>(type: "numeric", nullable: false),
                    EsAutomatico = table.Column<bool>(type: "boolean", nullable: false),
                    Activo = table.Column<bool>(type: "boolean", nullable: false),
                    UltModificacion = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UserName = table.Column<string>(type: "text", nullable: true),
                    IDEmpresa = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_detallelistapreciocondicionventa", x => x.ID);
                    table.ForeignKey(
                        name: "FK_detallelistapreciocondicionventa_articuloembalaje_IDArticul~",
                        column: x => x.IDArticuloEmbalaje,
                        principalSchema: "dbo",
                        principalTable: "articuloembalaje",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_detallelistapreciocondicionventa_listaprecio_IDListaPrecio",
                        column: x => x.IDListaPrecio,
                        principalSchema: "dbo",
                        principalTable: "listaprecio",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "carritocompra",
                schema: "dbo",
                columns: table => new
                {
                    ID = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    NroPedidoCarritoCompra = table.Column<long>(type: "bigint", nullable: false),
                    IDCuenta = table.Column<long>(type: "bigint", nullable: true),
                    IDListaPrecio = table.Column<long>(type: "bigint", nullable: true),
                    IDCondicionVenta = table.Column<long>(type: "bigint", nullable: false),
                    IDVendedor = table.Column<long>(type: "bigint", nullable: false),
                    IDDeposito = table.Column<long>(type: "bigint", nullable: false),
                    IDCotizacionComercial = table.Column<long>(type: "bigint", nullable: false),
                    IDAgenciaFacturacion = table.Column<long>(type: "bigint", nullable: false),
                    FechaPedido = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    MontoCambio = table.Column<decimal>(type: "numeric", nullable: false),
                    Plazo = table.Column<int>(type: "integer", nullable: false),
                    PorcentajeDescuento = table.Column<decimal>(type: "numeric", nullable: false),
                    PorcentajeRecargo = table.Column<decimal>(type: "numeric", nullable: false),
                    TipoEntrega = table.Column<int>(type: "integer", nullable: false),
                    TipoPago = table.Column<int>(type: "integer", nullable: false),
                    FechaPrevistaEntrega = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LugarEntrega = table.Column<string>(type: "text", nullable: true),
                    Observacion = table.Column<string>(type: "text", nullable: true),
                    MontoTotal = table.Column<decimal>(type: "numeric", nullable: false),
                    MontoIVA = table.Column<decimal>(type: "numeric", nullable: false),
                    MontoExento = table.Column<decimal>(type: "numeric", nullable: false),
                    MontoGravado = table.Column<decimal>(type: "numeric", nullable: false),
                    MontoDescuento = table.Column<decimal>(type: "numeric", nullable: false),
                    MontoRecargo = table.Column<decimal>(type: "numeric", nullable: false),
                    RazonSocial = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    NumeroDocumento = table.Column<string>(type: "character varying(25)", maxLength: 25, nullable: false),
                    IDPedidoMovil = table.Column<long>(type: "bigint", nullable: true),
                    LatitudEntrega = table.Column<decimal>(type: "numeric", nullable: true),
                    LongitudEntrega = table.Column<decimal>(type: "numeric", nullable: true),
                    Estado = table.Column<int>(type: "integer", nullable: false),
                    Migrado = table.Column<bool>(type: "boolean", nullable: false),
                    IDSesion = table.Column<string>(type: "text", nullable: true),
                    IDProcesoNegocio = table.Column<long>(type: "bigint", nullable: true),
                    CodigoCuponAplicado = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true),
                    Activo = table.Column<bool>(type: "boolean", nullable: false),
                    UltModificacion = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UserName = table.Column<string>(type: "text", nullable: true),
                    IDEmpresa = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_carritocompra", x => x.ID);
                    table.ForeignKey(
                        name: "FK_carritocompra_pedidomovil_IDPedidoMovil",
                        column: x => x.IDPedidoMovil,
                        principalSchema: "dbo",
                        principalTable: "pedidomovil",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "detallepedidomovil",
                schema: "dbo",
                columns: table => new
                {
                    ID = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    IDPedidoMovil = table.Column<long>(type: "bigint", nullable: false),
                    IDArticulo = table.Column<long>(type: "bigint", nullable: false),
                    IDDetalleListaPrecio = table.Column<long>(type: "bigint", nullable: false),
                    IDUsuarioTraspaso = table.Column<long>(type: "bigint", nullable: false),
                    IDUsuario = table.Column<long>(type: "bigint", nullable: false),
                    CodigoBarra = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    MontoCambio = table.Column<double>(type: "double precision", nullable: false),
                    PorcentajeDescuento = table.Column<double>(type: "double precision", nullable: false),
                    PrecioBrutoConImpuesto = table.Column<double>(type: "double precision", nullable: false),
                    PrecioBrutoSinImpuesto = table.Column<double>(type: "double precision", nullable: false),
                    PrecioNetoConImpuesto = table.Column<double>(type: "double precision", nullable: false),
                    PrecioNetoSinImpuesto = table.Column<double>(type: "double precision", nullable: false),
                    CantidadEmbalada = table.Column<int>(type: "integer", nullable: false),
                    CantidadFraccionada = table.Column<int>(type: "integer", nullable: false),
                    Traspaso = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Momento = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Host = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Observacion = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    NroOrden = table.Column<int>(type: "integer", nullable: true),
                    DiferenciaPrecioSupermercado = table.Column<bool>(type: "boolean", nullable: false),
                    PrecioLista = table.Column<decimal>(type: "numeric", nullable: true),
                    Activo = table.Column<bool>(type: "boolean", nullable: false),
                    UltModificacion = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UserName = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_detallepedidomovil", x => x.ID);
                    table.ForeignKey(
                        name: "FK_detallepedidomovil_articulo_IDArticulo",
                        column: x => x.IDArticulo,
                        principalSchema: "dbo",
                        principalTable: "articulo",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_detallepedidomovil_pedidomovil_IDPedidoMovil",
                        column: x => x.IDPedidoMovil,
                        principalSchema: "dbo",
                        principalTable: "pedidomovil",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "localidad",
                schema: "dbo",
                columns: table => new
                {
                    ID = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CodLocalidad = table.Column<int>(type: "integer", nullable: false),
                    DescripcionLocalidad = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    IDCiudad = table.Column<long>(type: "bigint", nullable: false),
                    Activo = table.Column<bool>(type: "boolean", nullable: false),
                    UltModificacion = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UserName = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_localidad", x => x.ID);
                    table.ForeignKey(
                        name: "FK_localidad_ciudad_IDCiudad",
                        column: x => x.IDCiudad,
                        principalSchema: "dbo",
                        principalTable: "ciudad",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "stock",
                schema: "dbo",
                columns: table => new
                {
                    ID = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    IDDeposito = table.Column<long>(type: "bigint", nullable: false),
                    IDArticulo = table.Column<long>(type: "bigint", nullable: false),
                    IDContenedor = table.Column<long>(type: "bigint", nullable: true),
                    IDLoteArticulo = table.Column<long>(type: "bigint", nullable: true),
                    CostoGestionUltimo = table.Column<double>(type: "double precision", nullable: false),
                    CostoGestionPromedio = table.Column<double>(type: "double precision", nullable: false),
                    CostoGestionFIFO = table.Column<double>(type: "double precision", nullable: false),
                    CostoGestionLIFO = table.Column<double>(type: "double precision", nullable: false),
                    CostoGestionNIFO = table.Column<double>(type: "double precision", nullable: false),
                    CostoUltimo = table.Column<double>(type: "double precision", nullable: false),
                    CostoPromedio = table.Column<double>(type: "double precision", nullable: false),
                    CostoFIFO = table.Column<double>(type: "double precision", nullable: false),
                    CostoLIFO = table.Column<double>(type: "double precision", nullable: false),
                    CostoNIFO = table.Column<double>(type: "double precision", nullable: false),
                    PuntoReposicion = table.Column<double>(type: "double precision", nullable: false),
                    LoteReposicion = table.Column<double>(type: "double precision", nullable: false),
                    TotalCompra = table.Column<double>(type: "double precision", nullable: false),
                    TotalDevolucionCompra = table.Column<double>(type: "double precision", nullable: false),
                    TotalPedidoVenta = table.Column<double>(type: "double precision", nullable: false),
                    TotalVenta = table.Column<double>(type: "double precision", nullable: false),
                    TotalDevolucionVenta = table.Column<double>(type: "double precision", nullable: false),
                    TotalTransferenciaSalida = table.Column<double>(type: "double precision", nullable: false),
                    TotalTransferenciaEntrada = table.Column<double>(type: "double precision", nullable: false),
                    TotalIndustrializacionIngreso = table.Column<double>(type: "double precision", nullable: false),
                    TotalIndustrializacionEgreso = table.Column<double>(type: "double precision", nullable: false),
                    TotalAjustePositivo = table.Column<double>(type: "double precision", nullable: false),
                    TotalAjusteNegativo = table.Column<double>(type: "double precision", nullable: false),
                    TotalTransito = table.Column<double>(type: "double precision", nullable: false),
                    TotalExistencia = table.Column<double>(type: "double precision", nullable: false),
                    ExistenciaCongelada = table.Column<bool>(type: "boolean", nullable: false),
                    TotalExistenciaInicial = table.Column<double>(type: "double precision", nullable: false),
                    CostoUnitarioInicial = table.Column<decimal>(type: "numeric", nullable: false),
                    TotalReserva = table.Column<decimal>(type: "numeric", nullable: false),
                    Activo = table.Column<bool>(type: "boolean", nullable: false),
                    UltModificacion = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UserName = table.Column<string>(type: "text", nullable: true),
                    IDEmpresa = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_stock", x => x.ID);
                    table.ForeignKey(
                        name: "FK_stock_articulo_IDArticulo",
                        column: x => x.IDArticulo,
                        principalSchema: "dbo",
                        principalTable: "articulo",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_stock_deposito_IDDeposito",
                        column: x => x.IDDeposito,
                        principalSchema: "dbo",
                        principalTable: "deposito",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "chatconversacionecommerce",
                schema: "dbo",
                columns: table => new
                {
                    ID = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    IDUsuarioEcommerce = table.Column<long>(type: "bigint", nullable: false),
                    IDPedidoMovil = table.Column<long>(type: "bigint", nullable: true),
                    Email = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    NombreCliente = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    Asunto = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    Estado = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    IDAdminAsignado = table.Column<long>(type: "bigint", nullable: true),
                    NombreAdminAsignado = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    FechaInicio = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    FechaUltimoMensaje = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    PreviewUltimoMensaje = table.Column<string>(type: "character varying(240)", maxLength: 240, nullable: true),
                    NoLeidosCliente = table.Column<int>(type: "integer", nullable: false),
                    NoLeidosSoporte = table.Column<int>(type: "integer", nullable: false),
                    Activo = table.Column<bool>(type: "boolean", nullable: false),
                    UltModificacion = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UserName = table.Column<string>(type: "text", nullable: true),
                    IDEmpresa = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_chatconversacionecommerce", x => x.ID);
                    table.ForeignKey(
                        name: "FK_chatconversacionecommerce_usuarioecommerce_IDUsuarioEcommer~",
                        column: x => x.IDUsuarioEcommerce,
                        principalSchema: "dbo",
                        principalTable: "usuarioecommerce",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "listafavoritoecommerce",
                schema: "dbo",
                columns: table => new
                {
                    ID = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    IDUsuarioEcommerce = table.Column<long>(type: "bigint", nullable: false),
                    IDCuenta = table.Column<long>(type: "bigint", nullable: true),
                    Email = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    NombreLista = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    CanalEcommerce = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    Orden = table.Column<int>(type: "integer", nullable: false),
                    Color = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    Activo = table.Column<bool>(type: "boolean", nullable: false),
                    UltModificacion = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UserName = table.Column<string>(type: "text", nullable: true),
                    IDEmpresa = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_listafavoritoecommerce", x => x.ID);
                    table.ForeignKey(
                        name: "FK_listafavoritoecommerce_usuarioecommerce_IDUsuarioEcommerce",
                        column: x => x.IDUsuarioEcommerce,
                        principalSchema: "dbo",
                        principalTable: "usuarioecommerce",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "resenaarticuloecommerce",
                schema: "dbo",
                columns: table => new
                {
                    ID = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    IDArticulo = table.Column<long>(type: "bigint", nullable: false),
                    IDUsuarioEcommerce = table.Column<long>(type: "bigint", nullable: false),
                    IDCuenta = table.Column<long>(type: "bigint", nullable: true),
                    IDPedidoMovil = table.Column<long>(type: "bigint", nullable: true),
                    IDCarritoCompra = table.Column<long>(type: "bigint", nullable: true),
                    Email = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    CanalEcommerce = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    Puntuacion = table.Column<int>(type: "integer", nullable: false),
                    Titulo = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    Comentario = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    Aprobado = table.Column<bool>(type: "boolean", nullable: false),
                    FechaResena = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Activo = table.Column<bool>(type: "boolean", nullable: false),
                    UltModificacion = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UserName = table.Column<string>(type: "text", nullable: true),
                    IDEmpresa = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_resenaarticuloecommerce", x => x.ID);
                    table.ForeignKey(
                        name: "FK_resenaarticuloecommerce_articulo_IDArticulo",
                        column: x => x.IDArticulo,
                        principalSchema: "dbo",
                        principalTable: "articulo",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_resenaarticuloecommerce_usuarioecommerce_IDUsuarioEcommerce",
                        column: x => x.IDUsuarioEcommerce,
                        principalSchema: "dbo",
                        principalTable: "usuarioecommerce",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "usuarioecommercecuenta",
                schema: "dbo",
                columns: table => new
                {
                    ID = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    IDUsuarioEcommerce = table.Column<long>(type: "bigint", nullable: false),
                    IDCuenta = table.Column<long>(type: "bigint", nullable: false),
                    EsPrincipal = table.Column<bool>(type: "boolean", nullable: false),
                    Activo = table.Column<bool>(type: "boolean", nullable: false),
                    UltModificacion = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UserName = table.Column<string>(type: "text", nullable: true),
                    IDEmpresa = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_usuarioecommercecuenta", x => x.ID);
                    table.ForeignKey(
                        name: "FK_usuarioecommercecuenta_usuarioecommerce_IDUsuarioEcommerce",
                        column: x => x.IDUsuarioEcommerce,
                        principalSchema: "dbo",
                        principalTable: "usuarioecommerce",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "detallecarritocompra",
                schema: "dbo",
                columns: table => new
                {
                    ID = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    IDCarritoCompra = table.Column<long>(type: "bigint", nullable: false),
                    IDArticulo = table.Column<long>(type: "bigint", nullable: false),
                    CantidadxEmbalaje = table.Column<decimal>(type: "numeric", nullable: false),
                    IDRegimenImpositivo = table.Column<long>(type: "bigint", nullable: false),
                    PorcentajeDescuento = table.Column<decimal>(type: "numeric", nullable: false),
                    CantidadBonificada = table.Column<decimal>(type: "numeric", nullable: false),
                    CantidadEmbaladaBonificada = table.Column<int>(type: "integer", nullable: false),
                    CantidadFraccionadaBonificada = table.Column<int>(type: "integer", nullable: false),
                    Cantidad = table.Column<decimal>(type: "numeric", nullable: false),
                    CantidadEmbalada = table.Column<int>(type: "integer", nullable: false),
                    CantidadFraccionada = table.Column<int>(type: "integer", nullable: false),
                    PorcentajeImpuesto = table.Column<decimal>(type: "numeric", nullable: false),
                    PrecioBrutoConImpuesto = table.Column<decimal>(type: "numeric", nullable: false),
                    PrecioBrutoSinImpuesto = table.Column<decimal>(type: "numeric", nullable: false),
                    PrecioNetoConImpuesto = table.Column<decimal>(type: "numeric", nullable: false),
                    PrecioNetoSinImpuesto = table.Column<decimal>(type: "numeric", nullable: false),
                    MontoTotal = table.Column<decimal>(type: "numeric", nullable: false),
                    MontoIVA = table.Column<decimal>(type: "numeric", nullable: false),
                    Activo = table.Column<bool>(type: "boolean", nullable: false),
                    UltModificacion = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UserName = table.Column<string>(type: "text", nullable: true),
                    IDEmpresa = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_detallecarritocompra", x => x.ID);
                    table.ForeignKey(
                        name: "FK_detallecarritocompra_articulo_IDArticulo",
                        column: x => x.IDArticulo,
                        principalSchema: "dbo",
                        principalTable: "articulo",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_detallecarritocompra_carritocompra_IDCarritoCompra",
                        column: x => x.IDCarritoCompra,
                        principalSchema: "dbo",
                        principalTable: "carritocompra",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "distrito",
                schema: "dbo",
                columns: table => new
                {
                    ID = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CodDistrito = table.Column<int>(type: "integer", nullable: false),
                    DescripcionDistrito = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    IDLocalidad = table.Column<long>(type: "bigint", nullable: false),
                    Activo = table.Column<bool>(type: "boolean", nullable: false),
                    UltModificacion = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UserName = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_distrito", x => x.ID);
                    table.ForeignKey(
                        name: "FK_distrito_localidad_IDLocalidad",
                        column: x => x.IDLocalidad,
                        principalSchema: "dbo",
                        principalTable: "localidad",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "chatsoportemensajeecommerce",
                schema: "dbo",
                columns: table => new
                {
                    ID = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    IDConversacion = table.Column<long>(type: "bigint", nullable: true),
                    IDUsuarioEcommerce = table.Column<long>(type: "bigint", nullable: true),
                    IDPedidoMovil = table.Column<long>(type: "bigint", nullable: true),
                    IDAdminUsuario = table.Column<long>(type: "bigint", nullable: true),
                    Email = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    Origen = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    TipoMensaje = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Mensaje = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    UrlAdjunto = table.Column<string>(type: "character varying(800)", maxLength: 800, nullable: true),
                    MimeType = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    NombreArchivo = table.Column<string>(type: "character varying(260)", maxLength: 260, nullable: true),
                    FechaMensaje = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Leido = table.Column<bool>(type: "boolean", nullable: false),
                    Activo = table.Column<bool>(type: "boolean", nullable: false),
                    UltModificacion = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UserName = table.Column<string>(type: "text", nullable: true),
                    IDEmpresa = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_chatsoportemensajeecommerce", x => x.ID);
                    table.ForeignKey(
                        name: "FK_chatsoportemensajeecommerce_chatconversacionecommerce_IDCon~",
                        column: x => x.IDConversacion,
                        principalSchema: "dbo",
                        principalTable: "chatconversacionecommerce",
                        principalColumn: "ID");
                });

            migrationBuilder.CreateTable(
                name: "detallelistafavoritoecommerce",
                schema: "dbo",
                columns: table => new
                {
                    ID = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    IDListaFavoritoEcommerce = table.Column<long>(type: "bigint", nullable: false),
                    IDArticulo = table.Column<long>(type: "bigint", nullable: false),
                    FechaAgregado = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Nota = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    Activo = table.Column<bool>(type: "boolean", nullable: false),
                    UltModificacion = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UserName = table.Column<string>(type: "text", nullable: true),
                    IDEmpresa = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_detallelistafavoritoecommerce", x => x.ID);
                    table.ForeignKey(
                        name: "FK_detallelistafavoritoecommerce_articulo_IDArticulo",
                        column: x => x.IDArticulo,
                        principalSchema: "dbo",
                        principalTable: "articulo",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_detallelistafavoritoecommerce_listafavoritoecommerce_IDList~",
                        column: x => x.IDListaFavoritoEcommerce,
                        principalSchema: "dbo",
                        principalTable: "listafavoritoecommerce",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "barrio",
                schema: "dbo",
                columns: table => new
                {
                    ID = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CodBarrio = table.Column<int>(type: "integer", nullable: false),
                    DescripcionBarrio = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    IDDistrito = table.Column<long>(type: "bigint", nullable: true),
                    Activo = table.Column<bool>(type: "boolean", nullable: false),
                    UltModificacion = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UserName = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_barrio", x => x.ID);
                    table.ForeignKey(
                        name: "FK_barrio_distrito_IDDistrito",
                        column: x => x.IDDistrito,
                        principalSchema: "dbo",
                        principalTable: "distrito",
                        principalColumn: "ID");
                });

            migrationBuilder.CreateTable(
                name: "tarifadelivery",
                schema: "dbo",
                columns: table => new
                {
                    ID = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    IDDivisionPolitica = table.Column<long>(type: "bigint", nullable: false),
                    IDCiudad = table.Column<long>(type: "bigint", nullable: false),
                    IDBarrio = table.Column<long>(type: "bigint", nullable: false),
                    FechaInicioVigencia = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    FechaFinVigencia = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ImporteDelivery = table.Column<decimal>(type: "numeric", nullable: false),
                    Estado = table.Column<int>(type: "integer", nullable: false),
                    IDAgencia = table.Column<long>(type: "bigint", nullable: true),
                    NombreCiudad = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    NombreBarrio = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Activo = table.Column<bool>(type: "boolean", nullable: false),
                    UltModificacion = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UserName = table.Column<string>(type: "text", nullable: true),
                    IDEmpresa = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tarifadelivery", x => x.ID);
                    table.ForeignKey(
                        name: "FK_tarifadelivery_barrio_IDBarrio",
                        column: x => x.IDBarrio,
                        principalSchema: "dbo",
                        principalTable: "barrio",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_tarifadelivery_ciudad_IDCiudad",
                        column: x => x.IDCiudad,
                        principalSchema: "dbo",
                        principalTable: "ciudad",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_tarifadelivery_divisionpolitica_IDDivisionPolitica",
                        column: x => x.IDDivisionPolitica,
                        principalSchema: "dbo",
                        principalTable: "divisionpolitica",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_adminauditorialog_IDEmpresa_Fecha",
                schema: "dbo",
                table: "adminauditorialog",
                columns: new[] { "IDEmpresa", "Fecha" });

            migrationBuilder.CreateIndex(
                name: "IX_adminfuncionalidad_IDEmpresa_CodigoPermission",
                schema: "dbo",
                table: "adminfuncionalidad",
                columns: new[] { "IDEmpresa", "CodigoPermission" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_adminrol_IDEmpresa_CodigoRol",
                schema: "dbo",
                table: "adminrol",
                columns: new[] { "IDEmpresa", "CodigoRol" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_adminrolfuncionalidad_IDEmpresa_IDRol_IDFuncionalidad",
                schema: "dbo",
                table: "adminrolfuncionalidad",
                columns: new[] { "IDEmpresa", "IDRol", "IDFuncionalidad" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_adminusuario_IDEmpresa_Usuario",
                schema: "dbo",
                table: "adminusuario",
                columns: new[] { "IDEmpresa", "Usuario" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_adminusuariorol_IDEmpresa_IDUsuario_IDRol",
                schema: "dbo",
                table: "adminusuariorol",
                columns: new[] { "IDEmpresa", "IDUsuario", "IDRol" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_agencia_IDEmpresa_CodAgencia",
                schema: "dbo",
                table: "agencia",
                columns: new[] { "IDEmpresa", "CodAgencia" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_articulo_IDEmpresa_ActivoEcommerce",
                schema: "dbo",
                table: "articulo",
                columns: new[] { "IDEmpresa", "ActivoEcommerce" });

            migrationBuilder.CreateIndex(
                name: "IX_articulo_IDEmpresa_SKU",
                schema: "dbo",
                table: "articulo",
                columns: new[] { "IDEmpresa", "SKU" });

            migrationBuilder.CreateIndex(
                name: "IX_articulo_IDEmpresa_SlugSeo",
                schema: "dbo",
                table: "articulo",
                columns: new[] { "IDEmpresa", "SlugSeo" });

            migrationBuilder.CreateIndex(
                name: "IX_articuloembalaje_IDArticulo",
                schema: "dbo",
                table: "articuloembalaje",
                column: "IDArticulo");

            migrationBuilder.CreateIndex(
                name: "IX_articuloembalaje_IDEmpresa_IDArticulo",
                schema: "dbo",
                table: "articuloembalaje",
                columns: new[] { "IDEmpresa", "IDArticulo" });

            migrationBuilder.CreateIndex(
                name: "IX_articuloembalajecodigobarra_IDArticuloEmbalaje",
                schema: "dbo",
                table: "articuloembalajecodigobarra",
                column: "IDArticuloEmbalaje");

            migrationBuilder.CreateIndex(
                name: "IX_articuloembalajecodigobarra_IDEmpresa_CodigoBarra",
                schema: "dbo",
                table: "articuloembalajecodigobarra",
                columns: new[] { "IDEmpresa", "CodigoBarra" });

            migrationBuilder.CreateIndex(
                name: "IX_articuloembalajecodigobarra_IDEmpresa_IDArticuloEmbalaje",
                schema: "dbo",
                table: "articuloembalajecodigobarra",
                columns: new[] { "IDEmpresa", "IDArticuloEmbalaje" });

            migrationBuilder.CreateIndex(
                name: "IX_articuloimagen_IDArticulo",
                schema: "dbo",
                table: "articuloimagen",
                column: "IDArticulo");

            migrationBuilder.CreateIndex(
                name: "IX_articuloimagen_IDEmpresa_IDArticulo",
                schema: "dbo",
                table: "articuloimagen",
                columns: new[] { "IDEmpresa", "IDArticulo" });

            migrationBuilder.CreateIndex(
                name: "IX_barrio_CodBarrio",
                schema: "dbo",
                table: "barrio",
                column: "CodBarrio");

            migrationBuilder.CreateIndex(
                name: "IX_barrio_IDDistrito",
                schema: "dbo",
                table: "barrio",
                column: "IDDistrito");

            migrationBuilder.CreateIndex(
                name: "IX_carritocompra_IDEmpresa_IDSesion",
                schema: "dbo",
                table: "carritocompra",
                columns: new[] { "IDEmpresa", "IDSesion" });

            migrationBuilder.CreateIndex(
                name: "IX_carritocompra_IDEmpresa_NroPedidoCarritoCompra",
                schema: "dbo",
                table: "carritocompra",
                columns: new[] { "IDEmpresa", "NroPedidoCarritoCompra" });

            migrationBuilder.CreateIndex(
                name: "IX_carritocompra_IDPedidoMovil",
                schema: "dbo",
                table: "carritocompra",
                column: "IDPedidoMovil");

            migrationBuilder.CreateIndex(
                name: "IX_catalogoparametro_IDEmpresa_Tipo_Codigo",
                schema: "dbo",
                table: "catalogoparametro",
                columns: new[] { "IDEmpresa", "Tipo", "Codigo" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_chatconversacionecommerce_IDEmpresa_FechaUltimoMensaje",
                schema: "dbo",
                table: "chatconversacionecommerce",
                columns: new[] { "IDEmpresa", "FechaUltimoMensaje" });

            migrationBuilder.CreateIndex(
                name: "IX_chatconversacionecommerce_IDEmpresa_IDUsuarioEcommerce_Esta~",
                schema: "dbo",
                table: "chatconversacionecommerce",
                columns: new[] { "IDEmpresa", "IDUsuarioEcommerce", "Estado" });

            migrationBuilder.CreateIndex(
                name: "IX_chatconversacionecommerce_IDUsuarioEcommerce",
                schema: "dbo",
                table: "chatconversacionecommerce",
                column: "IDUsuarioEcommerce");

            migrationBuilder.CreateIndex(
                name: "IX_chatsoportemensajeecommerce_IDConversacion",
                schema: "dbo",
                table: "chatsoportemensajeecommerce",
                column: "IDConversacion");

            migrationBuilder.CreateIndex(
                name: "IX_chatsoportemensajeecommerce_IDEmpresa_IDConversacion_FechaM~",
                schema: "dbo",
                table: "chatsoportemensajeecommerce",
                columns: new[] { "IDEmpresa", "IDConversacion", "FechaMensaje" });

            migrationBuilder.CreateIndex(
                name: "IX_chatsoportemensajeecommerce_IDEmpresa_IDUsuarioEcommerce_Fe~",
                schema: "dbo",
                table: "chatsoportemensajeecommerce",
                columns: new[] { "IDEmpresa", "IDUsuarioEcommerce", "FechaMensaje" });

            migrationBuilder.CreateIndex(
                name: "IX_chofer_IDEmpresa_NroChofer",
                schema: "dbo",
                table: "chofer",
                columns: new[] { "IDEmpresa", "NroChofer" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ciudad_CodCiudad",
                schema: "dbo",
                table: "ciudad",
                column: "CodCiudad");

            migrationBuilder.CreateIndex(
                name: "IX_ciudad_IDDivisionPolitica",
                schema: "dbo",
                table: "ciudad",
                column: "IDDivisionPolitica");

            migrationBuilder.CreateIndex(
                name: "IX_configuracionpersonalizacion_IDEmpresa_Clave_Activo",
                schema: "dbo",
                table: "configuracionpersonalizacion",
                columns: new[] { "IDEmpresa", "Clave", "Activo" });

            migrationBuilder.CreateIndex(
                name: "IX_configuracionpersonalizacion_IDEmpresa_Clave_Version",
                schema: "dbo",
                table: "configuracionpersonalizacion",
                columns: new[] { "IDEmpresa", "Clave", "Version" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_contenidobloque_IDEmpresa_BlockType",
                schema: "dbo",
                table: "contenidobloque",
                columns: new[] { "IDEmpresa", "BlockType" });

            migrationBuilder.CreateIndex(
                name: "IX_contenidobloque_IDEmpresa_PageCode_DisplayOrder",
                schema: "dbo",
                table: "contenidobloque",
                columns: new[] { "IDEmpresa", "PageCode", "DisplayOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_contenidobloqueitem_IDContenidoBloque_DisplayOrder",
                schema: "dbo",
                table: "contenidobloqueitem",
                columns: new[] { "IDContenidoBloque", "DisplayOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_cuenta_IDEmpresa_NroCuenta",
                schema: "dbo",
                table: "cuenta",
                columns: new[] { "IDEmpresa", "NroCuenta" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_cuponecommerce_IDEmpresa_Codigo",
                schema: "dbo",
                table: "cuponecommerce",
                columns: new[] { "IDEmpresa", "Codigo" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_deposito_IDEmpresa_CodDeposito",
                schema: "dbo",
                table: "deposito",
                columns: new[] { "IDEmpresa", "CodDeposito" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_deposito_IDTipoDeposito",
                schema: "dbo",
                table: "deposito",
                column: "IDTipoDeposito");

            migrationBuilder.CreateIndex(
                name: "IX_detallecarritocompra_IDArticulo",
                schema: "dbo",
                table: "detallecarritocompra",
                column: "IDArticulo");

            migrationBuilder.CreateIndex(
                name: "IX_detallecarritocompra_IDCarritoCompra",
                schema: "dbo",
                table: "detallecarritocompra",
                column: "IDCarritoCompra");

            migrationBuilder.CreateIndex(
                name: "IX_detallegrupoarticuloecommerce_IDArticulo",
                schema: "dbo",
                table: "detallegrupoarticuloecommerce",
                column: "IDArticulo");

            migrationBuilder.CreateIndex(
                name: "IX_detallegrupoarticuloecommerce_IDEmpresa_IDGrupoArticuloEcom~",
                schema: "dbo",
                table: "detallegrupoarticuloecommerce",
                columns: new[] { "IDEmpresa", "IDGrupoArticuloEcommerce", "IDArticulo" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_detallegrupoarticuloecommerce_IDGrupoArticuloEcommerce",
                schema: "dbo",
                table: "detallegrupoarticuloecommerce",
                column: "IDGrupoArticuloEcommerce");

            migrationBuilder.CreateIndex(
                name: "IX_detallelistafavoritoecommerce_IDArticulo",
                schema: "dbo",
                table: "detallelistafavoritoecommerce",
                column: "IDArticulo");

            migrationBuilder.CreateIndex(
                name: "IX_detallelistafavoritoecommerce_IDEmpresa_IDListaFavoritoEcom~",
                schema: "dbo",
                table: "detallelistafavoritoecommerce",
                columns: new[] { "IDEmpresa", "IDListaFavoritoEcommerce", "IDArticulo" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_detallelistafavoritoecommerce_IDListaFavoritoEcommerce",
                schema: "dbo",
                table: "detallelistafavoritoecommerce",
                column: "IDListaFavoritoEcommerce");

            migrationBuilder.CreateIndex(
                name: "IX_detallelistaprecio_IDArticuloEmbalaje",
                schema: "dbo",
                table: "detallelistaprecio",
                column: "IDArticuloEmbalaje");

            migrationBuilder.CreateIndex(
                name: "IX_detallelistaprecio_IDEmpresa_IDListaPrecio_IDArticuloEmbala~",
                schema: "dbo",
                table: "detallelistaprecio",
                columns: new[] { "IDEmpresa", "IDListaPrecio", "IDArticuloEmbalaje" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_detallelistaprecio_IDListaPrecio",
                schema: "dbo",
                table: "detallelistaprecio",
                column: "IDListaPrecio");

            migrationBuilder.CreateIndex(
                name: "IX_detallelistapreciocondicionventa_IDArticuloEmbalaje",
                schema: "dbo",
                table: "detallelistapreciocondicionventa",
                column: "IDArticuloEmbalaje");

            migrationBuilder.CreateIndex(
                name: "IX_detallelistapreciocondicionventa_IDEmpresa_IDListaPrecio_ID~",
                schema: "dbo",
                table: "detallelistapreciocondicionventa",
                columns: new[] { "IDEmpresa", "IDListaPrecio", "IDArticuloEmbalaje" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_detallelistapreciocondicionventa_IDListaPrecio",
                schema: "dbo",
                table: "detallelistapreciocondicionventa",
                column: "IDListaPrecio");

            migrationBuilder.CreateIndex(
                name: "IX_detallepedidomovil_IDArticulo",
                schema: "dbo",
                table: "detallepedidomovil",
                column: "IDArticulo");

            migrationBuilder.CreateIndex(
                name: "IX_detallepedidomovil_IDPedidoMovil",
                schema: "dbo",
                table: "detallepedidomovil",
                column: "IDPedidoMovil");

            migrationBuilder.CreateIndex(
                name: "IX_devolucionecommerce_IDEmpresa_IDUsuarioEcommerce",
                schema: "dbo",
                table: "devolucionecommerce",
                columns: new[] { "IDEmpresa", "IDUsuarioEcommerce" });

            migrationBuilder.CreateIndex(
                name: "IX_distrito_IDLocalidad",
                schema: "dbo",
                table: "distrito",
                column: "IDLocalidad");

            migrationBuilder.CreateIndex(
                name: "IX_divisionpolitica_CodDivisionPolitica",
                schema: "dbo",
                table: "divisionpolitica",
                column: "CodDivisionPolitica");

            migrationBuilder.CreateIndex(
                name: "IX_empresaconfiguracionecommerce_SlugTienda",
                schema: "dbo",
                table: "empresaconfiguracionecommerce",
                column: "SlugTienda",
                unique: true,
                filter: "\"SlugTienda\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_horarioatencion_IDAgencia",
                schema: "dbo",
                table: "horarioatencion",
                column: "IDAgencia");

            migrationBuilder.CreateIndex(
                name: "IX_horarioatencion_IDEmpresa_IDAgencia_DiaSemana",
                schema: "dbo",
                table: "horarioatencion",
                columns: new[] { "IDEmpresa", "IDAgencia", "DiaSemana" });

            migrationBuilder.CreateIndex(
                name: "IX_interaccionarticuloecommerce_IDEmpresa_IDArticulo_Momento",
                schema: "dbo",
                table: "interaccionarticuloecommerce",
                columns: new[] { "IDEmpresa", "IDArticulo", "Momento" });

            migrationBuilder.CreateIndex(
                name: "IX_inventariomovimiento_IDEmpresa_IDArticulo_Fecha",
                schema: "dbo",
                table: "inventariomovimiento",
                columns: new[] { "IDEmpresa", "IDArticulo", "Fecha" });

            migrationBuilder.CreateIndex(
                name: "IX_listafavoritoecommerce_IDEmpresa_IDUsuarioEcommerce_CanalEc~",
                schema: "dbo",
                table: "listafavoritoecommerce",
                columns: new[] { "IDEmpresa", "IDUsuarioEcommerce", "CanalEcommerce" });

            migrationBuilder.CreateIndex(
                name: "IX_listafavoritoecommerce_IDUsuarioEcommerce",
                schema: "dbo",
                table: "listafavoritoecommerce",
                column: "IDUsuarioEcommerce");

            migrationBuilder.CreateIndex(
                name: "IX_listaprecio_IDEmpresa_CanalEcommerce",
                schema: "dbo",
                table: "listaprecio",
                columns: new[] { "IDEmpresa", "CanalEcommerce" });

            migrationBuilder.CreateIndex(
                name: "IX_listaprecio_IDEmpresa_EsListaPrecioEcommerce",
                schema: "dbo",
                table: "listaprecio",
                columns: new[] { "IDEmpresa", "EsListaPrecioEcommerce" });

            migrationBuilder.CreateIndex(
                name: "IX_listapreciocuenta_IDCuenta",
                schema: "dbo",
                table: "listapreciocuenta",
                column: "IDCuenta");

            migrationBuilder.CreateIndex(
                name: "IX_listapreciocuenta_IDEmpresa_IDCuenta_Activo",
                schema: "dbo",
                table: "listapreciocuenta",
                columns: new[] { "IDEmpresa", "IDCuenta", "Activo" });

            migrationBuilder.CreateIndex(
                name: "IX_listapreciocuenta_IDEmpresa_IDCuenta_IDListaPrecio",
                schema: "dbo",
                table: "listapreciocuenta",
                columns: new[] { "IDEmpresa", "IDCuenta", "IDListaPrecio" });

            migrationBuilder.CreateIndex(
                name: "IX_listapreciocuenta_IDListaPrecio",
                schema: "dbo",
                table: "listapreciocuenta",
                column: "IDListaPrecio");

            migrationBuilder.CreateIndex(
                name: "IX_localidad_IDCiudad",
                schema: "dbo",
                table: "localidad",
                column: "IDCiudad");

            migrationBuilder.CreateIndex(
                name: "IX_logpagotarjeta_IDCarritoCompra",
                schema: "dbo",
                table: "logpagotarjeta",
                column: "IDCarritoCompra");

            migrationBuilder.CreateIndex(
                name: "IX_logpagotarjeta_IDEmpresa_ShopProcessId",
                schema: "dbo",
                table: "logpagotarjeta",
                columns: new[] { "IDEmpresa", "ShopProcessId" });

            migrationBuilder.CreateIndex(
                name: "IX_migracionpedidolog_IDEmpresa_Estado",
                schema: "dbo",
                table: "migracionpedidolog",
                columns: new[] { "IDEmpresa", "Estado" });

            migrationBuilder.CreateIndex(
                name: "IX_migracionpedidolog_IDEmpresa_IDPedidoMovil_FechaIntento",
                schema: "dbo",
                table: "migracionpedidolog",
                columns: new[] { "IDEmpresa", "IDPedidoMovil", "FechaIntento" });

            migrationBuilder.CreateIndex(
                name: "IX_navegacionmenu_IDEmpresa_Code",
                schema: "dbo",
                table: "navegacionmenu",
                columns: new[] { "IDEmpresa", "Code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_navegacionmenuitem_IDNavegacionMenu_DisplayOrder",
                schema: "dbo",
                table: "navegacionmenuitem",
                columns: new[] { "IDNavegacionMenu", "DisplayOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_pedidomovil_IDChofer",
                schema: "dbo",
                table: "pedidomovil",
                column: "IDChofer");

            migrationBuilder.CreateIndex(
                name: "IX_pedidomovil_IDEmpresa_Migrado",
                schema: "dbo",
                table: "pedidomovil",
                columns: new[] { "IDEmpresa", "Migrado" });

            migrationBuilder.CreateIndex(
                name: "IX_pedidomovil_IDEmpresa_NroPedido_OrigenPedido",
                schema: "dbo",
                table: "pedidomovil",
                columns: new[] { "IDEmpresa", "NroPedido", "OrigenPedido" });

            migrationBuilder.CreateIndex(
                name: "IX_pedidomovil_TokenTrackingPublico",
                schema: "dbo",
                table: "pedidomovil",
                column: "TokenTrackingPublico",
                unique: true,
                filter: "\"TokenTrackingPublico\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_productoseguidoecommerce_IDEmpresa_IDUsuarioEcommerce_IDArt~",
                schema: "dbo",
                table: "productoseguidoecommerce",
                columns: new[] { "IDEmpresa", "IDUsuarioEcommerce", "IDArticulo" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_resenaarticuloecommerce_IDArticulo",
                schema: "dbo",
                table: "resenaarticuloecommerce",
                column: "IDArticulo");

            migrationBuilder.CreateIndex(
                name: "IX_resenaarticuloecommerce_IDEmpresa_IDArticulo_Aprobado",
                schema: "dbo",
                table: "resenaarticuloecommerce",
                columns: new[] { "IDEmpresa", "IDArticulo", "Aprobado" });

            migrationBuilder.CreateIndex(
                name: "IX_resenaarticuloecommerce_IDEmpresa_IDUsuarioEcommerce_IDArti~",
                schema: "dbo",
                table: "resenaarticuloecommerce",
                columns: new[] { "IDEmpresa", "IDUsuarioEcommerce", "IDArticulo" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_resenaarticuloecommerce_IDUsuarioEcommerce",
                schema: "dbo",
                table: "resenaarticuloecommerce",
                column: "IDUsuarioEcommerce");

            migrationBuilder.CreateIndex(
                name: "IX_seguimientopedidoecommerce_IDEmpresa_IDPedidoMovil_FechaEst~",
                schema: "dbo",
                table: "seguimientopedidoecommerce",
                columns: new[] { "IDEmpresa", "IDPedidoMovil", "FechaEstado" });

            migrationBuilder.CreateIndex(
                name: "IX_stock_ExistenciaCongelada",
                schema: "dbo",
                table: "stock",
                column: "ExistenciaCongelada");

            migrationBuilder.CreateIndex(
                name: "IX_stock_IDArticulo",
                schema: "dbo",
                table: "stock",
                column: "IDArticulo");

            migrationBuilder.CreateIndex(
                name: "IX_stock_IDDeposito",
                schema: "dbo",
                table: "stock",
                column: "IDDeposito");

            migrationBuilder.CreateIndex(
                name: "IX_stock_IDEmpresa_IDDeposito_IDArticulo_IDContenedor_IDLoteAr~",
                schema: "dbo",
                table: "stock",
                columns: new[] { "IDEmpresa", "IDDeposito", "IDArticulo", "IDContenedor", "IDLoteArticulo" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_stock_TotalExistencia",
                schema: "dbo",
                table: "stock",
                column: "TotalExistencia");

            migrationBuilder.CreateIndex(
                name: "IX_synccursor_IDEmpresa_EntityKey",
                schema: "dbo",
                table: "synccursor",
                columns: new[] { "IDEmpresa", "EntityKey" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_tarifadelivery_IDBarrio",
                schema: "dbo",
                table: "tarifadelivery",
                column: "IDBarrio");

            migrationBuilder.CreateIndex(
                name: "IX_tarifadelivery_IDCiudad",
                schema: "dbo",
                table: "tarifadelivery",
                column: "IDCiudad");

            migrationBuilder.CreateIndex(
                name: "IX_tarifadelivery_IDDivisionPolitica",
                schema: "dbo",
                table: "tarifadelivery",
                column: "IDDivisionPolitica");

            migrationBuilder.CreateIndex(
                name: "IX_tarifadelivery_IDEmpresa_Activo_Estado",
                schema: "dbo",
                table: "tarifadelivery",
                columns: new[] { "IDEmpresa", "Activo", "Estado" });

            migrationBuilder.CreateIndex(
                name: "IX_tarifadelivery_IDEmpresa_IDBarrio_FechaInicioVigencia",
                schema: "dbo",
                table: "tarifadelivery",
                columns: new[] { "IDEmpresa", "IDBarrio", "FechaInicioVigencia" });

            migrationBuilder.CreateIndex(
                name: "IX_tiendaecommerce_IDEmpresa_TokenTienda",
                schema: "dbo",
                table: "tiendaecommerce",
                columns: new[] { "IDEmpresa", "TokenTienda" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_tipodeposito_IDEmpresa_CodTipoDeposito",
                schema: "dbo",
                table: "tipodeposito",
                columns: new[] { "IDEmpresa", "CodTipoDeposito" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_tipodocumento_IDEmpresa_CodTipoDocumento",
                schema: "dbo",
                table: "tipodocumento",
                columns: new[] { "IDEmpresa", "CodTipoDocumento" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_unidadmedidaecommerce_IDEmpresa_Codigo",
                schema: "dbo",
                table: "unidadmedidaecommerce",
                columns: new[] { "IDEmpresa", "Codigo" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_usocuponecommerce_IDCuponEcommerce",
                schema: "dbo",
                table: "usocuponecommerce",
                column: "IDCuponEcommerce");

            migrationBuilder.CreateIndex(
                name: "IX_usocuponecommerce_IDEmpresa_IDCuponEcommerce_IDUsuarioEcomm~",
                schema: "dbo",
                table: "usocuponecommerce",
                columns: new[] { "IDEmpresa", "IDCuponEcommerce", "IDUsuarioEcommerce" });

            migrationBuilder.CreateIndex(
                name: "IX_usuarioecommerce_IDEmpresa_Usuario",
                schema: "dbo",
                table: "usuarioecommerce",
                columns: new[] { "IDEmpresa", "Usuario" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_usuarioecommerce_IDTipoDocumento",
                schema: "dbo",
                table: "usuarioecommerce",
                column: "IDTipoDocumento");

            migrationBuilder.CreateIndex(
                name: "IX_usuarioecommercecuenta_IDEmpresa_IDUsuarioEcommerce_IDCuenta",
                schema: "dbo",
                table: "usuarioecommercecuenta",
                columns: new[] { "IDEmpresa", "IDUsuarioEcommerce", "IDCuenta" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_usuarioecommercecuenta_IDUsuarioEcommerce",
                schema: "dbo",
                table: "usuarioecommercecuenta",
                column: "IDUsuarioEcommerce");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "adminauditorialog",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "adminfuncionalidad",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "adminrol",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "adminrolfuncionalidad",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "adminusuario",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "adminusuariorol",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "articuloembalajecodigobarra",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "articuloimagen",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "catalogoparametro",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "chatsoportemensajeecommerce",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "configuracionpersonalizacion",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "contenidobloqueitem",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "detallecarritocompra",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "detallegrupoarticuloecommerce",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "detallelistafavoritoecommerce",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "detallelistaprecio",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "detallelistapreciocondicionventa",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "detallepedidomovil",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "devolucionecommerce",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "empresaconfiguracionecommerce",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "horarioatencion",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "interaccionarticuloecommerce",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "inventariomovimiento",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "listapreciocuenta",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "logpagotarjeta",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "migracionpedidolog",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "navegacionmenuitem",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "productoseguidoecommerce",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "resenaarticuloecommerce",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "seguimientopedidoecommerce",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "stock",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "synccursor",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "tarifadelivery",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "tiendaecommerce",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "unidadmedidaecommerce",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "usocuponecommerce",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "usuarioecommercecuenta",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "chatconversacionecommerce",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "contenidobloque",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "carritocompra",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "grupoarticuloecommerce",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "listafavoritoecommerce",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "articuloembalaje",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "empresa",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "agencia",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "cuenta",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "listaprecio",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "navegacionmenu",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "deposito",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "barrio",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "cuponecommerce",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "pedidomovil",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "usuarioecommerce",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "articulo",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "tipodeposito",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "distrito",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "chofer",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "tipodocumento",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "localidad",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "ciudad",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "divisionpolitica",
                schema: "dbo");
        }
    }
}
