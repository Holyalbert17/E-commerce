using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Datamex.ECommerce.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AdminUsuarioFuncionalidadHierarchy : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "IDFuncionalidadPadre",
                schema: "dbo",
                table: "adminfuncionalidad",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Orden",
                schema: "dbo",
                table: "adminfuncionalidad",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "adminusuariofuncionalidad",
                schema: "dbo",
                columns: table => new
                {
                    ID = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    IDUsuario = table.Column<long>(type: "bigint", nullable: false),
                    IDFuncionalidad = table.Column<long>(type: "bigint", nullable: false),
                    Activo = table.Column<bool>(type: "boolean", nullable: false),
                    UltModificacion = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UserName = table.Column<string>(type: "text", nullable: true),
                    IDEmpresa = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_adminusuariofuncionalidad", x => x.ID);
                });

            migrationBuilder.CreateIndex(
                name: "IX_adminfuncionalidad_IDEmpresa_IDFuncionalidadPadre",
                schema: "dbo",
                table: "adminfuncionalidad",
                columns: new[] { "IDEmpresa", "IDFuncionalidadPadre" });

            migrationBuilder.CreateIndex(
                name: "IX_adminusuariofuncionalidad_IDEmpresa_IDUsuario_IDFuncionalid~",
                schema: "dbo",
                table: "adminusuariofuncionalidad",
                columns: new[] { "IDEmpresa", "IDUsuario", "IDFuncionalidad" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "adminusuariofuncionalidad",
                schema: "dbo");

            migrationBuilder.DropIndex(
                name: "IX_adminfuncionalidad_IDEmpresa_IDFuncionalidadPadre",
                schema: "dbo",
                table: "adminfuncionalidad");

            migrationBuilder.DropColumn(
                name: "IDFuncionalidadPadre",
                schema: "dbo",
                table: "adminfuncionalidad");

            migrationBuilder.DropColumn(
                name: "Orden",
                schema: "dbo",
                table: "adminfuncionalidad");
        }
    }
}
