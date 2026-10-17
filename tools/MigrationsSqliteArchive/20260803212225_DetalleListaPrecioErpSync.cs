using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Datamex.ECommerce.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class DetalleListaPrecioErpSync : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "detallelistaprecio",
                columns: table => new
                {
                    ID = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    IDListaPrecio = table.Column<long>(type: "INTEGER", nullable: false),
                    IDArticuloEmbalaje = table.Column<long>(type: "INTEGER", nullable: false),
                    Precio = table.Column<decimal>(type: "TEXT", nullable: false),
                    Activo = table.Column<bool>(type: "INTEGER", nullable: false),
                    UltModificacion = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UserName = table.Column<string>(type: "TEXT", nullable: true),
                    IDEmpresa = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_detallelistaprecio", x => x.ID);
                    table.ForeignKey(
                        name: "FK_detallelistaprecio_articuloembalaje_IDArticuloEmbalaje",
                        column: x => x.IDArticuloEmbalaje,
                        principalTable: "articuloembalaje",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_detallelistaprecio_listaprecio_IDListaPrecio",
                        column: x => x.IDListaPrecio,
                        principalTable: "listaprecio",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_detallelistaprecio_IDArticuloEmbalaje",
                table: "detallelistaprecio",
                column: "IDArticuloEmbalaje");

            migrationBuilder.CreateIndex(
                name: "IX_detallelistaprecio_IDEmpresa_IDListaPrecio_IDArticuloEmbalaje",
                table: "detallelistaprecio",
                columns: new[] { "IDEmpresa", "IDListaPrecio", "IDArticuloEmbalaje" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_detallelistaprecio_IDListaPrecio",
                table: "detallelistaprecio",
                column: "IDListaPrecio");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "detallelistaprecio");
        }
    }
}
