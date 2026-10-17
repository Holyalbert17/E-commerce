using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Datamex.ECommerce.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InventoryCartPhase45 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "deposito",
                columns: table => new
                {
                    ID = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    CodDeposito = table.Column<int>(type: "INTEGER", nullable: false),
                    DescripcionDeposito = table.Column<string>(type: "TEXT", maxLength: 256, nullable: false),
                    DepositoEntrega = table.Column<bool>(type: "INTEGER", nullable: false),
                    EsDepositoEcommerce = table.Column<bool>(type: "INTEGER", nullable: false),
                    Activo = table.Column<bool>(type: "INTEGER", nullable: false),
                    UltModificacion = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UserName = table.Column<string>(type: "TEXT", nullable: true),
                    IDEmpresa = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_deposito", x => x.ID);
                });

            migrationBuilder.CreateTable(
                name: "existenciaarticulodeposito",
                columns: table => new
                {
                    ID = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    IDArticulo = table.Column<long>(type: "INTEGER", nullable: false),
                    IDDeposito = table.Column<long>(type: "INTEGER", nullable: false),
                    CantidadDisponible = table.Column<decimal>(type: "TEXT", nullable: false),
                    CantidadReservada = table.Column<decimal>(type: "TEXT", nullable: false),
                    Activo = table.Column<bool>(type: "INTEGER", nullable: false),
                    UltModificacion = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UserName = table.Column<string>(type: "TEXT", nullable: true),
                    IDEmpresa = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_existenciaarticulodeposito", x => x.ID);
                    table.ForeignKey(
                        name: "FK_existenciaarticulodeposito_articulo_IDArticulo",
                        column: x => x.IDArticulo,
                        principalTable: "articulo",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_existenciaarticulodeposito_deposito_IDDeposito",
                        column: x => x.IDDeposito,
                        principalTable: "deposito",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_deposito_IDEmpresa_CodDeposito",
                table: "deposito",
                columns: new[] { "IDEmpresa", "CodDeposito" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_existenciaarticulodeposito_IDArticulo",
                table: "existenciaarticulodeposito",
                column: "IDArticulo");

            migrationBuilder.CreateIndex(
                name: "IX_existenciaarticulodeposito_IDDeposito",
                table: "existenciaarticulodeposito",
                column: "IDDeposito");

            migrationBuilder.CreateIndex(
                name: "IX_existenciaarticulodeposito_IDEmpresa_IDArticulo_IDDeposito",
                table: "existenciaarticulodeposito",
                columns: new[] { "IDEmpresa", "IDArticulo", "IDDeposito" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "existenciaarticulodeposito");

            migrationBuilder.DropTable(
                name: "deposito");
        }
    }
}
