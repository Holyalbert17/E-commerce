using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Datamex.ECommerce.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class PedidoTrackingShareToken : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "TokenTrackingPublico",
                table: "pedidomovil",
                type: "TEXT",
                maxLength: 64,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_pedidomovil_TokenTrackingPublico",
                table: "pedidomovil",
                column: "TokenTrackingPublico",
                unique: true,
                filter: "[TokenTrackingPublico] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_pedidomovil_TokenTrackingPublico",
                table: "pedidomovil");

            migrationBuilder.DropColumn(
                name: "TokenTrackingPublico",
                table: "pedidomovil");
        }
    }
}
