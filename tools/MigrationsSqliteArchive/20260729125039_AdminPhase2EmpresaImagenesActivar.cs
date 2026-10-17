using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Datamex.ECommerce.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AdminPhase2EmpresaImagenesActivar : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "horarioatencion",
                columns: table => new
                {
                    ID = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    IDAgencia = table.Column<long>(type: "INTEGER", nullable: false),
                    DiaSemana = table.Column<byte>(type: "INTEGER", nullable: false),
                    HoraPrimeraApertura = table.Column<TimeSpan>(type: "TEXT", nullable: false),
                    HoraPrimerCierre = table.Column<TimeSpan>(type: "TEXT", nullable: false),
                    HoraSegundaApertura = table.Column<TimeSpan>(type: "TEXT", nullable: false),
                    HoraSegundoCierre = table.Column<TimeSpan>(type: "TEXT", nullable: false),
                    Activo = table.Column<bool>(type: "INTEGER", nullable: false),
                    UltModificacion = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UserName = table.Column<string>(type: "TEXT", nullable: true),
                    IDEmpresa = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_horarioatencion", x => x.ID);
                    table.ForeignKey(
                        name: "FK_horarioatencion_agencia_IDAgencia",
                        column: x => x.IDAgencia,
                        principalTable: "agencia",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_horarioatencion_IDAgencia",
                table: "horarioatencion",
                column: "IDAgencia");

            migrationBuilder.CreateIndex(
                name: "IX_horarioatencion_IDEmpresa_IDAgencia_DiaSemana",
                table: "horarioatencion",
                columns: new[] { "IDEmpresa", "IDAgencia", "DiaSemana" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "horarioatencion");
        }
    }
}
