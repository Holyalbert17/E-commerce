using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Datamex.ECommerce.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class GeographyRegisterMaps : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ChecksumActivacion",
                table: "usuarioecommerce",
                type: "TEXT",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "IDTipoDocumento",
                table: "usuarioecommerce",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "agencia",
                columns: table => new
                {
                    ID = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    CodAgencia = table.Column<int>(type: "INTEGER", nullable: false),
                    DescripcionAgencia = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Direccion = table.Column<string>(type: "TEXT", maxLength: 255, nullable: true),
                    IDCiudad = table.Column<long>(type: "INTEGER", nullable: true),
                    IDDeposito = table.Column<long>(type: "INTEGER", nullable: true),
                    Latitud = table.Column<decimal>(type: "TEXT", nullable: true),
                    Longitud = table.Column<decimal>(type: "TEXT", nullable: true),
                    EsSucursalEcommerce = table.Column<bool>(type: "INTEGER", nullable: false),
                    Activo = table.Column<bool>(type: "INTEGER", nullable: false),
                    UltModificacion = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UserName = table.Column<string>(type: "TEXT", nullable: true),
                    IDEmpresa = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_agencia", x => x.ID);
                });

            migrationBuilder.CreateTable(
                name: "cuenta",
                columns: table => new
                {
                    ID = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    NroCuenta = table.Column<long>(type: "INTEGER", nullable: false),
                    RazonSocial = table.Column<string>(type: "TEXT", maxLength: 255, nullable: false),
                    NombreCuenta = table.Column<string>(type: "TEXT", nullable: true),
                    ApellidoCuenta = table.Column<string>(type: "TEXT", nullable: true),
                    Direccion = table.Column<string>(type: "TEXT", maxLength: 255, nullable: true),
                    Email = table.Column<string>(type: "TEXT", maxLength: 60, nullable: true),
                    IDCiudad = table.Column<long>(type: "INTEGER", nullable: true),
                    IDBarrio = table.Column<long>(type: "INTEGER", nullable: true),
                    IDDivisionPolitica = table.Column<long>(type: "INTEGER", nullable: true),
                    IDListaPrecio = table.Column<long>(type: "INTEGER", nullable: true),
                    IDAgencia = table.Column<long>(type: "INTEGER", nullable: true),
                    Latitud = table.Column<decimal>(type: "TEXT", nullable: true),
                    Longitud = table.Column<decimal>(type: "TEXT", nullable: true),
                    Activo = table.Column<bool>(type: "INTEGER", nullable: false),
                    UltModificacion = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UserName = table.Column<string>(type: "TEXT", nullable: true),
                    IDEmpresa = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_cuenta", x => x.ID);
                });

            migrationBuilder.CreateTable(
                name: "divisionpolitica",
                columns: table => new
                {
                    ID = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    CodDivisionPolitica = table.Column<int>(type: "INTEGER", nullable: false),
                    DescripcionDivisionPolitica = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    NombreAbreviado = table.Column<string>(type: "TEXT", maxLength: 5, nullable: false),
                    IDPais = table.Column<long>(type: "INTEGER", nullable: true),
                    Activo = table.Column<bool>(type: "INTEGER", nullable: false),
                    UltModificacion = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UserName = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_divisionpolitica", x => x.ID);
                });

            migrationBuilder.CreateTable(
                name: "tipodocumento",
                columns: table => new
                {
                    ID = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    CodTipoDocumento = table.Column<int>(type: "INTEGER", nullable: false),
                    DescripcionTipoDocumento = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    EsRuc = table.Column<bool>(type: "INTEGER", nullable: false),
                    Activo = table.Column<bool>(type: "INTEGER", nullable: false),
                    UltModificacion = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UserName = table.Column<string>(type: "TEXT", nullable: true),
                    IDEmpresa = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tipodocumento", x => x.ID);
                });

            migrationBuilder.CreateTable(
                name: "ciudad",
                columns: table => new
                {
                    ID = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    CodCiudad = table.Column<int>(type: "INTEGER", nullable: false),
                    DescripcionCiudad = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    IDDivisionPolitica = table.Column<long>(type: "INTEGER", nullable: false),
                    CodigoSipap = table.Column<string>(type: "TEXT", maxLength: 20, nullable: true),
                    Activo = table.Column<bool>(type: "INTEGER", nullable: false),
                    UltModificacion = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UserName = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ciudad", x => x.ID);
                    table.ForeignKey(
                        name: "FK_ciudad_divisionpolitica_IDDivisionPolitica",
                        column: x => x.IDDivisionPolitica,
                        principalTable: "divisionpolitica",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "localidad",
                columns: table => new
                {
                    ID = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    CodLocalidad = table.Column<int>(type: "INTEGER", nullable: false),
                    DescripcionLocalidad = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    IDCiudad = table.Column<long>(type: "INTEGER", nullable: false),
                    Activo = table.Column<bool>(type: "INTEGER", nullable: false),
                    UltModificacion = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UserName = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_localidad", x => x.ID);
                    table.ForeignKey(
                        name: "FK_localidad_ciudad_IDCiudad",
                        column: x => x.IDCiudad,
                        principalTable: "ciudad",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "distrito",
                columns: table => new
                {
                    ID = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    CodDistrito = table.Column<int>(type: "INTEGER", nullable: false),
                    DescripcionDistrito = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    IDLocalidad = table.Column<long>(type: "INTEGER", nullable: false),
                    Activo = table.Column<bool>(type: "INTEGER", nullable: false),
                    UltModificacion = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UserName = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_distrito", x => x.ID);
                    table.ForeignKey(
                        name: "FK_distrito_localidad_IDLocalidad",
                        column: x => x.IDLocalidad,
                        principalTable: "localidad",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "barrio",
                columns: table => new
                {
                    ID = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    CodBarrio = table.Column<int>(type: "INTEGER", nullable: false),
                    DescripcionBarrio = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    IDDistrito = table.Column<long>(type: "INTEGER", nullable: true),
                    Activo = table.Column<bool>(type: "INTEGER", nullable: false),
                    UltModificacion = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UserName = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_barrio", x => x.ID);
                    table.ForeignKey(
                        name: "FK_barrio_distrito_IDDistrito",
                        column: x => x.IDDistrito,
                        principalTable: "distrito",
                        principalColumn: "ID");
                });

            migrationBuilder.CreateIndex(
                name: "IX_usuarioecommerce_IDTipoDocumento",
                table: "usuarioecommerce",
                column: "IDTipoDocumento");

            migrationBuilder.CreateIndex(
                name: "IX_tarifadelivery_IDBarrio",
                table: "tarifadelivery",
                column: "IDBarrio");

            migrationBuilder.CreateIndex(
                name: "IX_tarifadelivery_IDCiudad",
                table: "tarifadelivery",
                column: "IDCiudad");

            migrationBuilder.CreateIndex(
                name: "IX_tarifadelivery_IDDivisionPolitica",
                table: "tarifadelivery",
                column: "IDDivisionPolitica");

            migrationBuilder.CreateIndex(
                name: "IX_agencia_IDEmpresa_CodAgencia",
                table: "agencia",
                columns: new[] { "IDEmpresa", "CodAgencia" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_barrio_CodBarrio",
                table: "barrio",
                column: "CodBarrio");

            migrationBuilder.CreateIndex(
                name: "IX_barrio_IDDistrito",
                table: "barrio",
                column: "IDDistrito");

            migrationBuilder.CreateIndex(
                name: "IX_ciudad_CodCiudad",
                table: "ciudad",
                column: "CodCiudad");

            migrationBuilder.CreateIndex(
                name: "IX_ciudad_IDDivisionPolitica",
                table: "ciudad",
                column: "IDDivisionPolitica");

            migrationBuilder.CreateIndex(
                name: "IX_cuenta_IDEmpresa_NroCuenta",
                table: "cuenta",
                columns: new[] { "IDEmpresa", "NroCuenta" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_distrito_IDLocalidad",
                table: "distrito",
                column: "IDLocalidad");

            migrationBuilder.CreateIndex(
                name: "IX_divisionpolitica_CodDivisionPolitica",
                table: "divisionpolitica",
                column: "CodDivisionPolitica");

            migrationBuilder.CreateIndex(
                name: "IX_localidad_IDCiudad",
                table: "localidad",
                column: "IDCiudad");

            migrationBuilder.CreateIndex(
                name: "IX_tipodocumento_IDEmpresa_CodTipoDocumento",
                table: "tipodocumento",
                columns: new[] { "IDEmpresa", "CodTipoDocumento" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_tarifadelivery_barrio_IDBarrio",
                table: "tarifadelivery",
                column: "IDBarrio",
                principalTable: "barrio",
                principalColumn: "ID",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_tarifadelivery_ciudad_IDCiudad",
                table: "tarifadelivery",
                column: "IDCiudad",
                principalTable: "ciudad",
                principalColumn: "ID",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_tarifadelivery_divisionpolitica_IDDivisionPolitica",
                table: "tarifadelivery",
                column: "IDDivisionPolitica",
                principalTable: "divisionpolitica",
                principalColumn: "ID",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_usuarioecommerce_tipodocumento_IDTipoDocumento",
                table: "usuarioecommerce",
                column: "IDTipoDocumento",
                principalTable: "tipodocumento",
                principalColumn: "ID",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_tarifadelivery_barrio_IDBarrio",
                table: "tarifadelivery");

            migrationBuilder.DropForeignKey(
                name: "FK_tarifadelivery_ciudad_IDCiudad",
                table: "tarifadelivery");

            migrationBuilder.DropForeignKey(
                name: "FK_tarifadelivery_divisionpolitica_IDDivisionPolitica",
                table: "tarifadelivery");

            migrationBuilder.DropForeignKey(
                name: "FK_usuarioecommerce_tipodocumento_IDTipoDocumento",
                table: "usuarioecommerce");

            migrationBuilder.DropTable(
                name: "agencia");

            migrationBuilder.DropTable(
                name: "barrio");

            migrationBuilder.DropTable(
                name: "cuenta");

            migrationBuilder.DropTable(
                name: "tipodocumento");

            migrationBuilder.DropTable(
                name: "distrito");

            migrationBuilder.DropTable(
                name: "localidad");

            migrationBuilder.DropTable(
                name: "ciudad");

            migrationBuilder.DropTable(
                name: "divisionpolitica");

            migrationBuilder.DropIndex(
                name: "IX_usuarioecommerce_IDTipoDocumento",
                table: "usuarioecommerce");

            migrationBuilder.DropIndex(
                name: "IX_tarifadelivery_IDBarrio",
                table: "tarifadelivery");

            migrationBuilder.DropIndex(
                name: "IX_tarifadelivery_IDCiudad",
                table: "tarifadelivery");

            migrationBuilder.DropIndex(
                name: "IX_tarifadelivery_IDDivisionPolitica",
                table: "tarifadelivery");

            migrationBuilder.DropColumn(
                name: "ChecksumActivacion",
                table: "usuarioecommerce");

            migrationBuilder.DropColumn(
                name: "IDTipoDocumento",
                table: "usuarioecommerce");
        }
    }
}
