using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Datamex.ECommerce.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class FooterBrandingLocal : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CiudadLocal",
                table: "empresaconfiguracionecommerce",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DireccionLocal",
                table: "empresaconfiguracionecommerce",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DireccionURLTikTok",
                table: "empresaconfiguracionecommerce",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EmailContacto",
                table: "empresaconfiguracionecommerce",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FooterDescripcion",
                table: "empresaconfiguracionecommerce",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "HorarioAtencionTexto",
                table: "empresaconfiguracionecommerce",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "LatitudLocal",
                table: "empresaconfiguracionecommerce",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "LongitudLocal",
                table: "empresaconfiguracionecommerce",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "MostrarMapaFooter",
                table: "empresaconfiguracionecommerce",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "TelefonoContacto",
                table: "empresaconfiguracionecommerce",
                type: "TEXT",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CiudadLocal",
                table: "empresaconfiguracionecommerce");

            migrationBuilder.DropColumn(
                name: "DireccionLocal",
                table: "empresaconfiguracionecommerce");

            migrationBuilder.DropColumn(
                name: "DireccionURLTikTok",
                table: "empresaconfiguracionecommerce");

            migrationBuilder.DropColumn(
                name: "EmailContacto",
                table: "empresaconfiguracionecommerce");

            migrationBuilder.DropColumn(
                name: "FooterDescripcion",
                table: "empresaconfiguracionecommerce");

            migrationBuilder.DropColumn(
                name: "HorarioAtencionTexto",
                table: "empresaconfiguracionecommerce");

            migrationBuilder.DropColumn(
                name: "LatitudLocal",
                table: "empresaconfiguracionecommerce");

            migrationBuilder.DropColumn(
                name: "LongitudLocal",
                table: "empresaconfiguracionecommerce");

            migrationBuilder.DropColumn(
                name: "MostrarMapaFooter",
                table: "empresaconfiguracionecommerce");

            migrationBuilder.DropColumn(
                name: "TelefonoContacto",
                table: "empresaconfiguracionecommerce");
        }
    }
}
