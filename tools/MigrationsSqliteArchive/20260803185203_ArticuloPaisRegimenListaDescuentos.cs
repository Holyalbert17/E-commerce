using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Datamex.ECommerce.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ArticuloPaisRegimenListaDescuentos : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "CantidadMinimaMayorista",
                table: "listaprecio",
                type: "TEXT",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "PorDescuentoMayorista",
                table: "listaprecio",
                type: "TEXT",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "PorDescuentoMinorista",
                table: "listaprecio",
                type: "TEXT",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<long>(
                name: "IDPais",
                table: "articulo",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "IDRegimenImpositivo",
                table: "articulo",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "NombrePais",
                table: "articulo",
                type: "TEXT",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "NombreRegimenImpositivo",
                table: "articulo",
                type: "TEXT",
                maxLength: 100,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CantidadMinimaMayorista",
                table: "listaprecio");

            migrationBuilder.DropColumn(
                name: "PorDescuentoMayorista",
                table: "listaprecio");

            migrationBuilder.DropColumn(
                name: "PorDescuentoMinorista",
                table: "listaprecio");

            migrationBuilder.DropColumn(
                name: "IDPais",
                table: "articulo");

            migrationBuilder.DropColumn(
                name: "IDRegimenImpositivo",
                table: "articulo");

            migrationBuilder.DropColumn(
                name: "NombrePais",
                table: "articulo");

            migrationBuilder.DropColumn(
                name: "NombreRegimenImpositivo",
                table: "articulo");
        }
    }
}
