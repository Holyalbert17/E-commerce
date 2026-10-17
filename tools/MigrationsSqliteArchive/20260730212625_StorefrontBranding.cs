using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Datamex.ECommerce.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class StorefrontBranding : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "BannerUrl",
                table: "empresaconfiguracionecommerce",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ColorAcento",
                table: "empresaconfiguracionecommerce",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ColorPrimario",
                table: "empresaconfiguracionecommerce",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ColorSecundario",
                table: "empresaconfiguracionecommerce",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FaviconUrl",
                table: "empresaconfiguracionecommerce",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LogoMark",
                table: "empresaconfiguracionecommerce",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LogoUrl",
                table: "empresaconfiguracionecommerce",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "NombreTiendaDisplay",
                table: "empresaconfiguracionecommerce",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TaglineTienda",
                table: "empresaconfiguracionecommerce",
                type: "TEXT",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BannerUrl",
                table: "empresaconfiguracionecommerce");

            migrationBuilder.DropColumn(
                name: "ColorAcento",
                table: "empresaconfiguracionecommerce");

            migrationBuilder.DropColumn(
                name: "ColorPrimario",
                table: "empresaconfiguracionecommerce");

            migrationBuilder.DropColumn(
                name: "ColorSecundario",
                table: "empresaconfiguracionecommerce");

            migrationBuilder.DropColumn(
                name: "FaviconUrl",
                table: "empresaconfiguracionecommerce");

            migrationBuilder.DropColumn(
                name: "LogoMark",
                table: "empresaconfiguracionecommerce");

            migrationBuilder.DropColumn(
                name: "LogoUrl",
                table: "empresaconfiguracionecommerce");

            migrationBuilder.DropColumn(
                name: "NombreTiendaDisplay",
                table: "empresaconfiguracionecommerce");

            migrationBuilder.DropColumn(
                name: "TaglineTienda",
                table: "empresaconfiguracionecommerce");
        }
    }
}
