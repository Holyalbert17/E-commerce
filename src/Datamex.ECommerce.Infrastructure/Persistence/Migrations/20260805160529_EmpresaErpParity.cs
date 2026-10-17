using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Datamex.ECommerce.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class EmpresaErpParity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<decimal>(
                name: "MontoUmbralMayorista",
                schema: "dbo",
                table: "empresaconfiguracionecommerce",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric");

            migrationBuilder.AlterColumn<decimal>(
                name: "MontoPedidoMinimo",
                schema: "dbo",
                table: "empresaconfiguracionecommerce",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric");

            migrationBuilder.AlterColumn<string>(
                name: "ClavePublicaProcesadoraTarjeta",
                schema: "dbo",
                table: "empresaconfiguracionecommerce",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "ClavePrivadaProcesadoraTarjeta",
                schema: "dbo",
                table: "empresaconfiguracionecommerce",
                type: "character varying(40)",
                maxLength: 40,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "NombreServidorCorreoElectronico",
                schema: "dbo",
                table: "empresaconfiguracionecommerce",
                type: "character varying(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "NombreUsuarioCorreoElectronico",
                schema: "dbo",
                table: "empresaconfiguracionecommerce",
                type: "character varying(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PasswordCorreoElectronico",
                schema: "dbo",
                table: "empresaconfiguracionecommerce",
                type: "character varying(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "PuertoCorreoElectronico",
                schema: "dbo",
                table: "empresaconfiguracionecommerce",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<TimeSpan>(
                name: "TiempoAproxEntregaDelivery",
                schema: "dbo",
                table: "empresaconfiguracionecommerce",
                type: "time without time zone",
                nullable: true);

            migrationBuilder.AddColumn<TimeSpan>(
                name: "TiempoAproxEntregaPickup",
                schema: "dbo",
                table: "empresaconfiguracionecommerce",
                type: "time without time zone",
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "RazonSocial",
                schema: "dbo",
                table: "empresa",
                type: "character varying(256)",
                maxLength: 256,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(255)",
                oldMaxLength: 255);

            migrationBuilder.AlterColumn<string>(
                name: "NumeroDocumento",
                schema: "dbo",
                table: "empresa",
                type: "character varying(15)",
                maxLength: 15,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "NombreFantasia",
                schema: "dbo",
                table: "empresa",
                type: "character varying(256)",
                maxLength: 256,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<long>(
                name: "ID",
                schema: "dbo",
                table: "empresa",
                type: "bigint",
                nullable: false,
                oldClrType: typeof(long),
                oldType: "bigint")
                .OldAnnotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn);

            migrationBuilder.AddColumn<int>(
                name: "CodEmpresa",
                schema: "dbo",
                table: "empresa",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "DireccionEmpresa",
                schema: "dbo",
                table: "empresa",
                type: "character varying(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EMail",
                schema: "dbo",
                table: "empresa",
                type: "character varying(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Fax",
                schema: "dbo",
                table: "empresa",
                type: "character varying(15)",
                maxLength: 15,
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "IDPais",
                schema: "dbo",
                table: "empresa",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LogoEmpresa",
                schema: "dbo",
                table: "empresa",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RepresentanteLegal",
                schema: "dbo",
                table: "empresa",
                type: "character varying(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Telefono",
                schema: "dbo",
                table: "empresa",
                type: "character varying(15)",
                maxLength: 15,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "empresaconfiguracionecommerceredsocial",
                schema: "dbo",
                columns: table => new
                {
                    ID = table.Column<long>(type: "bigint", nullable: false),
                    IDEmpresaConfiguracionEcommerce = table.Column<long>(type: "bigint", nullable: false),
                    NombreRedSocial = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    DireccionURLRedSocial = table.Column<string>(type: "text", nullable: false),
                    LogoRedSocial = table.Column<string>(type: "text", nullable: false),
                    Activo = table.Column<bool>(type: "boolean", nullable: false),
                    UltModificacion = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UserName = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_empresaconfiguracionecommerceredsocial", x => x.ID);
                    table.ForeignKey(
                        name: "FK_empresaconfiguracionecommerceredsocial_empresaconfiguracion~",
                        column: x => x.IDEmpresaConfiguracionEcommerce,
                        principalSchema: "dbo",
                        principalTable: "empresaconfiguracionecommerce",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_empresaconfiguracionecommerceredsocial_IDEmpresaConfiguraci~",
                schema: "dbo",
                table: "empresaconfiguracionecommerceredsocial",
                column: "IDEmpresaConfiguracionEcommerce");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "empresaconfiguracionecommerceredsocial",
                schema: "dbo");

            migrationBuilder.DropColumn(
                name: "NombreServidorCorreoElectronico",
                schema: "dbo",
                table: "empresaconfiguracionecommerce");

            migrationBuilder.DropColumn(
                name: "NombreUsuarioCorreoElectronico",
                schema: "dbo",
                table: "empresaconfiguracionecommerce");

            migrationBuilder.DropColumn(
                name: "PasswordCorreoElectronico",
                schema: "dbo",
                table: "empresaconfiguracionecommerce");

            migrationBuilder.DropColumn(
                name: "PuertoCorreoElectronico",
                schema: "dbo",
                table: "empresaconfiguracionecommerce");

            migrationBuilder.DropColumn(
                name: "TiempoAproxEntregaDelivery",
                schema: "dbo",
                table: "empresaconfiguracionecommerce");

            migrationBuilder.DropColumn(
                name: "TiempoAproxEntregaPickup",
                schema: "dbo",
                table: "empresaconfiguracionecommerce");

            migrationBuilder.DropColumn(
                name: "CodEmpresa",
                schema: "dbo",
                table: "empresa");

            migrationBuilder.DropColumn(
                name: "DireccionEmpresa",
                schema: "dbo",
                table: "empresa");

            migrationBuilder.DropColumn(
                name: "EMail",
                schema: "dbo",
                table: "empresa");

            migrationBuilder.DropColumn(
                name: "Fax",
                schema: "dbo",
                table: "empresa");

            migrationBuilder.DropColumn(
                name: "IDPais",
                schema: "dbo",
                table: "empresa");

            migrationBuilder.DropColumn(
                name: "LogoEmpresa",
                schema: "dbo",
                table: "empresa");

            migrationBuilder.DropColumn(
                name: "RepresentanteLegal",
                schema: "dbo",
                table: "empresa");

            migrationBuilder.DropColumn(
                name: "Telefono",
                schema: "dbo",
                table: "empresa");

            migrationBuilder.AlterColumn<decimal>(
                name: "MontoUmbralMayorista",
                schema: "dbo",
                table: "empresaconfiguracionecommerce",
                type: "numeric",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,2)",
                oldPrecision: 18,
                oldScale: 2);

            migrationBuilder.AlterColumn<decimal>(
                name: "MontoPedidoMinimo",
                schema: "dbo",
                table: "empresaconfiguracionecommerce",
                type: "numeric",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,2)",
                oldPrecision: 18,
                oldScale: 2);

            migrationBuilder.AlterColumn<string>(
                name: "ClavePublicaProcesadoraTarjeta",
                schema: "dbo",
                table: "empresaconfiguracionecommerce",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(50)",
                oldMaxLength: 50,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "ClavePrivadaProcesadoraTarjeta",
                schema: "dbo",
                table: "empresaconfiguracionecommerce",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(40)",
                oldMaxLength: 40,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "RazonSocial",
                schema: "dbo",
                table: "empresa",
                type: "character varying(255)",
                maxLength: 255,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(256)",
                oldMaxLength: 256);

            migrationBuilder.AlterColumn<string>(
                name: "NumeroDocumento",
                schema: "dbo",
                table: "empresa",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(15)",
                oldMaxLength: 15,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "NombreFantasia",
                schema: "dbo",
                table: "empresa",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(256)",
                oldMaxLength: 256,
                oldNullable: true);

            migrationBuilder.AlterColumn<long>(
                name: "ID",
                schema: "dbo",
                table: "empresa",
                type: "bigint",
                nullable: false,
                oldClrType: typeof(long),
                oldType: "bigint")
                .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn);
        }
    }
}
