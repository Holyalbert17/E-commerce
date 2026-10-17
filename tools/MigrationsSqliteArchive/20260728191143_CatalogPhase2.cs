using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Datamex.ECommerce.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class CatalogPhase2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ImagenUrl",
                table: "grupoarticuloecommerce");

            migrationBuilder.DropColumn(
                name: "Orden",
                table: "detallegrupoarticuloecommerce");

            migrationBuilder.RenameColumn(
                name: "Descripcion",
                table: "grupoarticuloecommerce",
                newName: "DescripcionGrupoArticulo");

            migrationBuilder.AddColumn<int>(
                name: "CodGrupoArticulo",
                table: "grupoarticuloecommerce",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Estado",
                table: "grupoarticuloecommerce",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "FechaFinVigencia",
                table: "grupoarticuloecommerce",
                type: "TEXT",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<DateTime>(
                name: "FechaInicioVigencia",
                table: "grupoarticuloecommerce",
                type: "TEXT",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<string>(
                name: "Imagen",
                table: "grupoarticuloecommerce",
                type: "TEXT",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "TipoGrupo",
                table: "grupoarticuloecommerce",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "FichaTecnicaUrl",
                table: "articulo",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "NombreMarca",
                table: "articulo",
                type: "TEXT",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "VideoUrl",
                table: "articulo",
                type: "TEXT",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "articuloembalaje",
                columns: table => new
                {
                    ID = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    IDArticulo = table.Column<long>(type: "INTEGER", nullable: false),
                    IDEmbalaje = table.Column<long>(type: "INTEGER", nullable: false),
                    CantidadxEmbalaje = table.Column<double>(type: "REAL", nullable: false),
                    EmbalajeParaFacturacion = table.Column<bool>(type: "INTEGER", nullable: false),
                    EmbalajeParaControlStock = table.Column<bool>(type: "INTEGER", nullable: false),
                    CodigoBarras = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    DescripcionEmbalaje = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    PrecioDemo = table.Column<decimal>(type: "TEXT", nullable: true),
                    Activo = table.Column<bool>(type: "INTEGER", nullable: false),
                    UltModificacion = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UserName = table.Column<string>(type: "TEXT", nullable: true),
                    IDEmpresa = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_articuloembalaje", x => x.ID);
                    table.ForeignKey(
                        name: "FK_articuloembalaje_articulo_IDArticulo",
                        column: x => x.IDArticulo,
                        principalTable: "articulo",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "articuloimagen",
                columns: table => new
                {
                    ID = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    IDArticulo = table.Column<long>(type: "INTEGER", nullable: false),
                    Url = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    Titulo = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    Orden = table.Column<int>(type: "INTEGER", nullable: false),
                    EsPrincipal = table.Column<bool>(type: "INTEGER", nullable: false),
                    Activo = table.Column<bool>(type: "INTEGER", nullable: false),
                    UltModificacion = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UserName = table.Column<string>(type: "TEXT", nullable: true),
                    IDEmpresa = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_articuloimagen", x => x.ID);
                    table.ForeignKey(
                        name: "FK_articuloimagen_articulo_IDArticulo",
                        column: x => x.IDArticulo,
                        principalTable: "articulo",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_detallegrupoarticuloecommerce_IDEmpresa_IDGrupoArticuloEcommerce_IDArticulo",
                table: "detallegrupoarticuloecommerce",
                columns: new[] { "IDEmpresa", "IDGrupoArticuloEcommerce", "IDArticulo" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_articuloembalaje_IDArticulo",
                table: "articuloembalaje",
                column: "IDArticulo");

            migrationBuilder.CreateIndex(
                name: "IX_articuloembalaje_IDEmpresa_IDArticulo",
                table: "articuloembalaje",
                columns: new[] { "IDEmpresa", "IDArticulo" });

            migrationBuilder.CreateIndex(
                name: "IX_articuloimagen_IDArticulo",
                table: "articuloimagen",
                column: "IDArticulo");

            migrationBuilder.CreateIndex(
                name: "IX_articuloimagen_IDEmpresa_IDArticulo",
                table: "articuloimagen",
                columns: new[] { "IDEmpresa", "IDArticulo" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "articuloembalaje");

            migrationBuilder.DropTable(
                name: "articuloimagen");

            migrationBuilder.DropIndex(
                name: "IX_detallegrupoarticuloecommerce_IDEmpresa_IDGrupoArticuloEcommerce_IDArticulo",
                table: "detallegrupoarticuloecommerce");

            migrationBuilder.DropColumn(
                name: "CodGrupoArticulo",
                table: "grupoarticuloecommerce");

            migrationBuilder.DropColumn(
                name: "Estado",
                table: "grupoarticuloecommerce");

            migrationBuilder.DropColumn(
                name: "FechaFinVigencia",
                table: "grupoarticuloecommerce");

            migrationBuilder.DropColumn(
                name: "FechaInicioVigencia",
                table: "grupoarticuloecommerce");

            migrationBuilder.DropColumn(
                name: "Imagen",
                table: "grupoarticuloecommerce");

            migrationBuilder.DropColumn(
                name: "TipoGrupo",
                table: "grupoarticuloecommerce");

            migrationBuilder.DropColumn(
                name: "FichaTecnicaUrl",
                table: "articulo");

            migrationBuilder.DropColumn(
                name: "NombreMarca",
                table: "articulo");

            migrationBuilder.DropColumn(
                name: "VideoUrl",
                table: "articulo");

            migrationBuilder.RenameColumn(
                name: "DescripcionGrupoArticulo",
                table: "grupoarticuloecommerce",
                newName: "Descripcion");

            migrationBuilder.AddColumn<string>(
                name: "ImagenUrl",
                table: "grupoarticuloecommerce",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Orden",
                table: "detallegrupoarticuloecommerce",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);
        }
    }
}
