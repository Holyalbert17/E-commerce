using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Datamex.ECommerce.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ConfiguracionPersonalizacion : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "configuracionpersonalizacion",
                columns: table => new
                {
                    ID = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Clave = table.Column<string>(type: "TEXT", maxLength: 80, nullable: false),
                    ValorJson = table.Column<string>(type: "TEXT", nullable: false),
                    Version = table.Column<int>(type: "INTEGER", nullable: false),
                    FechaInicioVigencia = table.Column<DateTime>(type: "TEXT", nullable: true),
                    FechaFinVigencia = table.Column<DateTime>(type: "TEXT", nullable: true),
                    CreadoPor = table.Column<string>(type: "TEXT", maxLength: 80, nullable: true),
                    FechaCreacion = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Nota = table.Column<string>(type: "TEXT", maxLength: 300, nullable: true),
                    Activo = table.Column<bool>(type: "INTEGER", nullable: false),
                    UltModificacion = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UserName = table.Column<string>(type: "TEXT", nullable: true),
                    IDEmpresa = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_configuracionpersonalizacion", x => x.ID);
                });

            migrationBuilder.CreateIndex(
                name: "IX_configuracionpersonalizacion_IDEmpresa_Clave_Activo",
                table: "configuracionpersonalizacion",
                columns: new[] { "IDEmpresa", "Clave", "Activo" });

            migrationBuilder.CreateIndex(
                name: "IX_configuracionpersonalizacion_IDEmpresa_Clave_Version",
                table: "configuracionpersonalizacion",
                columns: new[] { "IDEmpresa", "Clave", "Version" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "configuracionpersonalizacion");
        }
    }
}
