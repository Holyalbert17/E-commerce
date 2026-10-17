using Datamex.ECommerce.Application.Abstractions;
using Datamex.ECommerce.Application.Sync;
using Datamex.ECommerce.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Datamex.ECommerce.Infrastructure.Sync;

/// <summary>
/// Write-back PedidoMovil → ERP. Flush síncrono (tiempo real) + Schedule como reintento.
/// Respeta <c>MigrarPedidosAlGenerar</c> por tenant y gates globales AllowWriteBack/AutoWriteBack.
/// </summary>
public sealed class PedidoErpWriteBackDispatcher : IPedidoErpWriteBackDispatcher
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IConfiguration _config;
    private readonly ILogger<PedidoErpWriteBackDispatcher> _logger;

    public PedidoErpWriteBackDispatcher(
        IServiceScopeFactory scopeFactory,
        IConfiguration config,
        ILogger<PedidoErpWriteBackDispatcher> logger)
    {
        _scopeFactory = scopeFactory;
        _config = config;
        _logger = logger;
    }

    public async Task<MigrarPedidosResult?> FlushPedidoAsync(
        long idEmpresa, long idPedidoMovil, CancellationToken ct = default)
    {
        if (!_config.GetValue("Erp:AutoWriteBack", true))
        {
            _logger.LogDebug(
                "AutoWriteBack=false: skip flush pedido {Pedido} empresa {Empresa}",
                idPedidoMovil, idEmpresa);
            return null;
        }

        await using var scope = _scopeFactory.CreateAsyncScope();
        var erpCs = scope.ServiceProvider.GetRequiredService<IErpConnectionResolver>();
        var cs = await erpCs.ResolveAsync(idEmpresa, ct);
        var allow = await erpCs.AllowWriteBackAsync(idEmpresa, ct);
        if (string.IsNullOrWhiteSpace(cs) || !allow)
        {
            _logger.LogInformation(
                "Write-back no disponible (CS/AllowWriteBack): pedido {Pedido} queda pendiente.",
                idPedidoMovil);
            return null;
        }

        var db = scope.ServiceProvider.GetRequiredService<EcomDbContext>();
        var cfg = await db.EmpresaConfiguracionesEcommerce.AsNoTracking()
            .FirstOrDefaultAsync(c => c.ID == idEmpresa, ct);
        if (cfg is null || !cfg.MigrarPedidosAlGenerar)
        {
            _logger.LogInformation(
                "MigrarPedidosAlGenerar=false: skip flush pedido {Pedido} empresa {Empresa}",
                idPedidoMovil, idEmpresa);
            return null;
        }

        var tenant = scope.ServiceProvider.GetRequiredService<ITenantContext>();
        tenant.Set(idEmpresa);
        var migration = scope.ServiceProvider.GetRequiredService<IPedidoErpMigrationService>();
        var result = await migration.MigrarAsync(new[] { idPedidoMovil }, ct);
        _logger.LogInformation(
            "Write-back sync pedido={Pedido} empresa={Empresa} ok={Ok} err={Err}",
            idPedidoMovil, idEmpresa, result.Exitosos, result.Errores);

        if (result.Errores > 0)
            SchedulePedido(idEmpresa, idPedidoMovil);

        return result;
    }

    public void SchedulePedido(long idEmpresa, long idPedidoMovil)
    {
        if (!_config.GetValue("Erp:AutoWriteBack", true))
        {
            _logger.LogDebug(
                "AutoWriteBack=false: no se encola pedido {Pedido} empresa {Empresa}",
                idPedidoMovil, idEmpresa);
            return;
        }

        _ = Task.Run(async () =>
        {
            try
            {
                await using var scope = _scopeFactory.CreateAsyncScope();
                var db = scope.ServiceProvider.GetRequiredService<EcomDbContext>();
                var cfg = await db.EmpresaConfiguracionesEcommerce.AsNoTracking()
                    .FirstOrDefaultAsync(c => c.ID == idEmpresa);
                if (cfg is null || !cfg.MigrarPedidosAlGenerar)
                    return;

                var tenant = scope.ServiceProvider.GetRequiredService<ITenantContext>();
                tenant.Set(idEmpresa);
                var migration = scope.ServiceProvider.GetRequiredService<IPedidoErpMigrationService>();
                // Breve espera para no pelear con la request HTTP.
                await Task.Delay(TimeSpan.FromSeconds(3));
                await migration.MigrarAsync(new[] { idPedidoMovil });
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex,
                    "Schedule write-back falló pedido {Pedido} empresa {Empresa}",
                    idPedidoMovil, idEmpresa);
            }
        });
    }
}
