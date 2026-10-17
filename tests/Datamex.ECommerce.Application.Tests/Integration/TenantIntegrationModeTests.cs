using Datamex.ECommerce.Application.Integration;
using Datamex.ECommerce.Domain.Entities;
using Datamex.ECommerce.Domain.Enums;
using Xunit;

namespace Datamex.ECommerce.Application.Tests.Integration;

public class TenantIntegrationModeTests
{
    private static EmpresaConfiguracionEcommerce Cfg(IntegrationMode mode, bool controlaStock = true) =>
        new()
        {
            ModoIntegracion = (int)mode,
            ControlaStockEnCarrito = controlaStock,
            ProcesaPedidosERP = true,
        };

    [Fact]
    public void Standalone_with_stock_control_mutates_local_stock()
    {
        var cfg = Cfg(IntegrationMode.Standalone, controlaStock: true);
        Assert.True(TenantIntegrationMode.MutatesLocalStock(cfg, IntegrationMode.Integrated));
    }

    [Fact]
    public void Integrated_never_mutates_local_stock_even_if_flag_true()
    {
        var cfg = Cfg(IntegrationMode.Integrated, controlaStock: true);
        Assert.False(TenantIntegrationMode.MutatesLocalStock(cfg, IntegrationMode.Standalone));
    }

    [Fact]
    public void External_does_not_mutate_local_stock()
    {
        var cfg = Cfg(IntegrationMode.External, controlaStock: true);
        Assert.False(TenantIntegrationMode.MutatesLocalStock(cfg, IntegrationMode.Standalone));
    }

    [Fact]
    public void Resolve_prefers_tenant_over_global_fallback()
    {
        var cfg = Cfg(IntegrationMode.Integrated);
        Assert.Equal(IntegrationMode.Integrated,
            TenantIntegrationMode.Resolve(cfg, IntegrationMode.Standalone));
    }

    [Fact]
    public void CreatesLocalPedidoMovil_when_procesa_erp_or_standalone()
    {
        var integrated = Cfg(IntegrationMode.Integrated);
        integrated.ProcesaPedidosERP = true;
        Assert.True(TenantIntegrationMode.CreatesLocalPedidoMovil(integrated, IntegrationMode.Standalone));

        var standalone = Cfg(IntegrationMode.Standalone);
        standalone.ProcesaPedidosERP = false;
        Assert.True(TenantIntegrationMode.CreatesLocalPedidoMovil(standalone, IntegrationMode.Integrated));
    }

    [Theory]
    [InlineData("Integrated", true)]
    [InlineData("standalone", true)]
    [InlineData("nope", false)]
    public void TryParse_modes(string raw, bool ok)
    {
        Assert.Equal(ok, TenantIntegrationMode.TryParse(raw, out _));
    }
}
