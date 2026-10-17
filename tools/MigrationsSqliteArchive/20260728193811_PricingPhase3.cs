using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Datamex.ECommerce.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class PricingPhase3 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_articulo_empresa_EmpresaID",
                table: "articulo");

            migrationBuilder.DropForeignKey(
                name: "FK_listaprecio_empresa_EmpresaID",
                table: "listaprecio");

            migrationBuilder.DropForeignKey(
                name: "FK_tiendaecommerce_empresa_EmpresaID",
                table: "tiendaecommerce");

            migrationBuilder.DropForeignKey(
                name: "FK_usuarioecommerce_empresa_EmpresaID",
                table: "usuarioecommerce");

            migrationBuilder.DropIndex(
                name: "IX_usuarioecommerce_EmpresaID",
                table: "usuarioecommerce");

            migrationBuilder.DropIndex(
                name: "IX_tiendaecommerce_EmpresaID",
                table: "tiendaecommerce");

            migrationBuilder.DropIndex(
                name: "IX_listaprecio_EmpresaID",
                table: "listaprecio");

            migrationBuilder.DropIndex(
                name: "IX_articulo_EmpresaID",
                table: "articulo");

            migrationBuilder.DropColumn(
                name: "EmpresaID",
                table: "usuarioecommerce");

            migrationBuilder.DropColumn(
                name: "EmpresaID",
                table: "tiendaecommerce");

            migrationBuilder.DropColumn(
                name: "EmpresaID",
                table: "listaprecio");

            migrationBuilder.DropColumn(
                name: "EmpresaID",
                table: "articulo");

            migrationBuilder.AddColumn<string>(
                name: "CanalEcommerce",
                table: "listaprecio",
                type: "TEXT",
                maxLength: 10,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "FechaRegistro",
                table: "listaprecio",
                type: "TEXT",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<bool>(
                name: "PrecioConImpuestos",
                table: "listaprecio",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "detallelistapreciocondicionventa",
                columns: table => new
                {
                    ID = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    IDListaPrecio = table.Column<long>(type: "INTEGER", nullable: false),
                    IDArticuloEmbalaje = table.Column<long>(type: "INTEGER", nullable: false),
                    PrecioContado = table.Column<decimal>(type: "TEXT", nullable: false),
                    PrecioCheque = table.Column<decimal>(type: "TEXT", nullable: false),
                    PrecioCredito = table.Column<decimal>(type: "TEXT", nullable: false),
                    CantidadMinimaMayorista = table.Column<decimal>(type: "TEXT", nullable: false),
                    PorDescuentoMinorista = table.Column<decimal>(type: "TEXT", nullable: false),
                    PorDescuentoMayorista = table.Column<decimal>(type: "TEXT", nullable: false),
                    EsAutomatico = table.Column<bool>(type: "INTEGER", nullable: false),
                    Activo = table.Column<bool>(type: "INTEGER", nullable: false),
                    UltModificacion = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UserName = table.Column<string>(type: "TEXT", nullable: true),
                    IDEmpresa = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_detallelistapreciocondicionventa", x => x.ID);
                    table.ForeignKey(
                        name: "FK_detallelistapreciocondicionventa_articuloembalaje_IDArticuloEmbalaje",
                        column: x => x.IDArticuloEmbalaje,
                        principalTable: "articuloembalaje",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_detallelistapreciocondicionventa_listaprecio_IDListaPrecio",
                        column: x => x.IDListaPrecio,
                        principalTable: "listaprecio",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_listaprecio_IDEmpresa_CanalEcommerce",
                table: "listaprecio",
                columns: new[] { "IDEmpresa", "CanalEcommerce" });

            migrationBuilder.CreateIndex(
                name: "IX_detallelistapreciocondicionventa_IDArticuloEmbalaje",
                table: "detallelistapreciocondicionventa",
                column: "IDArticuloEmbalaje");

            migrationBuilder.CreateIndex(
                name: "IX_detallelistapreciocondicionventa_IDEmpresa_IDListaPrecio_IDArticuloEmbalaje",
                table: "detallelistapreciocondicionventa",
                columns: new[] { "IDEmpresa", "IDListaPrecio", "IDArticuloEmbalaje" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_detallelistapreciocondicionventa_IDListaPrecio",
                table: "detallelistapreciocondicionventa",
                column: "IDListaPrecio");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "detallelistapreciocondicionventa");

            migrationBuilder.DropIndex(
                name: "IX_listaprecio_IDEmpresa_CanalEcommerce",
                table: "listaprecio");

            migrationBuilder.DropColumn(
                name: "CanalEcommerce",
                table: "listaprecio");

            migrationBuilder.DropColumn(
                name: "FechaRegistro",
                table: "listaprecio");

            migrationBuilder.DropColumn(
                name: "PrecioConImpuestos",
                table: "listaprecio");

            migrationBuilder.AddColumn<long>(
                name: "EmpresaID",
                table: "usuarioecommerce",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "EmpresaID",
                table: "tiendaecommerce",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "EmpresaID",
                table: "listaprecio",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "EmpresaID",
                table: "articulo",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_usuarioecommerce_EmpresaID",
                table: "usuarioecommerce",
                column: "EmpresaID");

            migrationBuilder.CreateIndex(
                name: "IX_tiendaecommerce_EmpresaID",
                table: "tiendaecommerce",
                column: "EmpresaID");

            migrationBuilder.CreateIndex(
                name: "IX_listaprecio_EmpresaID",
                table: "listaprecio",
                column: "EmpresaID");

            migrationBuilder.CreateIndex(
                name: "IX_articulo_EmpresaID",
                table: "articulo",
                column: "EmpresaID");

            migrationBuilder.AddForeignKey(
                name: "FK_articulo_empresa_EmpresaID",
                table: "articulo",
                column: "EmpresaID",
                principalTable: "empresa",
                principalColumn: "ID");

            migrationBuilder.AddForeignKey(
                name: "FK_listaprecio_empresa_EmpresaID",
                table: "listaprecio",
                column: "EmpresaID",
                principalTable: "empresa",
                principalColumn: "ID");

            migrationBuilder.AddForeignKey(
                name: "FK_tiendaecommerce_empresa_EmpresaID",
                table: "tiendaecommerce",
                column: "EmpresaID",
                principalTable: "empresa",
                principalColumn: "ID");

            migrationBuilder.AddForeignKey(
                name: "FK_usuarioecommerce_empresa_EmpresaID",
                table: "usuarioecommerce",
                column: "EmpresaID",
                principalTable: "empresa",
                principalColumn: "ID");
        }
    }
}
