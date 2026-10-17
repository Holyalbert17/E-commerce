using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Datamex.ECommerce.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class UnidadesMedidaYMarketing : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Subtitulo",
                table: "grupoarticuloecommerce",
                type: "TEXT",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UrlDestino",
                table: "grupoarticuloecommerce",
                type: "TEXT",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "CantidadMinimaVenta",
                table: "articuloembalaje",
                type: "TEXT",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "CodigoUnidadMedida",
                table: "articuloembalaje",
                type: "TEXT",
                maxLength: 10,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<decimal>(
                name: "IncrementoVenta",
                table: "articuloembalaje",
                type: "TEXT",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<bool>(
                name: "PermiteFraccion",
                table: "articuloembalaje",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "unidadmedidaecommerce",
                columns: table => new
                {
                    ID = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Codigo = table.Column<string>(type: "TEXT", maxLength: 10, nullable: false),
                    Nombre = table.Column<string>(type: "TEXT", maxLength: 80, nullable: false),
                    Simbolo = table.Column<string>(type: "TEXT", maxLength: 16, nullable: true),
                    PermiteDecimales = table.Column<bool>(type: "INTEGER", nullable: false),
                    IncrementoDefault = table.Column<decimal>(type: "TEXT", nullable: false),
                    Orden = table.Column<int>(type: "INTEGER", nullable: false),
                    Activo = table.Column<bool>(type: "INTEGER", nullable: false),
                    UltModificacion = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UserName = table.Column<string>(type: "TEXT", nullable: true),
                    IDEmpresa = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_unidadmedidaecommerce", x => x.ID);
                });

            migrationBuilder.CreateIndex(
                name: "IX_unidadmedidaecommerce_IDEmpresa_Codigo",
                table: "unidadmedidaecommerce",
                columns: new[] { "IDEmpresa", "Codigo" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "unidadmedidaecommerce");

            migrationBuilder.DropColumn(
                name: "Subtitulo",
                table: "grupoarticuloecommerce");

            migrationBuilder.DropColumn(
                name: "UrlDestino",
                table: "grupoarticuloecommerce");

            migrationBuilder.DropColumn(
                name: "CantidadMinimaVenta",
                table: "articuloembalaje");

            migrationBuilder.DropColumn(
                name: "CodigoUnidadMedida",
                table: "articuloembalaje");

            migrationBuilder.DropColumn(
                name: "IncrementoVenta",
                table: "articuloembalaje");

            migrationBuilder.DropColumn(
                name: "PermiteFraccion",
                table: "articuloembalaje");
        }
    }
}
