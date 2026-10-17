using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Datamex.ECommerce.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ShippingAccountOrders : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "tarifadelivery",
                columns: table => new
                {
                    ID = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    IDDivisionPolitica = table.Column<long>(type: "INTEGER", nullable: false),
                    IDCiudad = table.Column<long>(type: "INTEGER", nullable: false),
                    IDBarrio = table.Column<long>(type: "INTEGER", nullable: false),
                    FechaInicioVigencia = table.Column<DateTime>(type: "TEXT", nullable: false),
                    FechaFinVigencia = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ImporteDelivery = table.Column<decimal>(type: "TEXT", nullable: false),
                    Estado = table.Column<int>(type: "INTEGER", nullable: false),
                    IDAgencia = table.Column<long>(type: "INTEGER", nullable: true),
                    NombreCiudad = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    NombreBarrio = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Activo = table.Column<bool>(type: "INTEGER", nullable: false),
                    UltModificacion = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UserName = table.Column<string>(type: "TEXT", nullable: true),
                    IDEmpresa = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tarifadelivery", x => x.ID);
                });

            migrationBuilder.CreateIndex(
                name: "IX_tarifadelivery_IDEmpresa_Activo_Estado",
                table: "tarifadelivery",
                columns: new[] { "IDEmpresa", "Activo", "Estado" });

            migrationBuilder.CreateIndex(
                name: "IX_tarifadelivery_IDEmpresa_IDBarrio_FechaInicioVigencia",
                table: "tarifadelivery",
                columns: new[] { "IDEmpresa", "IDBarrio", "FechaInicioVigencia" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "tarifadelivery");
        }
    }
}
