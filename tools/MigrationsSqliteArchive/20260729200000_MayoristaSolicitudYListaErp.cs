using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Datamex.ECommerce.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class MayoristaSolicitudYListaErp : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "EstadoMayorista",
                table: "usuarioecommerce",
                type: "TEXT",
                maxLength: 20,
                nullable: false,
                defaultValue: "None");

            migrationBuilder.AddColumn<DateTime>(
                name: "FechaSolicitudMayorista",
                table: "usuarioecommerce",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MotivoSolicitudMayorista",
                table: "usuarioecommerce",
                type: "TEXT",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "MontoComprasAcumulado",
                table: "usuarioecommerce",
                type: "TEXT",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "MontoUmbralMayorista",
                table: "empresaconfiguracionecommerce",
                type: "TEXT",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<bool>(
                name: "UsarListaPrecioCuentaErp",
                table: "empresaconfiguracionecommerce",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<long>(
                name: "IDListaPrecioMayoristaDefault",
                table: "empresaconfiguracionecommerce",
                type: "INTEGER",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "EstadoMayorista", table: "usuarioecommerce");
            migrationBuilder.DropColumn(name: "FechaSolicitudMayorista", table: "usuarioecommerce");
            migrationBuilder.DropColumn(name: "MotivoSolicitudMayorista", table: "usuarioecommerce");
            migrationBuilder.DropColumn(name: "MontoComprasAcumulado", table: "usuarioecommerce");
            migrationBuilder.DropColumn(name: "MontoUmbralMayorista", table: "empresaconfiguracionecommerce");
            migrationBuilder.DropColumn(name: "UsarListaPrecioCuentaErp", table: "empresaconfiguracionecommerce");
            migrationBuilder.DropColumn(name: "IDListaPrecioMayoristaDefault", table: "empresaconfiguracionecommerce");
        }
    }
}
