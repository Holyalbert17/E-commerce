using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Datamex.ECommerce.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ChoferBarcodePedidoEntrega : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "DireccionEntrega",
                table: "pedidomovil",
                type: "TEXT",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "IDChofer",
                table: "pedidomovil",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "articuloembalajecodigobarra",
                columns: table => new
                {
                    ID = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    IDArticuloEmbalaje = table.Column<long>(type: "INTEGER", nullable: false),
                    CodigoBarra = table.Column<string>(type: "TEXT", maxLength: 400, nullable: false),
                    ParaSupermercado = table.Column<bool>(type: "INTEGER", nullable: false),
                    Activo = table.Column<bool>(type: "INTEGER", nullable: false),
                    UltModificacion = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UserName = table.Column<string>(type: "TEXT", nullable: true),
                    IDEmpresa = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_articuloembalajecodigobarra", x => x.ID);
                    table.ForeignKey(
                        name: "FK_articuloembalajecodigobarra_articuloembalaje_IDArticuloEmbalaje",
                        column: x => x.IDArticuloEmbalaje,
                        principalTable: "articuloembalaje",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "chofer",
                columns: table => new
                {
                    ID = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    NroChofer = table.Column<int>(type: "INTEGER", nullable: false),
                    Nombre = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Documento = table.Column<string>(type: "TEXT", maxLength: 60, nullable: true),
                    Telefono = table.Column<string>(type: "TEXT", maxLength: 40, nullable: true),
                    Observacion = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    Activo = table.Column<bool>(type: "INTEGER", nullable: false),
                    UltModificacion = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UserName = table.Column<string>(type: "TEXT", nullable: true),
                    IDEmpresa = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_chofer", x => x.ID);
                });

            migrationBuilder.CreateIndex(
                name: "IX_pedidomovil_IDChofer",
                table: "pedidomovil",
                column: "IDChofer");

            migrationBuilder.CreateIndex(
                name: "IX_articuloembalajecodigobarra_IDArticuloEmbalaje",
                table: "articuloembalajecodigobarra",
                column: "IDArticuloEmbalaje");

            migrationBuilder.CreateIndex(
                name: "IX_articuloembalajecodigobarra_IDEmpresa_CodigoBarra",
                table: "articuloembalajecodigobarra",
                columns: new[] { "IDEmpresa", "CodigoBarra" });

            migrationBuilder.CreateIndex(
                name: "IX_articuloembalajecodigobarra_IDEmpresa_IDArticuloEmbalaje",
                table: "articuloembalajecodigobarra",
                columns: new[] { "IDEmpresa", "IDArticuloEmbalaje" });

            migrationBuilder.CreateIndex(
                name: "IX_chofer_IDEmpresa_NroChofer",
                table: "chofer",
                columns: new[] { "IDEmpresa", "NroChofer" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_pedidomovil_chofer_IDChofer",
                table: "pedidomovil",
                column: "IDChofer",
                principalTable: "chofer",
                principalColumn: "ID",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_pedidomovil_chofer_IDChofer",
                table: "pedidomovil");

            migrationBuilder.DropTable(
                name: "articuloembalajecodigobarra");

            migrationBuilder.DropTable(
                name: "chofer");

            migrationBuilder.DropIndex(
                name: "IX_pedidomovil_IDChofer",
                table: "pedidomovil");

            migrationBuilder.DropColumn(
                name: "DireccionEntrega",
                table: "pedidomovil");

            migrationBuilder.DropColumn(
                name: "IDChofer",
                table: "pedidomovil");
        }
    }
}
