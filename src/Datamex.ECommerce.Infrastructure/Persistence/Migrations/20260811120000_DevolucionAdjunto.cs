using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Datamex.ECommerce.Infrastructure.Persistence.Migrations;

/// <inheritdoc />
public partial class DevolucionAdjunto : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "UrlAdjunto",
            schema: "dbo",
            table: "devolucionecommerce",
            type: "character varying(800)",
            maxLength: 800,
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "MimeType",
            schema: "dbo",
            table: "devolucionecommerce",
            type: "character varying(100)",
            maxLength: 100,
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "NombreArchivo",
            schema: "dbo",
            table: "devolucionecommerce",
            type: "character varying(260)",
            maxLength: 260,
            nullable: true);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "UrlAdjunto",
            schema: "dbo",
            table: "devolucionecommerce");

        migrationBuilder.DropColumn(
            name: "MimeType",
            schema: "dbo",
            table: "devolucionecommerce");

        migrationBuilder.DropColumn(
            name: "NombreArchivo",
            schema: "dbo",
            table: "devolucionecommerce");
    }
}
