using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Datamex.ECommerce.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class PasswordResetTokens : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CodigoResetPassword",
                table: "usuarioecommerce",
                type: "TEXT",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ChecksumResetPassword",
                table: "usuarioecommerce",
                type: "TEXT",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "FechaExpiraResetPassword",
                table: "usuarioecommerce",
                type: "TEXT",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "CodigoResetPassword", table: "usuarioecommerce");
            migrationBuilder.DropColumn(name: "ChecksumResetPassword", table: "usuarioecommerce");
            migrationBuilder.DropColumn(name: "FechaExpiraResetPassword", table: "usuarioecommerce");
        }
    }
}
