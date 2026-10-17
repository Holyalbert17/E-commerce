using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Datamex.ECommerce.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class EmpresaModoIntegracion : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ModoIntegracion",
                table: "empresaconfiguracionecommerce",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ModoIntegracion",
                table: "empresaconfiguracionecommerce");
        }
    }
}
