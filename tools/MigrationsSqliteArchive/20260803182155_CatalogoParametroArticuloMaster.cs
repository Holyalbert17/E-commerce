using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Datamex.ECommerce.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class CatalogoParametroArticuloMaster : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "IDLinea",
                table: "articulo",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "IDProveedor",
                table: "articulo",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "IDTipoArticulo",
                table: "articulo",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "NombreLinea",
                table: "articulo",
                type: "TEXT",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "NombreProveedor",
                table: "articulo",
                type: "TEXT",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "NombreTipoArticulo",
                table: "articulo",
                type: "TEXT",
                maxLength: 100,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "catalogoparametro",
                columns: table => new
                {
                    ID = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Tipo = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    Codigo = table.Column<int>(type: "INTEGER", nullable: false),
                    Descripcion = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Activo = table.Column<bool>(type: "INTEGER", nullable: false),
                    UltModificacion = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UserName = table.Column<string>(type: "TEXT", nullable: true),
                    IDEmpresa = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_catalogoparametro", x => x.ID);
                });

            migrationBuilder.CreateIndex(
                name: "IX_catalogoparametro_IDEmpresa_Tipo_Codigo",
                table: "catalogoparametro",
                columns: new[] { "IDEmpresa", "Tipo", "Codigo" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "catalogoparametro");

            migrationBuilder.DropColumn(
                name: "IDLinea",
                table: "articulo");

            migrationBuilder.DropColumn(
                name: "IDProveedor",
                table: "articulo");

            migrationBuilder.DropColumn(
                name: "IDTipoArticulo",
                table: "articulo");

            migrationBuilder.DropColumn(
                name: "NombreLinea",
                table: "articulo");

            migrationBuilder.DropColumn(
                name: "NombreProveedor",
                table: "articulo");

            migrationBuilder.DropColumn(
                name: "NombreTipoArticulo",
                table: "articulo");
        }
    }
}
