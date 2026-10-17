using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Datamex.ECommerce.Infrastructure.Persistence.Migrations;

/// <inheritdoc />
public partial class SyncStockAndMigrarPedidosToggles : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<bool>(
            name: "SyncStockContinuo",
            schema: "dbo",
            table: "empresaconfiguracionecommerce",
            type: "boolean",
            nullable: false,
            defaultValue: true);

        migrationBuilder.AddColumn<bool>(
            name: "MigrarPedidosAlGenerar",
            schema: "dbo",
            table: "empresaconfiguracionecommerce",
            type: "boolean",
            nullable: false,
            defaultValue: true);

        migrationBuilder.AddColumn<int>(
            name: "SyncStockIntervaloMinutos",
            schema: "dbo",
            table: "empresaconfiguracionecommerce",
            type: "integer",
            nullable: false,
            defaultValue: 5);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "SyncStockContinuo",
            schema: "dbo",
            table: "empresaconfiguracionecommerce");

        migrationBuilder.DropColumn(
            name: "MigrarPedidosAlGenerar",
            schema: "dbo",
            table: "empresaconfiguracionecommerce");

        migrationBuilder.DropColumn(
            name: "SyncStockIntervaloMinutos",
            schema: "dbo",
            table: "empresaconfiguracionecommerce");
    }
}
