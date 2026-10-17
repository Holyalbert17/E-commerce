using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Datamex.ECommerce.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class CheckoutPedidoMovil : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "pedidomovil",
                columns: table => new
                {
                    ID = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    NroPedido = table.Column<long>(type: "INTEGER", nullable: false),
                    NroFactura = table.Column<long>(type: "INTEGER", nullable: true),
                    IDCuenta = table.Column<long>(type: "INTEGER", nullable: false),
                    IDVendedor = table.Column<long>(type: "INTEGER", nullable: false),
                    IDDeposito = table.Column<long>(type: "INTEGER", nullable: false),
                    IndiceComision = table.Column<double>(type: "REAL", nullable: false),
                    IDCotizacionComercial = table.Column<long>(type: "INTEGER", nullable: false),
                    IDCondicionVenta = table.Column<long>(type: "INTEGER", nullable: false),
                    IDListaPrecioCuenta = table.Column<long>(type: "INTEGER", nullable: true),
                    IDUsuario = table.Column<long>(type: "INTEGER", nullable: false),
                    FechaPedido = table.Column<DateTime>(type: "TEXT", nullable: false),
                    MontoCambio = table.Column<decimal>(type: "TEXT", nullable: false),
                    FechaEmision = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Plazo = table.Column<int>(type: "INTEGER", nullable: false),
                    PorcentajeDescuento = table.Column<double>(type: "REAL", nullable: false),
                    TipoFactura = table.Column<int>(type: "INTEGER", nullable: false),
                    TipoPedido = table.Column<int>(type: "INTEGER", nullable: true),
                    TipoPrecio = table.Column<int>(type: "INTEGER", nullable: false),
                    FechaPrevistaEntrega = table.Column<DateTime>(type: "TEXT", nullable: true),
                    Estado = table.Column<int>(type: "INTEGER", nullable: false),
                    NroPedidoDispositivoMovil = table.Column<long>(type: "INTEGER", nullable: false),
                    Migrado = table.Column<bool>(type: "INTEGER", nullable: false),
                    Momento = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Host = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    Observacion = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    Latitud = table.Column<decimal>(type: "TEXT", nullable: true),
                    Longitud = table.Column<decimal>(type: "TEXT", nullable: true),
                    Precision = table.Column<decimal>(type: "TEXT", nullable: true),
                    NroOrdenCompra = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    OrigenPedido = table.Column<int>(type: "INTEGER", nullable: false),
                    MotivoPendiente = table.Column<string>(type: "TEXT", maxLength: 512, nullable: true),
                    Activo = table.Column<bool>(type: "INTEGER", nullable: false),
                    UltModificacion = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UserName = table.Column<string>(type: "TEXT", nullable: true),
                    IDEmpresa = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_pedidomovil", x => x.ID);
                });

            migrationBuilder.CreateTable(
                name: "detallepedidomovil",
                columns: table => new
                {
                    ID = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    IDPedidoMovil = table.Column<long>(type: "INTEGER", nullable: false),
                    IDArticulo = table.Column<long>(type: "INTEGER", nullable: false),
                    IDDetalleListaPrecio = table.Column<long>(type: "INTEGER", nullable: false),
                    IDUsuarioTraspaso = table.Column<long>(type: "INTEGER", nullable: false),
                    IDUsuario = table.Column<long>(type: "INTEGER", nullable: false),
                    CodigoBarra = table.Column<string>(type: "TEXT", maxLength: 20, nullable: true),
                    MontoCambio = table.Column<double>(type: "REAL", nullable: false),
                    PorcentajeDescuento = table.Column<double>(type: "REAL", nullable: false),
                    PrecioBrutoConImpuesto = table.Column<double>(type: "REAL", nullable: false),
                    PrecioBrutoSinImpuesto = table.Column<double>(type: "REAL", nullable: false),
                    PrecioNetoConImpuesto = table.Column<double>(type: "REAL", nullable: false),
                    PrecioNetoSinImpuesto = table.Column<double>(type: "REAL", nullable: false),
                    CantidadEmbalada = table.Column<int>(type: "INTEGER", nullable: false),
                    CantidadFraccionada = table.Column<int>(type: "INTEGER", nullable: false),
                    Traspaso = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Momento = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Host = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    Observacion = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    NroOrden = table.Column<int>(type: "INTEGER", nullable: true),
                    DiferenciaPrecioSupermercado = table.Column<bool>(type: "INTEGER", nullable: false),
                    PrecioLista = table.Column<decimal>(type: "TEXT", nullable: true),
                    Activo = table.Column<bool>(type: "INTEGER", nullable: false),
                    UltModificacion = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UserName = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_detallepedidomovil", x => x.ID);
                    table.ForeignKey(
                        name: "FK_detallepedidomovil_articulo_IDArticulo",
                        column: x => x.IDArticulo,
                        principalTable: "articulo",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_detallepedidomovil_pedidomovil_IDPedidoMovil",
                        column: x => x.IDPedidoMovil,
                        principalTable: "pedidomovil",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_carritocompra_IDPedidoMovil",
                table: "carritocompra",
                column: "IDPedidoMovil");

            migrationBuilder.CreateIndex(
                name: "IX_detallepedidomovil_IDArticulo",
                table: "detallepedidomovil",
                column: "IDArticulo");

            migrationBuilder.CreateIndex(
                name: "IX_detallepedidomovil_IDPedidoMovil",
                table: "detallepedidomovil",
                column: "IDPedidoMovil");

            migrationBuilder.CreateIndex(
                name: "IX_pedidomovil_IDEmpresa_Migrado",
                table: "pedidomovil",
                columns: new[] { "IDEmpresa", "Migrado" });

            migrationBuilder.CreateIndex(
                name: "IX_pedidomovil_IDEmpresa_NroPedido_OrigenPedido",
                table: "pedidomovil",
                columns: new[] { "IDEmpresa", "NroPedido", "OrigenPedido" });

            migrationBuilder.AddForeignKey(
                name: "FK_carritocompra_pedidomovil_IDPedidoMovil",
                table: "carritocompra",
                column: "IDPedidoMovil",
                principalTable: "pedidomovil",
                principalColumn: "ID",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_carritocompra_pedidomovil_IDPedidoMovil",
                table: "carritocompra");

            migrationBuilder.DropTable(
                name: "detallepedidomovil");

            migrationBuilder.DropTable(
                name: "pedidomovil");

            migrationBuilder.DropIndex(
                name: "IX_carritocompra_IDPedidoMovil",
                table: "carritocompra");
        }
    }
}
