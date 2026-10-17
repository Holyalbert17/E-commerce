using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Datamex.ECommerce.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AdminBackofficeRbac : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "adminfuncionalidad",
                columns: table => new
                {
                    ID = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    CodigoPermission = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Descripcion = table.Column<string>(type: "TEXT", maxLength: 400, nullable: false),
                    Activo = table.Column<bool>(type: "INTEGER", nullable: false),
                    UltModificacion = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UserName = table.Column<string>(type: "TEXT", nullable: true),
                    IDEmpresa = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_adminfuncionalidad", x => x.ID);
                });

            migrationBuilder.CreateTable(
                name: "adminrol",
                columns: table => new
                {
                    ID = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    CodigoRol = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    NombreRol = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Activo = table.Column<bool>(type: "INTEGER", nullable: false),
                    UltModificacion = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UserName = table.Column<string>(type: "TEXT", nullable: true),
                    IDEmpresa = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_adminrol", x => x.ID);
                });

            migrationBuilder.CreateTable(
                name: "adminrolfuncionalidad",
                columns: table => new
                {
                    ID = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    IDRol = table.Column<long>(type: "INTEGER", nullable: false),
                    IDFuncionalidad = table.Column<long>(type: "INTEGER", nullable: false),
                    Activo = table.Column<bool>(type: "INTEGER", nullable: false),
                    UltModificacion = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UserName = table.Column<string>(type: "TEXT", nullable: true),
                    IDEmpresa = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_adminrolfuncionalidad", x => x.ID);
                });

            migrationBuilder.CreateTable(
                name: "adminusuario",
                columns: table => new
                {
                    ID = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Usuario = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    PasswordHash = table.Column<string>(type: "TEXT", maxLength: 400, nullable: false),
                    Nombre = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    Email = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    Activo = table.Column<bool>(type: "INTEGER", nullable: false),
                    UltModificacion = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UserName = table.Column<string>(type: "TEXT", nullable: true),
                    IDEmpresa = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_adminusuario", x => x.ID);
                });

            migrationBuilder.CreateTable(
                name: "adminusuariorol",
                columns: table => new
                {
                    ID = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    IDUsuario = table.Column<long>(type: "INTEGER", nullable: false),
                    IDRol = table.Column<long>(type: "INTEGER", nullable: false),
                    Activo = table.Column<bool>(type: "INTEGER", nullable: false),
                    UltModificacion = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UserName = table.Column<string>(type: "TEXT", nullable: true),
                    IDEmpresa = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_adminusuariorol", x => x.ID);
                });

            migrationBuilder.CreateIndex(
                name: "IX_adminfuncionalidad_IDEmpresa_CodigoPermission",
                table: "adminfuncionalidad",
                columns: new[] { "IDEmpresa", "CodigoPermission" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_adminrol_IDEmpresa_CodigoRol",
                table: "adminrol",
                columns: new[] { "IDEmpresa", "CodigoRol" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_adminrolfuncionalidad_IDEmpresa_IDRol_IDFuncionalidad",
                table: "adminrolfuncionalidad",
                columns: new[] { "IDEmpresa", "IDRol", "IDFuncionalidad" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_adminusuario_IDEmpresa_Usuario",
                table: "adminusuario",
                columns: new[] { "IDEmpresa", "Usuario" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_adminusuariorol_IDEmpresa_IDUsuario_IDRol",
                table: "adminusuariorol",
                columns: new[] { "IDEmpresa", "IDUsuario", "IDRol" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "adminfuncionalidad");

            migrationBuilder.DropTable(
                name: "adminrol");

            migrationBuilder.DropTable(
                name: "adminrolfuncionalidad");

            migrationBuilder.DropTable(
                name: "adminusuario");

            migrationBuilder.DropTable(
                name: "adminusuariorol");
        }
    }
}
