using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Datamex.ECommerce.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class DefiComplianceCms : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "SlugSeo",
                table: "articulo",
                type: "TEXT",
                maxLength: 180,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "adminauditorialog",
                columns: table => new
                {
                    ID = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    IDAdminUsuario = table.Column<long>(type: "INTEGER", nullable: true),
                    Usuario = table.Column<string>(type: "TEXT", maxLength: 120, nullable: false),
                    Accion = table.Column<string>(type: "TEXT", maxLength: 80, nullable: false),
                    Entidad = table.Column<string>(type: "TEXT", maxLength: 80, nullable: false),
                    EntidadId = table.Column<string>(type: "TEXT", maxLength: 80, nullable: true),
                    Detalle = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true),
                    Fecha = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Ip = table.Column<string>(type: "TEXT", maxLength: 64, nullable: true),
                    Activo = table.Column<bool>(type: "INTEGER", nullable: false),
                    UltModificacion = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UserName = table.Column<string>(type: "TEXT", nullable: true),
                    IDEmpresa = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_adminauditorialog", x => x.ID);
                });

            migrationBuilder.CreateTable(
                name: "contenidobloque",
                columns: table => new
                {
                    ID = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    PageCode = table.Column<string>(type: "TEXT", maxLength: 80, nullable: false),
                    PageZone = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    BlockType = table.Column<string>(type: "TEXT", maxLength: 60, nullable: false),
                    InternalName = table.Column<string>(type: "TEXT", maxLength: 150, nullable: false),
                    Title = table.Column<string>(type: "TEXT", maxLength: 250, nullable: true),
                    Subtitle = table.Column<string>(type: "TEXT", maxLength: 250, nullable: true),
                    Description = table.Column<string>(type: "TEXT", nullable: true),
                    LayoutVariant = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    DisplayOrder = table.Column<int>(type: "INTEGER", nullable: false),
                    ContentStatus = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    IsReadOnlyWhenIntegrated = table.Column<bool>(type: "INTEGER", nullable: false),
                    StartDate = table.Column<DateTime>(type: "TEXT", nullable: true),
                    EndDate = table.Column<DateTime>(type: "TEXT", nullable: true),
                    SettingsJson = table.Column<string>(type: "TEXT", nullable: true),
                    PublishedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    UpdatedByAdminUserId = table.Column<long>(type: "INTEGER", nullable: true),
                    FechaCreacion = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Activo = table.Column<bool>(type: "INTEGER", nullable: false),
                    UltModificacion = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UserName = table.Column<string>(type: "TEXT", nullable: true),
                    IDEmpresa = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_contenidobloque", x => x.ID);
                });

            migrationBuilder.CreateTable(
                name: "inventariomovimiento",
                columns: table => new
                {
                    ID = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    IDArticulo = table.Column<long>(type: "INTEGER", nullable: false),
                    IDEmbalaje = table.Column<long>(type: "INTEGER", nullable: true),
                    IDDeposito = table.Column<long>(type: "INTEGER", nullable: true),
                    IDPedidoMovil = table.Column<long>(type: "INTEGER", nullable: true),
                    TipoMovimiento = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false),
                    Cantidad = table.Column<decimal>(type: "TEXT", precision: 18, scale: 4, nullable: false),
                    ExistenciaAntes = table.Column<decimal>(type: "TEXT", precision: 18, scale: 4, nullable: false),
                    ExistenciaDespues = table.Column<decimal>(type: "TEXT", precision: 18, scale: 4, nullable: false),
                    Referencia = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    Fecha = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Activo = table.Column<bool>(type: "INTEGER", nullable: false),
                    UltModificacion = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UserName = table.Column<string>(type: "TEXT", nullable: true),
                    IDEmpresa = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_inventariomovimiento", x => x.ID);
                });

            migrationBuilder.CreateTable(
                name: "navegacionmenu",
                columns: table => new
                {
                    ID = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Code = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 150, nullable: false),
                    MenuType = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    PageCode = table.Column<string>(type: "TEXT", maxLength: 80, nullable: true),
                    PageZone = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    DisplayOrder = table.Column<int>(type: "INTEGER", nullable: false),
                    ContentStatus = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    SettingsJson = table.Column<string>(type: "TEXT", nullable: true),
                    Activo = table.Column<bool>(type: "INTEGER", nullable: false),
                    UltModificacion = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UserName = table.Column<string>(type: "TEXT", nullable: true),
                    IDEmpresa = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_navegacionmenu", x => x.ID);
                });

            migrationBuilder.CreateTable(
                name: "contenidobloqueitem",
                columns: table => new
                {
                    ID = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    IDContenidoBloque = table.Column<long>(type: "INTEGER", nullable: false),
                    ParentItemId = table.Column<long>(type: "INTEGER", nullable: true),
                    ItemType = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    Title = table.Column<string>(type: "TEXT", maxLength: 250, nullable: true),
                    Subtitle = table.Column<string>(type: "TEXT", maxLength: 250, nullable: true),
                    Description = table.Column<string>(type: "TEXT", nullable: true),
                    ImageUrl = table.Column<string>(type: "TEXT", maxLength: 800, nullable: true),
                    MobileImageUrl = table.Column<string>(type: "TEXT", maxLength: 800, nullable: true),
                    IconUrl = table.Column<string>(type: "TEXT", nullable: true),
                    LinkUrl = table.Column<string>(type: "TEXT", maxLength: 800, nullable: true),
                    LinkTarget = table.Column<string>(type: "TEXT", maxLength: 20, nullable: true),
                    BadgeText = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    BadgeType = table.Column<string>(type: "TEXT", nullable: true),
                    PriceText = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    CountdownEndAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    DisplayOrder = table.Column<int>(type: "INTEGER", nullable: false),
                    ContentStatus = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    StartDate = table.Column<DateTime>(type: "TEXT", nullable: true),
                    EndDate = table.Column<DateTime>(type: "TEXT", nullable: true),
                    SettingsJson = table.Column<string>(type: "TEXT", nullable: true),
                    Activo = table.Column<bool>(type: "INTEGER", nullable: false),
                    UltModificacion = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UserName = table.Column<string>(type: "TEXT", nullable: true),
                    IDEmpresa = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_contenidobloqueitem", x => x.ID);
                    table.ForeignKey(
                        name: "FK_contenidobloqueitem_contenidobloque_IDContenidoBloque",
                        column: x => x.IDContenidoBloque,
                        principalTable: "contenidobloque",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "navegacionmenuitem",
                columns: table => new
                {
                    ID = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    IDNavegacionMenu = table.Column<long>(type: "INTEGER", nullable: false),
                    ParentItemId = table.Column<long>(type: "INTEGER", nullable: true),
                    IDGrupoArticulo = table.Column<long>(type: "INTEGER", nullable: true),
                    ItemType = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    Label = table.Column<string>(type: "TEXT", maxLength: 150, nullable: false),
                    Url = table.Column<string>(type: "TEXT", maxLength: 800, nullable: true),
                    Target = table.Column<string>(type: "TEXT", maxLength: 20, nullable: true),
                    IconUrl = table.Column<string>(type: "TEXT", nullable: true),
                    BadgeText = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    DisplayOrder = table.Column<int>(type: "INTEGER", nullable: false),
                    ContentStatus = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    Activo = table.Column<bool>(type: "INTEGER", nullable: false),
                    UltModificacion = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UserName = table.Column<string>(type: "TEXT", nullable: true),
                    IDEmpresa = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_navegacionmenuitem", x => x.ID);
                    table.ForeignKey(
                        name: "FK_navegacionmenuitem_navegacionmenu_IDNavegacionMenu",
                        column: x => x.IDNavegacionMenu,
                        principalTable: "navegacionmenu",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_articulo_IDEmpresa_SlugSeo",
                table: "articulo",
                columns: new[] { "IDEmpresa", "SlugSeo" });

            migrationBuilder.CreateIndex(
                name: "IX_adminauditorialog_IDEmpresa_Fecha",
                table: "adminauditorialog",
                columns: new[] { "IDEmpresa", "Fecha" });

            migrationBuilder.CreateIndex(
                name: "IX_contenidobloque_IDEmpresa_BlockType",
                table: "contenidobloque",
                columns: new[] { "IDEmpresa", "BlockType" });

            migrationBuilder.CreateIndex(
                name: "IX_contenidobloque_IDEmpresa_PageCode_DisplayOrder",
                table: "contenidobloque",
                columns: new[] { "IDEmpresa", "PageCode", "DisplayOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_contenidobloqueitem_IDContenidoBloque_DisplayOrder",
                table: "contenidobloqueitem",
                columns: new[] { "IDContenidoBloque", "DisplayOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_inventariomovimiento_IDEmpresa_IDArticulo_Fecha",
                table: "inventariomovimiento",
                columns: new[] { "IDEmpresa", "IDArticulo", "Fecha" });

            migrationBuilder.CreateIndex(
                name: "IX_navegacionmenu_IDEmpresa_Code",
                table: "navegacionmenu",
                columns: new[] { "IDEmpresa", "Code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_navegacionmenuitem_IDNavegacionMenu_DisplayOrder",
                table: "navegacionmenuitem",
                columns: new[] { "IDNavegacionMenu", "DisplayOrder" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "adminauditorialog");

            migrationBuilder.DropTable(
                name: "contenidobloqueitem");

            migrationBuilder.DropTable(
                name: "inventariomovimiento");

            migrationBuilder.DropTable(
                name: "navegacionmenuitem");

            migrationBuilder.DropTable(
                name: "contenidobloque");

            migrationBuilder.DropTable(
                name: "navegacionmenu");

            migrationBuilder.DropIndex(
                name: "IX_articulo_IDEmpresa_SlugSeo",
                table: "articulo");

            migrationBuilder.DropColumn(
                name: "SlugSeo",
                table: "articulo");
        }
    }
}
