using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Datamex.ECommerce.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class MarketplaceEngagement : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CodigoCuponAplicado",
                table: "carritocompra",
                type: "TEXT",
                maxLength: 40,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "resenaarticuloecommerce",
                columns: table => new
                {
                    ID = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    IDArticulo = table.Column<long>(type: "INTEGER", nullable: false),
                    IDUsuarioEcommerce = table.Column<long>(type: "INTEGER", nullable: false),
                    IDCuenta = table.Column<long>(type: "INTEGER", nullable: true),
                    IDPedidoMovil = table.Column<long>(type: "INTEGER", nullable: true),
                    IDCarritoCompra = table.Column<long>(type: "INTEGER", nullable: true),
                    Email = table.Column<string>(type: "TEXT", maxLength: 120, nullable: false),
                    CanalEcommerce = table.Column<string>(type: "TEXT", maxLength: 10, nullable: false),
                    Puntuacion = table.Column<int>(type: "INTEGER", nullable: false),
                    Titulo = table.Column<string>(type: "TEXT", maxLength: 120, nullable: true),
                    Comentario = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: true),
                    Aprobado = table.Column<bool>(type: "INTEGER", nullable: false),
                    FechaResena = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Activo = table.Column<bool>(type: "INTEGER", nullable: false),
                    UltModificacion = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UserName = table.Column<string>(type: "TEXT", nullable: true),
                    IDEmpresa = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_resenaarticuloecommerce", x => x.ID);
                    table.ForeignKey(
                        name: "FK_resenaarticuloecommerce_articulo_IDArticulo",
                        column: x => x.IDArticulo,
                        principalTable: "articulo",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_resenaarticuloecommerce_usuarioecommerce_IDUsuarioEcommerce",
                        column: x => x.IDUsuarioEcommerce,
                        principalTable: "usuarioecommerce",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "listafavoritoecommerce",
                columns: table => new
                {
                    ID = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    IDUsuarioEcommerce = table.Column<long>(type: "INTEGER", nullable: false),
                    IDCuenta = table.Column<long>(type: "INTEGER", nullable: true),
                    Email = table.Column<string>(type: "TEXT", maxLength: 120, nullable: false),
                    NombreLista = table.Column<string>(type: "TEXT", maxLength: 80, nullable: false),
                    CanalEcommerce = table.Column<string>(type: "TEXT", maxLength: 10, nullable: false),
                    Orden = table.Column<int>(type: "INTEGER", nullable: false),
                    Color = table.Column<string>(type: "TEXT", maxLength: 20, nullable: true),
                    Activo = table.Column<bool>(type: "INTEGER", nullable: false),
                    UltModificacion = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UserName = table.Column<string>(type: "TEXT", nullable: true),
                    IDEmpresa = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_listafavoritoecommerce", x => x.ID);
                    table.ForeignKey(
                        name: "FK_listafavoritoecommerce_usuarioecommerce_IDUsuarioEcommerce",
                        column: x => x.IDUsuarioEcommerce,
                        principalTable: "usuarioecommerce",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "detallelistafavoritoecommerce",
                columns: table => new
                {
                    ID = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    IDListaFavoritoEcommerce = table.Column<long>(type: "INTEGER", nullable: false),
                    IDArticulo = table.Column<long>(type: "INTEGER", nullable: false),
                    FechaAgregado = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Nota = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    Activo = table.Column<bool>(type: "INTEGER", nullable: false),
                    UltModificacion = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UserName = table.Column<string>(type: "TEXT", nullable: true),
                    IDEmpresa = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_detallelistafavoritoecommerce", x => x.ID);
                    table.ForeignKey(
                        name: "FK_detallelistafavoritoecommerce_articulo_IDArticulo",
                        column: x => x.IDArticulo,
                        principalTable: "articulo",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_detallelistafavoritoecommerce_listafavoritoecommerce_IDListaFavoritoEcommerce",
                        column: x => x.IDListaFavoritoEcommerce,
                        principalTable: "listafavoritoecommerce",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "interaccionarticuloecommerce",
                columns: table => new
                {
                    ID = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    IDArticulo = table.Column<long>(type: "INTEGER", nullable: false),
                    IDUsuarioEcommerce = table.Column<long>(type: "INTEGER", nullable: true),
                    Email = table.Column<string>(type: "TEXT", maxLength: 120, nullable: true),
                    IDSesion = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    TipoInteraccion = table.Column<string>(type: "TEXT", maxLength: 30, nullable: false),
                    CanalEcommerce = table.Column<string>(type: "TEXT", maxLength: 10, nullable: false),
                    Momento = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Activo = table.Column<bool>(type: "INTEGER", nullable: false),
                    UltModificacion = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UserName = table.Column<string>(type: "TEXT", nullable: true),
                    IDEmpresa = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_interaccionarticuloecommerce", x => x.ID);
                });

            migrationBuilder.CreateTable(
                name: "cuponecommerce",
                columns: table => new
                {
                    ID = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Codigo = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false),
                    Descripcion = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    TipoDescuento = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    ValorDescuento = table.Column<decimal>(type: "TEXT", nullable: false),
                    MontoMinimo = table.Column<decimal>(type: "TEXT", nullable: false),
                    MontoMaximoDescuento = table.Column<decimal>(type: "TEXT", nullable: true),
                    CanalEcommerce = table.Column<string>(type: "TEXT", maxLength: 10, nullable: false),
                    FechaInicio = table.Column<DateTime>(type: "TEXT", nullable: false),
                    FechaFin = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UsosMaximos = table.Column<int>(type: "INTEGER", nullable: true),
                    UsosActuales = table.Column<int>(type: "INTEGER", nullable: false),
                    UsosPorUsuario = table.Column<int>(type: "INTEGER", nullable: false),
                    Activo = table.Column<bool>(type: "INTEGER", nullable: false),
                    UltModificacion = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UserName = table.Column<string>(type: "TEXT", nullable: true),
                    IDEmpresa = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_cuponecommerce", x => x.ID);
                });

            migrationBuilder.CreateTable(
                name: "usocuponecommerce",
                columns: table => new
                {
                    ID = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    IDCuponEcommerce = table.Column<long>(type: "INTEGER", nullable: false),
                    IDUsuarioEcommerce = table.Column<long>(type: "INTEGER", nullable: true),
                    IDCarritoCompra = table.Column<long>(type: "INTEGER", nullable: true),
                    Email = table.Column<string>(type: "TEXT", maxLength: 120, nullable: true),
                    MontoDescontado = table.Column<decimal>(type: "TEXT", nullable: false),
                    FechaUso = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Activo = table.Column<bool>(type: "INTEGER", nullable: false),
                    UltModificacion = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UserName = table.Column<string>(type: "TEXT", nullable: true),
                    IDEmpresa = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_usocuponecommerce", x => x.ID);
                    table.ForeignKey(
                        name: "FK_usocuponecommerce_cuponecommerce_IDCuponEcommerce",
                        column: x => x.IDCuponEcommerce,
                        principalTable: "cuponecommerce",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "productoseguidoecommerce",
                columns: table => new
                {
                    ID = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    IDUsuarioEcommerce = table.Column<long>(type: "INTEGER", nullable: false),
                    IDArticulo = table.Column<long>(type: "INTEGER", nullable: false),
                    Email = table.Column<string>(type: "TEXT", maxLength: 120, nullable: false),
                    CanalEcommerce = table.Column<string>(type: "TEXT", maxLength: 10, nullable: false),
                    FechaSeguimiento = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Activo = table.Column<bool>(type: "INTEGER", nullable: false),
                    UltModificacion = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UserName = table.Column<string>(type: "TEXT", nullable: true),
                    IDEmpresa = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_productoseguidoecommerce", x => x.ID);
                });

            migrationBuilder.CreateTable(
                name: "seguimientopedidoecommerce",
                columns: table => new
                {
                    ID = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    IDPedidoMovil = table.Column<long>(type: "INTEGER", nullable: false),
                    IDCarritoCompra = table.Column<long>(type: "INTEGER", nullable: true),
                    Estado = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false),
                    Mensaje = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    FechaEstado = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CodigoTracking = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    Activo = table.Column<bool>(type: "INTEGER", nullable: false),
                    UltModificacion = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UserName = table.Column<string>(type: "TEXT", nullable: true),
                    IDEmpresa = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_seguimientopedidoecommerce", x => x.ID);
                });

            migrationBuilder.CreateTable(
                name: "devolucionecommerce",
                columns: table => new
                {
                    ID = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    IDUsuarioEcommerce = table.Column<long>(type: "INTEGER", nullable: false),
                    IDCuenta = table.Column<long>(type: "INTEGER", nullable: true),
                    Email = table.Column<string>(type: "TEXT", maxLength: 120, nullable: false),
                    IDPedidoMovil = table.Column<long>(type: "INTEGER", nullable: false),
                    IDArticulo = table.Column<long>(type: "INTEGER", nullable: true),
                    Motivo = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Detalle = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true),
                    Estado = table.Column<string>(type: "TEXT", maxLength: 30, nullable: false),
                    FechaSolicitud = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Activo = table.Column<bool>(type: "INTEGER", nullable: false),
                    UltModificacion = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UserName = table.Column<string>(type: "TEXT", nullable: true),
                    IDEmpresa = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_devolucionecommerce", x => x.ID);
                });

            migrationBuilder.CreateTable(
                name: "chatsoportemensajeecommerce",
                columns: table => new
                {
                    ID = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    IDUsuarioEcommerce = table.Column<long>(type: "INTEGER", nullable: true),
                    IDPedidoMovil = table.Column<long>(type: "INTEGER", nullable: true),
                    Email = table.Column<string>(type: "TEXT", maxLength: 120, nullable: false),
                    Origen = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    Mensaje = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: false),
                    FechaMensaje = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Leido = table.Column<bool>(type: "INTEGER", nullable: false),
                    Activo = table.Column<bool>(type: "INTEGER", nullable: false),
                    UltModificacion = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UserName = table.Column<string>(type: "TEXT", nullable: true),
                    IDEmpresa = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_chatsoportemensajeecommerce", x => x.ID);
                });

            migrationBuilder.CreateIndex(
                name: "IX_resenaarticuloecommerce_IDArticulo",
                table: "resenaarticuloecommerce",
                column: "IDArticulo");

            migrationBuilder.CreateIndex(
                name: "IX_resenaarticuloecommerce_IDEmpresa_IDArticulo_Aprobado",
                table: "resenaarticuloecommerce",
                columns: new[] { "IDEmpresa", "IDArticulo", "Aprobado" });

            migrationBuilder.CreateIndex(
                name: "IX_resenaarticuloecommerce_IDEmpresa_IDUsuarioEcommerce_IDArticulo",
                table: "resenaarticuloecommerce",
                columns: new[] { "IDEmpresa", "IDUsuarioEcommerce", "IDArticulo" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_resenaarticuloecommerce_IDUsuarioEcommerce",
                table: "resenaarticuloecommerce",
                column: "IDUsuarioEcommerce");

            migrationBuilder.CreateIndex(
                name: "IX_listafavoritoecommerce_IDEmpresa_IDUsuarioEcommerce_CanalEcommerce",
                table: "listafavoritoecommerce",
                columns: new[] { "IDEmpresa", "IDUsuarioEcommerce", "CanalEcommerce" });

            migrationBuilder.CreateIndex(
                name: "IX_listafavoritoecommerce_IDUsuarioEcommerce",
                table: "listafavoritoecommerce",
                column: "IDUsuarioEcommerce");

            migrationBuilder.CreateIndex(
                name: "IX_detallelistafavoritoecommerce_IDArticulo",
                table: "detallelistafavoritoecommerce",
                column: "IDArticulo");

            migrationBuilder.CreateIndex(
                name: "IX_detallelistafavoritoecommerce_IDEmpresa_IDListaFavoritoEcommerce_IDArticulo",
                table: "detallelistafavoritoecommerce",
                columns: new[] { "IDEmpresa", "IDListaFavoritoEcommerce", "IDArticulo" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_detallelistafavoritoecommerce_IDListaFavoritoEcommerce",
                table: "detallelistafavoritoecommerce",
                column: "IDListaFavoritoEcommerce");

            migrationBuilder.CreateIndex(
                name: "IX_interaccionarticuloecommerce_IDEmpresa_IDArticulo_Momento",
                table: "interaccionarticuloecommerce",
                columns: new[] { "IDEmpresa", "IDArticulo", "Momento" });

            migrationBuilder.CreateIndex(
                name: "IX_cuponecommerce_IDEmpresa_Codigo",
                table: "cuponecommerce",
                columns: new[] { "IDEmpresa", "Codigo" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_usocuponecommerce_IDCuponEcommerce",
                table: "usocuponecommerce",
                column: "IDCuponEcommerce");

            migrationBuilder.CreateIndex(
                name: "IX_usocuponecommerce_IDEmpresa_IDCuponEcommerce_IDUsuarioEcommerce",
                table: "usocuponecommerce",
                columns: new[] { "IDEmpresa", "IDCuponEcommerce", "IDUsuarioEcommerce" });

            migrationBuilder.CreateIndex(
                name: "IX_productoseguidoecommerce_IDEmpresa_IDUsuarioEcommerce_IDArticulo",
                table: "productoseguidoecommerce",
                columns: new[] { "IDEmpresa", "IDUsuarioEcommerce", "IDArticulo" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_seguimientopedidoecommerce_IDEmpresa_IDPedidoMovil_FechaEstado",
                table: "seguimientopedidoecommerce",
                columns: new[] { "IDEmpresa", "IDPedidoMovil", "FechaEstado" });

            migrationBuilder.CreateIndex(
                name: "IX_devolucionecommerce_IDEmpresa_IDUsuarioEcommerce",
                table: "devolucionecommerce",
                columns: new[] { "IDEmpresa", "IDUsuarioEcommerce" });

            migrationBuilder.CreateIndex(
                name: "IX_chatsoportemensajeecommerce_IDEmpresa_IDUsuarioEcommerce_FechaMensaje",
                table: "chatsoportemensajeecommerce",
                columns: new[] { "IDEmpresa", "IDUsuarioEcommerce", "FechaMensaje" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "detallelistafavoritoecommerce");
            migrationBuilder.DropTable(name: "resenaarticuloecommerce");
            migrationBuilder.DropTable(name: "interaccionarticuloecommerce");
            migrationBuilder.DropTable(name: "usocuponecommerce");
            migrationBuilder.DropTable(name: "productoseguidoecommerce");
            migrationBuilder.DropTable(name: "seguimientopedidoecommerce");
            migrationBuilder.DropTable(name: "devolucionecommerce");
            migrationBuilder.DropTable(name: "chatsoportemensajeecommerce");
            migrationBuilder.DropTable(name: "listafavoritoecommerce");
            migrationBuilder.DropTable(name: "cuponecommerce");
            migrationBuilder.DropColumn(name: "CodigoCuponAplicado", table: "carritocompra");
        }
    }
}
