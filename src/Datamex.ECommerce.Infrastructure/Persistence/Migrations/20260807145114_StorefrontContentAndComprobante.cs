using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Datamex.ECommerce.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class StorefrontContentAndComprobante : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ContenidoComoComprarHtml",
                schema: "dbo",
                table: "empresaconfiguracionecommerce",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ContenidoFaqHtml",
                schema: "dbo",
                table: "empresaconfiguracionecommerce",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ContenidoTerminosHtml",
                schema: "dbo",
                table: "empresaconfiguracionecommerce",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CreditoDesarrolladoPor",
                schema: "dbo",
                table: "empresaconfiguracionecommerce",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CreditoDesarrolladoPorUrl",
                schema: "dbo",
                table: "empresaconfiguracionecommerce",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "DeliveryEmiteFactura",
                schema: "dbo",
                table: "empresaconfiguracionecommerce",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<bool>(
                name: "DeliveryEmiteTicket",
                schema: "dbo",
                table: "empresaconfiguracionecommerce",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "PickupEmiteFactura",
                schema: "dbo",
                table: "empresaconfiguracionecommerce",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<bool>(
                name: "PickupEmiteTicket",
                schema: "dbo",
                table: "empresaconfiguracionecommerce",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "contactomensaje",
                schema: "dbo",
                columns: table => new
                {
                    ID = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Nombre = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    Email = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    Telefono = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true),
                    Mensaje = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: false),
                    FechaUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Leido = table.Column<bool>(type: "boolean", nullable: false),
                    Activo = table.Column<bool>(type: "boolean", nullable: false),
                    UltModificacion = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UserName = table.Column<string>(type: "text", nullable: true),
                    IDEmpresa = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_contactomensaje", x => x.ID);
                });

            migrationBuilder.CreateIndex(
                name: "IX_contactomensaje_IDEmpresa_FechaUtc",
                schema: "dbo",
                table: "contactomensaje",
                columns: new[] { "IDEmpresa", "FechaUtc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "contactomensaje",
                schema: "dbo");

            migrationBuilder.DropColumn(
                name: "ContenidoComoComprarHtml",
                schema: "dbo",
                table: "empresaconfiguracionecommerce");

            migrationBuilder.DropColumn(
                name: "ContenidoFaqHtml",
                schema: "dbo",
                table: "empresaconfiguracionecommerce");

            migrationBuilder.DropColumn(
                name: "ContenidoTerminosHtml",
                schema: "dbo",
                table: "empresaconfiguracionecommerce");

            migrationBuilder.DropColumn(
                name: "CreditoDesarrolladoPor",
                schema: "dbo",
                table: "empresaconfiguracionecommerce");

            migrationBuilder.DropColumn(
                name: "CreditoDesarrolladoPorUrl",
                schema: "dbo",
                table: "empresaconfiguracionecommerce");

            migrationBuilder.DropColumn(
                name: "DeliveryEmiteFactura",
                schema: "dbo",
                table: "empresaconfiguracionecommerce");

            migrationBuilder.DropColumn(
                name: "DeliveryEmiteTicket",
                schema: "dbo",
                table: "empresaconfiguracionecommerce");

            migrationBuilder.DropColumn(
                name: "PickupEmiteFactura",
                schema: "dbo",
                table: "empresaconfiguracionecommerce");

            migrationBuilder.DropColumn(
                name: "PickupEmiteTicket",
                schema: "dbo",
                table: "empresaconfiguracionecommerce");
        }
    }
}
