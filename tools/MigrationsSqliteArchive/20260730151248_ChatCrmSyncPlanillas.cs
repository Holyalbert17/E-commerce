using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Datamex.ECommerce.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ChatCrmSyncPlanillas : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "chatconversacionecommerce",
                columns: table => new
                {
                    ID = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    IDUsuarioEcommerce = table.Column<long>(type: "INTEGER", nullable: false),
                    IDPedidoMovil = table.Column<long>(type: "INTEGER", nullable: true),
                    Email = table.Column<string>(type: "TEXT", maxLength: 120, nullable: false),
                    NombreCliente = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    Asunto = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false),
                    Estado = table.Column<string>(type: "TEXT", maxLength: 30, nullable: false),
                    IDAdminAsignado = table.Column<long>(type: "INTEGER", nullable: true),
                    NombreAdminAsignado = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    FechaInicio = table.Column<DateTime>(type: "TEXT", nullable: false),
                    FechaUltimoMensaje = table.Column<DateTime>(type: "TEXT", nullable: false),
                    PreviewUltimoMensaje = table.Column<string>(type: "TEXT", maxLength: 240, nullable: true),
                    NoLeidosCliente = table.Column<int>(type: "INTEGER", nullable: false),
                    NoLeidosSoporte = table.Column<int>(type: "INTEGER", nullable: false),
                    Activo = table.Column<bool>(type: "INTEGER", nullable: false),
                    UltModificacion = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UserName = table.Column<string>(type: "TEXT", nullable: true),
                    IDEmpresa = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_chatconversacionecommerce", x => x.ID);
                });

            migrationBuilder.CreateTable(
                name: "migracionpedidolog",
                columns: table => new
                {
                    ID = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    IDPedidoMovil = table.Column<long>(type: "INTEGER", nullable: false),
                    IDCarritoCompra = table.Column<long>(type: "INTEGER", nullable: true),
                    NroPedido = table.Column<long>(type: "INTEGER", nullable: false),
                    Estado = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    MensajeError = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    DetalleTecnico = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: true),
                    IDPedidoMovilErp = table.Column<long>(type: "INTEGER", nullable: true),
                    Intentos = table.Column<int>(type: "INTEGER", nullable: false),
                    FechaIntento = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UserNameOperador = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    Activo = table.Column<bool>(type: "INTEGER", nullable: false),
                    UltModificacion = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UserName = table.Column<string>(type: "TEXT", nullable: true),
                    IDEmpresa = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_migracionpedidolog", x => x.ID);
                });

            migrationBuilder.CreateTable(
                name: "synccursor",
                columns: table => new
                {
                    ID = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    EntityKey = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false),
                    LastWatermarkUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    LastId = table.Column<long>(type: "INTEGER", nullable: true),
                    LastRunUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    LastStatus = table.Column<string>(type: "TEXT", maxLength: 40, nullable: true),
                    LastError = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true),
                    RowsProcessed = table.Column<int>(type: "INTEGER", nullable: false),
                    Activo = table.Column<bool>(type: "INTEGER", nullable: false),
                    UltModificacion = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UserName = table.Column<string>(type: "TEXT", nullable: true),
                    IDEmpresa = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_synccursor", x => x.ID);
                });

            migrationBuilder.AddColumn<long>(
                name: "IDConversacion",
                table: "chatsoportemensajeecommerce",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "IDAdminUsuario",
                table: "chatsoportemensajeecommerce",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TipoMensaje",
                table: "chatsoportemensajeecommerce",
                type: "TEXT",
                maxLength: 20,
                nullable: false,
                defaultValue: "texto");

            migrationBuilder.AddColumn<string>(
                name: "UrlAdjunto",
                table: "chatsoportemensajeecommerce",
                type: "TEXT",
                maxLength: 800,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MimeType",
                table: "chatsoportemensajeecommerce",
                type: "TEXT",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "NombreArchivo",
                table: "chatsoportemensajeecommerce",
                type: "TEXT",
                maxLength: 260,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_chatconversacionecommerce_IDEmpresa_IDUsuarioEcommerce_Estado",
                table: "chatconversacionecommerce",
                columns: new[] { "IDEmpresa", "IDUsuarioEcommerce", "Estado" });

            migrationBuilder.CreateIndex(
                name: "IX_chatconversacionecommerce_IDEmpresa_FechaUltimoMensaje",
                table: "chatconversacionecommerce",
                columns: new[] { "IDEmpresa", "FechaUltimoMensaje" });

            migrationBuilder.CreateIndex(
                name: "IX_chatsoportemensajeecommerce_IDEmpresa_IDConversacion_FechaMensaje",
                table: "chatsoportemensajeecommerce",
                columns: new[] { "IDEmpresa", "IDConversacion", "FechaMensaje" });

            migrationBuilder.CreateIndex(
                name: "IX_chatsoportemensajeecommerce_IDConversacion",
                table: "chatsoportemensajeecommerce",
                column: "IDConversacion");

            migrationBuilder.CreateIndex(
                name: "IX_migracionpedidolog_IDEmpresa_Estado",
                table: "migracionpedidolog",
                columns: new[] { "IDEmpresa", "Estado" });

            migrationBuilder.CreateIndex(
                name: "IX_migracionpedidolog_IDEmpresa_IDPedidoMovil_FechaIntento",
                table: "migracionpedidolog",
                columns: new[] { "IDEmpresa", "IDPedidoMovil", "FechaIntento" });

            migrationBuilder.CreateIndex(
                name: "IX_synccursor_IDEmpresa_EntityKey",
                table: "synccursor",
                columns: new[] { "IDEmpresa", "EntityKey" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_chatsoportemensajeecommerce_chatconversacionecommerce_IDConversacion",
                table: "chatsoportemensajeecommerce",
                column: "IDConversacion",
                principalTable: "chatconversacionecommerce",
                principalColumn: "ID");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_chatsoportemensajeecommerce_chatconversacionecommerce_IDConversacion",
                table: "chatsoportemensajeecommerce");

            migrationBuilder.DropIndex(
                name: "IX_chatsoportemensajeecommerce_IDEmpresa_IDConversacion_FechaMensaje",
                table: "chatsoportemensajeecommerce");

            migrationBuilder.DropIndex(
                name: "IX_chatsoportemensajeecommerce_IDConversacion",
                table: "chatsoportemensajeecommerce");

            migrationBuilder.DropColumn(name: "IDConversacion", table: "chatsoportemensajeecommerce");
            migrationBuilder.DropColumn(name: "IDAdminUsuario", table: "chatsoportemensajeecommerce");
            migrationBuilder.DropColumn(name: "TipoMensaje", table: "chatsoportemensajeecommerce");
            migrationBuilder.DropColumn(name: "UrlAdjunto", table: "chatsoportemensajeecommerce");
            migrationBuilder.DropColumn(name: "MimeType", table: "chatsoportemensajeecommerce");
            migrationBuilder.DropColumn(name: "NombreArchivo", table: "chatsoportemensajeecommerce");

            migrationBuilder.DropTable(name: "chatconversacionecommerce");
            migrationBuilder.DropTable(name: "migracionpedidolog");
            migrationBuilder.DropTable(name: "synccursor");
        }
    }
}
