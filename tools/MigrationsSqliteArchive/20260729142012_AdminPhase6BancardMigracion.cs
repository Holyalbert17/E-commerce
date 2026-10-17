using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Datamex.ECommerce.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AdminPhase6BancardMigracion : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "UltIDProcesoNegocio",
                table: "empresaconfiguracionecommerce",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.CreateTable(
                name: "logpagotarjeta",
                columns: table => new
                {
                    ID = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    IDCarritoCompra = table.Column<long>(type: "INTEGER", nullable: false),
                    ShopProcessId = table.Column<long>(type: "INTEGER", nullable: false),
                    ProcessId = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    Monto = table.Column<decimal>(type: "TEXT", nullable: false),
                    Status = table.Column<string>(type: "TEXT", maxLength: 30, nullable: false),
                    AuthorizationCode = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    RawBody = table.Column<string>(type: "TEXT", nullable: true),
                    Fecha = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Activo = table.Column<bool>(type: "INTEGER", nullable: false),
                    UltModificacion = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UserName = table.Column<string>(type: "TEXT", nullable: true),
                    IDEmpresa = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_logpagotarjeta", x => x.ID);
                });

            migrationBuilder.CreateIndex(
                name: "IX_logpagotarjeta_IDCarritoCompra",
                table: "logpagotarjeta",
                column: "IDCarritoCompra");

            migrationBuilder.CreateIndex(
                name: "IX_logpagotarjeta_IDEmpresa_ShopProcessId",
                table: "logpagotarjeta",
                columns: new[] { "IDEmpresa", "ShopProcessId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "logpagotarjeta");

            migrationBuilder.DropColumn(
                name: "UltIDProcesoNegocio",
                table: "empresaconfiguracionecommerce");
        }
    }
}
