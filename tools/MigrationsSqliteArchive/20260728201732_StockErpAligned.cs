using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Datamex.ECommerce.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class StockErpAligned : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "existenciaarticulodeposito");

            migrationBuilder.AddColumn<bool>(
                name: "DepositoDelCliente",
                table: "deposito",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "FacturacionMoviles",
                table: "deposito",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<long>(
                name: "IDTipoDeposito",
                table: "deposito",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.CreateTable(
                name: "stock",
                columns: table => new
                {
                    ID = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    IDDeposito = table.Column<long>(type: "INTEGER", nullable: false),
                    IDArticulo = table.Column<long>(type: "INTEGER", nullable: false),
                    IDContenedor = table.Column<long>(type: "INTEGER", nullable: true),
                    IDLoteArticulo = table.Column<long>(type: "INTEGER", nullable: true),
                    CostoGestionUltimo = table.Column<double>(type: "REAL", nullable: false),
                    CostoGestionPromedio = table.Column<double>(type: "REAL", nullable: false),
                    CostoGestionFIFO = table.Column<double>(type: "REAL", nullable: false),
                    CostoGestionLIFO = table.Column<double>(type: "REAL", nullable: false),
                    CostoGestionNIFO = table.Column<double>(type: "REAL", nullable: false),
                    CostoUltimo = table.Column<double>(type: "REAL", nullable: false),
                    CostoPromedio = table.Column<double>(type: "REAL", nullable: false),
                    CostoFIFO = table.Column<double>(type: "REAL", nullable: false),
                    CostoLIFO = table.Column<double>(type: "REAL", nullable: false),
                    CostoNIFO = table.Column<double>(type: "REAL", nullable: false),
                    PuntoReposicion = table.Column<double>(type: "REAL", nullable: false),
                    LoteReposicion = table.Column<double>(type: "REAL", nullable: false),
                    TotalCompra = table.Column<double>(type: "REAL", nullable: false),
                    TotalDevolucionCompra = table.Column<double>(type: "REAL", nullable: false),
                    TotalPedidoVenta = table.Column<double>(type: "REAL", nullable: false),
                    TotalVenta = table.Column<double>(type: "REAL", nullable: false),
                    TotalDevolucionVenta = table.Column<double>(type: "REAL", nullable: false),
                    TotalTransferenciaSalida = table.Column<double>(type: "REAL", nullable: false),
                    TotalTransferenciaEntrada = table.Column<double>(type: "REAL", nullable: false),
                    TotalIndustrializacionIngreso = table.Column<double>(type: "REAL", nullable: false),
                    TotalIndustrializacionEgreso = table.Column<double>(type: "REAL", nullable: false),
                    TotalAjustePositivo = table.Column<double>(type: "REAL", nullable: false),
                    TotalAjusteNegativo = table.Column<double>(type: "REAL", nullable: false),
                    TotalTransito = table.Column<double>(type: "REAL", nullable: false),
                    TotalExistencia = table.Column<double>(type: "REAL", nullable: false),
                    ExistenciaCongelada = table.Column<bool>(type: "INTEGER", nullable: false),
                    TotalExistenciaInicial = table.Column<double>(type: "REAL", nullable: false),
                    CostoUnitarioInicial = table.Column<decimal>(type: "TEXT", nullable: false),
                    TotalReserva = table.Column<decimal>(type: "TEXT", nullable: false),
                    Activo = table.Column<bool>(type: "INTEGER", nullable: false),
                    UltModificacion = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UserName = table.Column<string>(type: "TEXT", nullable: true),
                    IDEmpresa = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_stock", x => x.ID);
                    table.ForeignKey(
                        name: "FK_stock_articulo_IDArticulo",
                        column: x => x.IDArticulo,
                        principalTable: "articulo",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_stock_deposito_IDDeposito",
                        column: x => x.IDDeposito,
                        principalTable: "deposito",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "tipodeposito",
                columns: table => new
                {
                    ID = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    CodTipoDeposito = table.Column<int>(type: "INTEGER", nullable: false),
                    DescripcionTipoDeposito = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Normal = table.Column<bool>(type: "INTEGER", nullable: false),
                    Consolidado = table.Column<bool>(type: "INTEGER", nullable: false),
                    Facturable = table.Column<bool>(type: "INTEGER", nullable: false),
                    PermiteFacturarBajoCosto = table.Column<bool>(type: "INTEGER", nullable: false),
                    Oferta = table.Column<bool>(type: "INTEGER", nullable: false),
                    Remate = table.Column<bool>(type: "INTEGER", nullable: false),
                    PuntoVenta = table.Column<bool>(type: "INTEGER", nullable: false),
                    Activo = table.Column<bool>(type: "INTEGER", nullable: false),
                    UltModificacion = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UserName = table.Column<string>(type: "TEXT", nullable: true),
                    IDEmpresa = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tipodeposito", x => x.ID);
                });

            migrationBuilder.CreateIndex(
                name: "IX_deposito_IDTipoDeposito",
                table: "deposito",
                column: "IDTipoDeposito");

            migrationBuilder.CreateIndex(
                name: "IX_stock_ExistenciaCongelada",
                table: "stock",
                column: "ExistenciaCongelada");

            migrationBuilder.CreateIndex(
                name: "IX_stock_IDArticulo",
                table: "stock",
                column: "IDArticulo");

            migrationBuilder.CreateIndex(
                name: "IX_stock_IDDeposito",
                table: "stock",
                column: "IDDeposito");

            migrationBuilder.CreateIndex(
                name: "IX_stock_IDEmpresa_IDDeposito_IDArticulo_IDContenedor_IDLoteArticulo",
                table: "stock",
                columns: new[] { "IDEmpresa", "IDDeposito", "IDArticulo", "IDContenedor", "IDLoteArticulo" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_stock_TotalExistencia",
                table: "stock",
                column: "TotalExistencia");

            migrationBuilder.CreateIndex(
                name: "IX_tipodeposito_IDEmpresa_CodTipoDeposito",
                table: "tipodeposito",
                columns: new[] { "IDEmpresa", "CodTipoDeposito" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_deposito_tipodeposito_IDTipoDeposito",
                table: "deposito",
                column: "IDTipoDeposito",
                principalTable: "tipodeposito",
                principalColumn: "ID",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_deposito_tipodeposito_IDTipoDeposito",
                table: "deposito");

            migrationBuilder.DropTable(
                name: "stock");

            migrationBuilder.DropTable(
                name: "tipodeposito");

            migrationBuilder.DropIndex(
                name: "IX_deposito_IDTipoDeposito",
                table: "deposito");

            migrationBuilder.DropColumn(
                name: "DepositoDelCliente",
                table: "deposito");

            migrationBuilder.DropColumn(
                name: "FacturacionMoviles",
                table: "deposito");

            migrationBuilder.DropColumn(
                name: "IDTipoDeposito",
                table: "deposito");

            migrationBuilder.CreateTable(
                name: "existenciaarticulodeposito",
                columns: table => new
                {
                    ID = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    IDArticulo = table.Column<long>(type: "INTEGER", nullable: false),
                    IDDeposito = table.Column<long>(type: "INTEGER", nullable: false),
                    Activo = table.Column<bool>(type: "INTEGER", nullable: false),
                    CantidadDisponible = table.Column<decimal>(type: "TEXT", nullable: false),
                    CantidadReservada = table.Column<decimal>(type: "TEXT", nullable: false),
                    IDEmpresa = table.Column<long>(type: "INTEGER", nullable: false),
                    UltModificacion = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UserName = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_existenciaarticulodeposito", x => x.ID);
                    table.ForeignKey(
                        name: "FK_existenciaarticulodeposito_articulo_IDArticulo",
                        column: x => x.IDArticulo,
                        principalTable: "articulo",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_existenciaarticulodeposito_deposito_IDDeposito",
                        column: x => x.IDDeposito,
                        principalTable: "deposito",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_existenciaarticulodeposito_IDArticulo",
                table: "existenciaarticulodeposito",
                column: "IDArticulo");

            migrationBuilder.CreateIndex(
                name: "IX_existenciaarticulodeposito_IDDeposito",
                table: "existenciaarticulodeposito",
                column: "IDDeposito");

            migrationBuilder.CreateIndex(
                name: "IX_existenciaarticulodeposito_IDEmpresa_IDArticulo_IDDeposito",
                table: "existenciaarticulodeposito",
                columns: new[] { "IDEmpresa", "IDArticulo", "IDDeposito" },
                unique: true);
        }
    }
}
