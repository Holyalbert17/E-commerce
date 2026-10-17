using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Datamex.ECommerce.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ListaPrecioCuenta : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "listapreciocuenta",
                columns: table => new
                {
                    ID = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    IDListaPrecio = table.Column<long>(type: "INTEGER", nullable: false),
                    IDCuenta = table.Column<long>(type: "INTEGER", nullable: false),
                    Activo = table.Column<bool>(type: "INTEGER", nullable: false),
                    UltModificacion = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UserName = table.Column<string>(type: "TEXT", nullable: true),
                    IDEmpresa = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_listapreciocuenta", x => x.ID);
                    table.ForeignKey(
                        name: "FK_listapreciocuenta_cuenta_IDCuenta",
                        column: x => x.IDCuenta,
                        principalTable: "cuenta",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_listapreciocuenta_listaprecio_IDListaPrecio",
                        column: x => x.IDListaPrecio,
                        principalTable: "listaprecio",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_listapreciocuenta_IDCuenta",
                table: "listapreciocuenta",
                column: "IDCuenta");

            migrationBuilder.CreateIndex(
                name: "IX_listapreciocuenta_IDEmpresa_IDCuenta_Activo",
                table: "listapreciocuenta",
                columns: new[] { "IDEmpresa", "IDCuenta", "Activo" });

            migrationBuilder.CreateIndex(
                name: "IX_listapreciocuenta_IDEmpresa_IDCuenta_IDListaPrecio",
                table: "listapreciocuenta",
                columns: new[] { "IDEmpresa", "IDCuenta", "IDListaPrecio" });

            migrationBuilder.CreateIndex(
                name: "IX_listapreciocuenta_IDListaPrecio",
                table: "listapreciocuenta",
                column: "IDListaPrecio");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "listapreciocuenta");
        }
    }
}
