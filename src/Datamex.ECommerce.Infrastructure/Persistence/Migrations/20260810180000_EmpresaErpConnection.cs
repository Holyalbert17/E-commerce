using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Datamex.ECommerce.Infrastructure.Persistence.Migrations;

/// <inheritdoc />
public partial class EmpresaErpConnection : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<bool>(
            name: "ErpAllowWriteBack",
            schema: "dbo",
            table: "empresaconfiguracionecommerce",
            type: "boolean",
            nullable: false,
            defaultValue: false);

        migrationBuilder.AddColumn<string>(
            name: "ErpHost",
            schema: "dbo",
            table: "empresaconfiguracionecommerce",
            type: "character varying(200)",
            maxLength: 200,
            nullable: true);

        migrationBuilder.AddColumn<int>(
            name: "ErpPort",
            schema: "dbo",
            table: "empresaconfiguracionecommerce",
            type: "integer",
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "ErpDatabase",
            schema: "dbo",
            table: "empresaconfiguracionecommerce",
            type: "character varying(120)",
            maxLength: 120,
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "ErpUsername",
            schema: "dbo",
            table: "empresaconfiguracionecommerce",
            type: "character varying(120)",
            maxLength: 120,
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "ErpPasswordProtected",
            schema: "dbo",
            table: "empresaconfiguracionecommerce",
            type: "character varying(500)",
            maxLength: 500,
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "ErpSslMode",
            schema: "dbo",
            table: "empresaconfiguracionecommerce",
            type: "character varying(40)",
            maxLength: 40,
            nullable: true);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(name: "ErpAllowWriteBack", schema: "dbo", table: "empresaconfiguracionecommerce");
        migrationBuilder.DropColumn(name: "ErpHost", schema: "dbo", table: "empresaconfiguracionecommerce");
        migrationBuilder.DropColumn(name: "ErpPort", schema: "dbo", table: "empresaconfiguracionecommerce");
        migrationBuilder.DropColumn(name: "ErpDatabase", schema: "dbo", table: "empresaconfiguracionecommerce");
        migrationBuilder.DropColumn(name: "ErpUsername", schema: "dbo", table: "empresaconfiguracionecommerce");
        migrationBuilder.DropColumn(name: "ErpPasswordProtected", schema: "dbo", table: "empresaconfiguracionecommerce");
        migrationBuilder.DropColumn(name: "ErpSslMode", schema: "dbo", table: "empresaconfiguracionecommerce");
    }
}
