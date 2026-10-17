using Datamex.ECommerce.Application.Abstractions;
using Datamex.ECommerce.Application.Notifications;
using Datamex.ECommerce.Application.Options;
using Datamex.ECommerce.Application.Payments;
using Datamex.ECommerce.Application.Sync;
using Datamex.ECommerce.Domain.Entities;
using Datamex.ECommerce.Domain.Enums;
using Datamex.ECommerce.Infrastructure.Inventory;
using Datamex.ECommerce.Infrastructure.Notifications;
using Datamex.ECommerce.Infrastructure.Payments;
using Datamex.ECommerce.Infrastructure.Persistence;
using Datamex.ECommerce.Infrastructure.Tenancy;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace Datamex.ECommerce.Application.Tests.Payments;

public class PaymentServiceBancardTests
{
    [Fact]
    public async Task ConfirmWebhook_Ok_CierraCarrito_Y_BajaStock_Standalone()
    {
        await using var fx = await Fixture.CreateAsync();
        var stockAntes = await fx.Db.Stocks.AsNoTracking().SingleAsync(s => s.IDArticulo == fx.ArticuloId);

        var result = await fx.Payments.ConfirmWebhookAsync(OkPayload(fx.ShopProcessId, fx.Monto), default);

        Assert.Equal(EstadoCarrito.Cerrado, result.EstadoCarrito);
        Assert.False(result.RequierePagoOnline);

        var stock = await fx.Db.Stocks.AsNoTracking().SingleAsync(s => s.IDArticulo == fx.ArticuloId);
        Assert.Equal(stockAntes.TotalExistencia - 2, stock.TotalExistencia);
        Assert.Equal(stockAntes.TotalPedidoVenta + 2, stock.TotalPedidoVenta);

        var logs = await fx.Db.LogsPagoTarjeta.Where(l => l.ShopProcessId == fx.ShopProcessId).ToListAsync();
        Assert.Contains(logs, l => l.Status == "success");
    }

    [Fact]
    public async Task ConfirmWebhook_Duplicado_NoVuelveABajarStock()
    {
        await using var fx = await Fixture.CreateAsync();
        await fx.Payments.ConfirmWebhookAsync(OkPayload(fx.ShopProcessId, fx.Monto), default);
        var stockTrasPrimero = await fx.Db.Stocks.AsNoTracking().SingleAsync(s => s.IDArticulo == fx.ArticuloId);

        var second = await fx.Payments.ConfirmWebhookAsync(OkPayload(fx.ShopProcessId, fx.Monto), default);

        Assert.Contains("ya confirmado", second.Mensaje, StringComparison.OrdinalIgnoreCase);
        var stock = await fx.Db.Stocks.AsNoTracking().SingleAsync(s => s.IDArticulo == fx.ArticuloId);
        Assert.Equal(stockTrasPrimero.TotalExistencia, stock.TotalExistencia);
        Assert.Equal(1, await fx.Db.LogsPagoTarjeta.CountAsync(l => l.Status == "success" && l.ShopProcessId == fx.ShopProcessId));
    }

    [Fact]
    public async Task ConfirmWebhook_Rejected_DejaAbierto_Y_Loguea()
    {
        await using var fx = await Fixture.CreateAsync();
        var stockAntes = await fx.Db.Stocks.AsNoTracking().SingleAsync(s => s.IDArticulo == fx.ArticuloId);

        var payload = new BancardWebhookPayload(
            fx.ShopProcessId,
            Amt(fx.Monto),
            "0.00",
            "PYG",
            [new BancardConfirmItem("05", null, null, Amt(fx.Monto), "PYG")],
            "rejected");

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => fx.Payments.ConfirmWebhookAsync(payload, default));
        Assert.Contains("rechazó", ex.Message, StringComparison.OrdinalIgnoreCase);

        var cart = await fx.Db.CarritosCompra.AsNoTracking().SingleAsync(c => c.ID == fx.CartId);
        Assert.Equal(EstadoCarrito.Abierto, cart.Estado);

        var stock = await fx.Db.Stocks.AsNoTracking().SingleAsync(s => s.IDArticulo == fx.ArticuloId);
        Assert.Equal(stockAntes.TotalExistencia, stock.TotalExistencia);
        Assert.Contains(await fx.Db.LogsPagoTarjeta.ToListAsync(), l => l.Status == "rejected");
    }

    [Fact]
    public async Task Rollback_TrasPago_RestauraStock_Standalone()
    {
        await using var fx = await Fixture.CreateAsync();
        var stockInicial = await fx.Db.Stocks.AsNoTracking().SingleAsync(s => s.IDArticulo == fx.ArticuloId);
        await fx.Payments.ConfirmWebhookAsync(OkPayload(fx.ShopProcessId, fx.Monto), default);

        await fx.Payments.RollbackAsync(fx.ShopProcessId, default);

        var cart = await fx.Db.CarritosCompra.AsNoTracking().SingleAsync(c => c.ID == fx.CartId);
        Assert.Equal(EstadoCarrito.Anulado, cart.Estado);

        var stock = await fx.Db.Stocks.AsNoTracking().SingleAsync(s => s.IDArticulo == fx.ArticuloId);
        Assert.Equal(stockInicial.TotalExistencia, stock.TotalExistencia);
        Assert.Equal(stockInicial.TotalPedidoVenta, stock.TotalPedidoVenta);
    }

    [Fact]
    public async Task Rollback_SinPago_NoTocaStock()
    {
        await using var fx = await Fixture.CreateAsync();
        var stockAntes = await fx.Db.Stocks.AsNoTracking().SingleAsync(s => s.IDArticulo == fx.ArticuloId);

        await fx.Payments.RollbackAsync(fx.ShopProcessId, default);

        var stock = await fx.Db.Stocks.AsNoTracking().SingleAsync(s => s.IDArticulo == fx.ArticuloId);
        Assert.Equal(stockAntes.TotalExistencia, stock.TotalExistencia);
        Assert.Equal(EstadoCarrito.Anulado,
            (await fx.Db.CarritosCompra.AsNoTracking().SingleAsync(c => c.ID == fx.CartId)).Estado);
    }

    private static string Amt(decimal monto) =>
        monto.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture);

    private static BancardWebhookPayload OkPayload(long shopProcessId, decimal monto) =>
        new(
            shopProcessId,
            Amt(monto),
            "0.00",
            "PYG",
            [new BancardConfirmItem("00", "AUTH", "00", Amt(monto), "PYG")],
            "ok");

    private sealed class Fixture : IAsyncDisposable
    {
        public required EcomDbContext Db { get; init; }
        public required PaymentService Payments { get; init; }
        public long CartId { get; init; }
        public long ArticuloId { get; init; }
        public long ShopProcessId { get; init; }
        public decimal Monto { get; init; }

        public static async Task<Fixture> CreateAsync()
        {
            var options = new DbContextOptionsBuilder<EcomDbContext>()
                .UseInMemoryDatabase($"bancard-{Guid.NewGuid():N}")
                .Options;
            var db = new EcomDbContext(options);

            const long idEmpresa = 9001;
            const long depositoId = 1;
            const long articuloId = 501;
            const long cuentaId = 77;
            const long shopProcessId = 88001;
            const decimal monto = 15000m;

            db.Empresas.Add(new Empresa { ID = idEmpresa, RazonSocial = "Test SA", Activo = true });
            db.EmpresaConfiguracionesEcommerce.Add(new EmpresaConfiguracionEcommerce
            {
                ID = idEmpresa,
                Activo = true,
                ControlaStockEnCarrito = true,
                ModoIntegracion = (int)IntegrationMode.Standalone,
                ProcesaPedidosERP = true,
                IDDeposito = depositoId,
                ClavePublicaProcesadoraTarjeta = "pub",
                ClavePrivadaProcesadoraTarjeta = "priv"
            });
            db.Articulos.Add(new Articulo
            {
                ID = articuloId,
                IDEmpresa = idEmpresa,
                Activo = true,
                DescripcionArticulo = "Test art",
                SKU = "T-1",
                ActivoEcommerce = true
            });
            db.Cuentas.Add(new Cuenta
            {
                ID = cuentaId,
                IDEmpresa = idEmpresa,
                Activo = true,
                NroCuenta = 77,
                RazonSocial = "Cliente Test"
            });
            db.Stocks.Add(new Stock
            {
                IDEmpresa = idEmpresa,
                IDArticulo = articuloId,
                IDDeposito = depositoId,
                Activo = true,
                TotalExistencia = 10,
                TotalPedidoVenta = 0
            });

            var cart = new CarritoCompra
            {
                IDEmpresa = idEmpresa,
                Activo = true,
                NroPedidoCarritoCompra = 1001,
                IDCuenta = cuentaId,
                IDListaPrecio = 1,
                IDDeposito = depositoId,
                IDSesion = "sess-1",
                IDProcesoNegocio = shopProcessId,
                Estado = EstadoCarrito.Abierto,
                TipoPago = TipoPagoEcommerce.Pedido,
                TipoEntrega = TipoEntregaEcommerce.Pickup,
                MontoTotal = monto,
                MontoCambio = 1,
                RazonSocial = "Cliente Test",
                NumeroDocumento = "123",
                FechaPedido = DateTime.UtcNow,
                FechaPrevistaEntrega = DateTime.UtcNow.AddDays(1)
            };
            cart.DetalleCarritoCompras.Add(new DetalleCarritoCompra
            {
                IDEmpresa = idEmpresa,
                Activo = true,
                IDArticulo = articuloId,
                Cantidad = 2,
                CantidadEmbalada = 2,
                PrecioNetoConImpuesto = 7500,
                PrecioNetoSinImpuesto = 7500,
                PrecioBrutoConImpuesto = 7500,
                PrecioBrutoSinImpuesto = 7500,
                MontoTotal = monto
            });
            db.CarritosCompra.Add(cart);
            await db.SaveChangesAsync();

            var tenant = new TenantContext();
            tenant.Set(idEmpresa, "test");
            var inventory = new InventoryService(db, tenant);
            var bancardOpts = Microsoft.Extensions.Options.Options.Create(new BancardOptions { Mode = "Stub" });
            var ecomOpts = Microsoft.Extensions.Options.Options.Create(new ECommerceOptions { IntegrationMode = IntegrationMode.Standalone });
            var pentahoOpts = Microsoft.Extensions.Options.Options.Create(new PentahoOptions { Enabled = false });
            var bancard = new StubBancardClient(bancardOpts, NullLogger<StubBancardClient>.Instance);
            IEmailSender email = new LoggingEmailSender(NullLogger<LoggingEmailSender>.Instance);
            IPedidoErpWriteBackDispatcher writeBack = new NoOpWriteBack();

            var payments = new PaymentService(
                db, tenant, bancard, email, inventory, ecomOpts, bancardOpts, writeBack, pentahoOpts,
                NullLogger<PaymentService>.Instance);

            return new Fixture
            {
                Db = db,
                Payments = payments,
                CartId = cart.ID,
                ArticuloId = articuloId,
                ShopProcessId = shopProcessId,
                Monto = monto
            };
        }

        public async ValueTask DisposeAsync() => await Db.DisposeAsync();
    }

    private sealed class NoOpWriteBack : IPedidoErpWriteBackDispatcher
    {
        public void SchedulePedido(long idEmpresa, long idPedidoMovil) { }
    }
}
