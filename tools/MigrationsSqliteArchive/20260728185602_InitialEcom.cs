using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Datamex.ECommerce.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialEcom : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "carritocompra",
                columns: table => new
                {
                    ID = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    NroPedidoCarritoCompra = table.Column<long>(type: "INTEGER", nullable: false),
                    IDCuenta = table.Column<long>(type: "INTEGER", nullable: true),
                    IDListaPrecio = table.Column<long>(type: "INTEGER", nullable: true),
                    IDCondicionVenta = table.Column<long>(type: "INTEGER", nullable: false),
                    IDVendedor = table.Column<long>(type: "INTEGER", nullable: false),
                    IDDeposito = table.Column<long>(type: "INTEGER", nullable: false),
                    IDCotizacionComercial = table.Column<long>(type: "INTEGER", nullable: false),
                    IDAgenciaFacturacion = table.Column<long>(type: "INTEGER", nullable: false),
                    FechaPedido = table.Column<DateTime>(type: "TEXT", nullable: false),
                    MontoCambio = table.Column<decimal>(type: "TEXT", nullable: false),
                    Plazo = table.Column<int>(type: "INTEGER", nullable: false),
                    PorcentajeDescuento = table.Column<decimal>(type: "TEXT", nullable: false),
                    PorcentajeRecargo = table.Column<decimal>(type: "TEXT", nullable: false),
                    TipoEntrega = table.Column<int>(type: "INTEGER", nullable: false),
                    TipoPago = table.Column<int>(type: "INTEGER", nullable: false),
                    FechaPrevistaEntrega = table.Column<DateTime>(type: "TEXT", nullable: false),
                    LugarEntrega = table.Column<string>(type: "TEXT", nullable: true),
                    Observacion = table.Column<string>(type: "TEXT", nullable: true),
                    MontoTotal = table.Column<decimal>(type: "TEXT", nullable: false),
                    MontoIVA = table.Column<decimal>(type: "TEXT", nullable: false),
                    MontoExento = table.Column<decimal>(type: "TEXT", nullable: false),
                    MontoGravado = table.Column<decimal>(type: "TEXT", nullable: false),
                    MontoDescuento = table.Column<decimal>(type: "TEXT", nullable: false),
                    MontoRecargo = table.Column<decimal>(type: "TEXT", nullable: false),
                    RazonSocial = table.Column<string>(type: "TEXT", maxLength: 255, nullable: false),
                    NumeroDocumento = table.Column<string>(type: "TEXT", maxLength: 25, nullable: false),
                    IDPedidoMovil = table.Column<long>(type: "INTEGER", nullable: true),
                    LatitudEntrega = table.Column<decimal>(type: "TEXT", nullable: true),
                    LongitudEntrega = table.Column<decimal>(type: "TEXT", nullable: true),
                    Estado = table.Column<int>(type: "INTEGER", nullable: false),
                    Migrado = table.Column<bool>(type: "INTEGER", nullable: false),
                    IDSesion = table.Column<string>(type: "TEXT", nullable: true),
                    IDProcesoNegocio = table.Column<long>(type: "INTEGER", nullable: true),
                    Activo = table.Column<bool>(type: "INTEGER", nullable: false),
                    UltModificacion = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UserName = table.Column<string>(type: "TEXT", nullable: true),
                    IDEmpresa = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_carritocompra", x => x.ID);
                });

            migrationBuilder.CreateTable(
                name: "empresa",
                columns: table => new
                {
                    ID = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    RazonSocial = table.Column<string>(type: "TEXT", maxLength: 255, nullable: false),
                    NombreFantasia = table.Column<string>(type: "TEXT", nullable: true),
                    NumeroDocumento = table.Column<string>(type: "TEXT", nullable: true),
                    Activo = table.Column<bool>(type: "INTEGER", nullable: false),
                    UltModificacion = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UserName = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_empresa", x => x.ID);
                });

            migrationBuilder.CreateTable(
                name: "grupoarticuloecommerce",
                columns: table => new
                {
                    ID = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Descripcion = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Orden = table.Column<int>(type: "INTEGER", nullable: false),
                    ImagenUrl = table.Column<string>(type: "TEXT", nullable: true),
                    Activo = table.Column<bool>(type: "INTEGER", nullable: false),
                    UltModificacion = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UserName = table.Column<string>(type: "TEXT", nullable: true),
                    IDEmpresa = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_grupoarticuloecommerce", x => x.ID);
                });

            migrationBuilder.CreateTable(
                name: "articulo",
                columns: table => new
                {
                    ID = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    NroArticulo = table.Column<int>(type: "INTEGER", nullable: false),
                    SKU = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    DescripcionArticulo = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    DescripcionArticuloAbreviada = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    DescripcionArticuloExtensa = table.Column<string>(type: "TEXT", nullable: false),
                    CodReferenciaProveedor = table.Column<string>(type: "TEXT", nullable: true),
                    IDMarca = table.Column<long>(type: "INTEGER", nullable: false),
                    ActivoEcommerce = table.Column<bool>(type: "INTEGER", nullable: false),
                    EstadoEcommerce = table.Column<string>(type: "TEXT", maxLength: 30, nullable: false),
                    EmpresaID = table.Column<long>(type: "INTEGER", nullable: true),
                    Activo = table.Column<bool>(type: "INTEGER", nullable: false),
                    UltModificacion = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UserName = table.Column<string>(type: "TEXT", nullable: true),
                    IDEmpresa = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_articulo", x => x.ID);
                    table.ForeignKey(
                        name: "FK_articulo_empresa_EmpresaID",
                        column: x => x.EmpresaID,
                        principalTable: "empresa",
                        principalColumn: "ID");
                });

            migrationBuilder.CreateTable(
                name: "empresaconfiguracionecommerce",
                columns: table => new
                {
                    ID = table.Column<long>(type: "INTEGER", nullable: false),
                    RealizaDelivery = table.Column<bool>(type: "INTEGER", nullable: false),
                    RealizaPickup = table.Column<bool>(type: "INTEGER", nullable: false),
                    CobraEnLaEntrega = table.Column<bool>(type: "INTEGER", nullable: false),
                    CobraAlHacerPedido = table.Column<bool>(type: "INTEGER", nullable: false),
                    MontoPedidoMinimo = table.Column<decimal>(type: "TEXT", nullable: false),
                    ProcesaPedidosPorPlanilla = table.Column<bool>(type: "INTEGER", nullable: false),
                    ProcesaPedidosERP = table.Column<bool>(type: "INTEGER", nullable: false),
                    NroUltPedidoCarritoCompra = table.Column<long>(type: "INTEGER", nullable: false),
                    IDArticuloDelivery = table.Column<long>(type: "INTEGER", nullable: true),
                    ControlaStockEnCarrito = table.Column<bool>(type: "INTEGER", nullable: false),
                    ClavePublicaProcesadoraTarjeta = table.Column<string>(type: "TEXT", nullable: true),
                    ClavePrivadaProcesadoraTarjeta = table.Column<string>(type: "TEXT", nullable: true),
                    DireccionURLCheckout = table.Column<string>(type: "TEXT", nullable: true),
                    CodigoComercio = table.Column<long>(type: "INTEGER", nullable: true),
                    CodigoSucursal = table.Column<long>(type: "INTEGER", nullable: true),
                    IDDeposito = table.Column<long>(type: "INTEGER", nullable: false),
                    DireccionURLFacebook = table.Column<string>(type: "TEXT", nullable: true),
                    DireccionURLTwitter = table.Column<string>(type: "TEXT", nullable: true),
                    DireccionURLInstagram = table.Column<string>(type: "TEXT", nullable: true),
                    DireccionURLYoutube = table.Column<string>(type: "TEXT", nullable: true),
                    DireccionURLWhatsapp = table.Column<string>(type: "TEXT", nullable: true),
                    SlugTienda = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    DominioCustom = table.Column<string>(type: "TEXT", nullable: true),
                    Activo = table.Column<bool>(type: "INTEGER", nullable: false),
                    UltModificacion = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UserName = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_empresaconfiguracionecommerce", x => x.ID);
                    table.ForeignKey(
                        name: "FK_empresaconfiguracionecommerce_empresa_ID",
                        column: x => x.ID,
                        principalTable: "empresa",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "listaprecio",
                columns: table => new
                {
                    ID = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    NroListaPrecio = table.Column<int>(type: "INTEGER", nullable: false),
                    Descripcion = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    EsListaPrecioEcommerce = table.Column<bool>(type: "INTEGER", nullable: false),
                    FechaInicioVigencia = table.Column<DateTime>(type: "TEXT", nullable: true),
                    FechaFinVigencia = table.Column<DateTime>(type: "TEXT", nullable: true),
                    EmpresaID = table.Column<long>(type: "INTEGER", nullable: true),
                    Activo = table.Column<bool>(type: "INTEGER", nullable: false),
                    UltModificacion = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UserName = table.Column<string>(type: "TEXT", nullable: true),
                    IDEmpresa = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_listaprecio", x => x.ID);
                    table.ForeignKey(
                        name: "FK_listaprecio_empresa_EmpresaID",
                        column: x => x.EmpresaID,
                        principalTable: "empresa",
                        principalColumn: "ID");
                });

            migrationBuilder.CreateTable(
                name: "tiendaecommerce",
                columns: table => new
                {
                    ID = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    IDClasificacionArticulo = table.Column<long>(type: "INTEGER", nullable: false),
                    TokenTienda = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    IDDepositoTienda = table.Column<long>(type: "INTEGER", nullable: false),
                    Nombre = table.Column<string>(type: "TEXT", nullable: true),
                    HabilitaB2C = table.Column<bool>(type: "INTEGER", nullable: false),
                    HabilitaB2B = table.Column<bool>(type: "INTEGER", nullable: false),
                    EmpresaID = table.Column<long>(type: "INTEGER", nullable: true),
                    Activo = table.Column<bool>(type: "INTEGER", nullable: false),
                    UltModificacion = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UserName = table.Column<string>(type: "TEXT", nullable: true),
                    IDEmpresa = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tiendaecommerce", x => x.ID);
                    table.ForeignKey(
                        name: "FK_tiendaecommerce_empresa_EmpresaID",
                        column: x => x.EmpresaID,
                        principalTable: "empresa",
                        principalColumn: "ID");
                });

            migrationBuilder.CreateTable(
                name: "usuarioecommerce",
                columns: table => new
                {
                    ID = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    CodigoUsuario = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    Usuario = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Password = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Nombre = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Apellido = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Email = table.Column<string>(type: "TEXT", maxLength: 60, nullable: true),
                    NumeroTelefono = table.Column<string>(type: "TEXT", nullable: true),
                    NumeroMovil = table.Column<string>(type: "TEXT", nullable: true),
                    NumeroDocumento = table.Column<string>(type: "TEXT", nullable: true),
                    FechaRegistro = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Activado = table.Column<bool>(type: "INTEGER", nullable: false),
                    Bloqueado = table.Column<bool>(type: "INTEGER", nullable: false),
                    CodigoActivacion = table.Column<string>(type: "TEXT", nullable: true),
                    CheckSum = table.Column<string>(type: "TEXT", nullable: true),
                    CanalPreferido = table.Column<string>(type: "TEXT", maxLength: 10, nullable: false),
                    EmpresaID = table.Column<long>(type: "INTEGER", nullable: true),
                    Activo = table.Column<bool>(type: "INTEGER", nullable: false),
                    UltModificacion = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UserName = table.Column<string>(type: "TEXT", nullable: true),
                    IDEmpresa = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_usuarioecommerce", x => x.ID);
                    table.ForeignKey(
                        name: "FK_usuarioecommerce_empresa_EmpresaID",
                        column: x => x.EmpresaID,
                        principalTable: "empresa",
                        principalColumn: "ID");
                });

            migrationBuilder.CreateTable(
                name: "detallecarritocompra",
                columns: table => new
                {
                    ID = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    IDCarritoCompra = table.Column<long>(type: "INTEGER", nullable: false),
                    IDArticulo = table.Column<long>(type: "INTEGER", nullable: false),
                    CantidadxEmbalaje = table.Column<decimal>(type: "TEXT", nullable: false),
                    IDRegimenImpositivo = table.Column<long>(type: "INTEGER", nullable: false),
                    PorcentajeDescuento = table.Column<decimal>(type: "TEXT", nullable: false),
                    CantidadBonificada = table.Column<decimal>(type: "TEXT", nullable: false),
                    CantidadEmbaladaBonificada = table.Column<int>(type: "INTEGER", nullable: false),
                    CantidadFraccionadaBonificada = table.Column<int>(type: "INTEGER", nullable: false),
                    Cantidad = table.Column<decimal>(type: "TEXT", nullable: false),
                    CantidadEmbalada = table.Column<int>(type: "INTEGER", nullable: false),
                    CantidadFraccionada = table.Column<int>(type: "INTEGER", nullable: false),
                    PorcentajeImpuesto = table.Column<decimal>(type: "TEXT", nullable: false),
                    PrecioBrutoConImpuesto = table.Column<decimal>(type: "TEXT", nullable: false),
                    PrecioBrutoSinImpuesto = table.Column<decimal>(type: "TEXT", nullable: false),
                    PrecioNetoConImpuesto = table.Column<decimal>(type: "TEXT", nullable: false),
                    PrecioNetoSinImpuesto = table.Column<decimal>(type: "TEXT", nullable: false),
                    MontoTotal = table.Column<decimal>(type: "TEXT", nullable: false),
                    MontoIVA = table.Column<decimal>(type: "TEXT", nullable: false),
                    Activo = table.Column<bool>(type: "INTEGER", nullable: false),
                    UltModificacion = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UserName = table.Column<string>(type: "TEXT", nullable: true),
                    IDEmpresa = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_detallecarritocompra", x => x.ID);
                    table.ForeignKey(
                        name: "FK_detallecarritocompra_articulo_IDArticulo",
                        column: x => x.IDArticulo,
                        principalTable: "articulo",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_detallecarritocompra_carritocompra_IDCarritoCompra",
                        column: x => x.IDCarritoCompra,
                        principalTable: "carritocompra",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "detallegrupoarticuloecommerce",
                columns: table => new
                {
                    ID = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    IDGrupoArticuloEcommerce = table.Column<long>(type: "INTEGER", nullable: false),
                    IDArticulo = table.Column<long>(type: "INTEGER", nullable: false),
                    Orden = table.Column<int>(type: "INTEGER", nullable: false),
                    Activo = table.Column<bool>(type: "INTEGER", nullable: false),
                    UltModificacion = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UserName = table.Column<string>(type: "TEXT", nullable: true),
                    IDEmpresa = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_detallegrupoarticuloecommerce", x => x.ID);
                    table.ForeignKey(
                        name: "FK_detallegrupoarticuloecommerce_articulo_IDArticulo",
                        column: x => x.IDArticulo,
                        principalTable: "articulo",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_detallegrupoarticuloecommerce_grupoarticuloecommerce_IDGrupoArticuloEcommerce",
                        column: x => x.IDGrupoArticuloEcommerce,
                        principalTable: "grupoarticuloecommerce",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "usuarioecommercecuenta",
                columns: table => new
                {
                    ID = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    IDUsuarioEcommerce = table.Column<long>(type: "INTEGER", nullable: false),
                    IDCuenta = table.Column<long>(type: "INTEGER", nullable: false),
                    EsPrincipal = table.Column<bool>(type: "INTEGER", nullable: false),
                    Activo = table.Column<bool>(type: "INTEGER", nullable: false),
                    UltModificacion = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UserName = table.Column<string>(type: "TEXT", nullable: true),
                    IDEmpresa = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_usuarioecommercecuenta", x => x.ID);
                    table.ForeignKey(
                        name: "FK_usuarioecommercecuenta_usuarioecommerce_IDUsuarioEcommerce",
                        column: x => x.IDUsuarioEcommerce,
                        principalTable: "usuarioecommerce",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_articulo_EmpresaID",
                table: "articulo",
                column: "EmpresaID");

            migrationBuilder.CreateIndex(
                name: "IX_articulo_IDEmpresa_ActivoEcommerce",
                table: "articulo",
                columns: new[] { "IDEmpresa", "ActivoEcommerce" });

            migrationBuilder.CreateIndex(
                name: "IX_articulo_IDEmpresa_SKU",
                table: "articulo",
                columns: new[] { "IDEmpresa", "SKU" });

            migrationBuilder.CreateIndex(
                name: "IX_carritocompra_IDEmpresa_IDSesion",
                table: "carritocompra",
                columns: new[] { "IDEmpresa", "IDSesion" });

            migrationBuilder.CreateIndex(
                name: "IX_carritocompra_IDEmpresa_NroPedidoCarritoCompra",
                table: "carritocompra",
                columns: new[] { "IDEmpresa", "NroPedidoCarritoCompra" });

            migrationBuilder.CreateIndex(
                name: "IX_detallecarritocompra_IDArticulo",
                table: "detallecarritocompra",
                column: "IDArticulo");

            migrationBuilder.CreateIndex(
                name: "IX_detallecarritocompra_IDCarritoCompra",
                table: "detallecarritocompra",
                column: "IDCarritoCompra");

            migrationBuilder.CreateIndex(
                name: "IX_detallegrupoarticuloecommerce_IDArticulo",
                table: "detallegrupoarticuloecommerce",
                column: "IDArticulo");

            migrationBuilder.CreateIndex(
                name: "IX_detallegrupoarticuloecommerce_IDGrupoArticuloEcommerce",
                table: "detallegrupoarticuloecommerce",
                column: "IDGrupoArticuloEcommerce");

            migrationBuilder.CreateIndex(
                name: "IX_empresaconfiguracionecommerce_SlugTienda",
                table: "empresaconfiguracionecommerce",
                column: "SlugTienda",
                unique: true,
                filter: "\"SlugTienda\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_listaprecio_EmpresaID",
                table: "listaprecio",
                column: "EmpresaID");

            migrationBuilder.CreateIndex(
                name: "IX_listaprecio_IDEmpresa_EsListaPrecioEcommerce",
                table: "listaprecio",
                columns: new[] { "IDEmpresa", "EsListaPrecioEcommerce" });

            migrationBuilder.CreateIndex(
                name: "IX_tiendaecommerce_EmpresaID",
                table: "tiendaecommerce",
                column: "EmpresaID");

            migrationBuilder.CreateIndex(
                name: "IX_tiendaecommerce_IDEmpresa_TokenTienda",
                table: "tiendaecommerce",
                columns: new[] { "IDEmpresa", "TokenTienda" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_usuarioecommerce_EmpresaID",
                table: "usuarioecommerce",
                column: "EmpresaID");

            migrationBuilder.CreateIndex(
                name: "IX_usuarioecommerce_IDEmpresa_Usuario",
                table: "usuarioecommerce",
                columns: new[] { "IDEmpresa", "Usuario" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_usuarioecommercecuenta_IDEmpresa_IDUsuarioEcommerce_IDCuenta",
                table: "usuarioecommercecuenta",
                columns: new[] { "IDEmpresa", "IDUsuarioEcommerce", "IDCuenta" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_usuarioecommercecuenta_IDUsuarioEcommerce",
                table: "usuarioecommercecuenta",
                column: "IDUsuarioEcommerce");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "detallecarritocompra");

            migrationBuilder.DropTable(
                name: "detallegrupoarticuloecommerce");

            migrationBuilder.DropTable(
                name: "empresaconfiguracionecommerce");

            migrationBuilder.DropTable(
                name: "listaprecio");

            migrationBuilder.DropTable(
                name: "tiendaecommerce");

            migrationBuilder.DropTable(
                name: "usuarioecommercecuenta");

            migrationBuilder.DropTable(
                name: "carritocompra");

            migrationBuilder.DropTable(
                name: "articulo");

            migrationBuilder.DropTable(
                name: "grupoarticuloecommerce");

            migrationBuilder.DropTable(
                name: "usuarioecommerce");

            migrationBuilder.DropTable(
                name: "empresa");
        }
    }
}
