using Datamex.ECommerce.Application.Abstractions;
using Datamex.ECommerce.Application.Sync;
using Datamex.ECommerce.Domain.Entities;
using Datamex.ECommerce.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Datamex.ECommerce.Infrastructure.Sync;

/// <summary>
/// Reintento de pedidos e-com pendientes de INSERT en ERP (sin ETL).
/// </summary>
public sealed class PedidoErpWriteBackHostedService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<PedidoErpWriteBackHostedService> _logger;

    public PedidoErpWriteBackHostedService(
        IServiceScopeFactory scopeFactory,
        ILogger<PedidoErpWriteBackHostedService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var minutes = 2;
        try
        {
            await using var scope = _scopeFactory.CreateAsyncScope();
            var config = scope.ServiceProvider.GetRequiredService<IConfiguration>();
            minutes = Math.Clamp(config.GetValue("Erp:WriteBackRetryMinutes", 2), 1, 60);
        }
        catch { /* default */ }

        var interval = TimeSpan.FromMinutes(minutes);
        _logger.LogInformation(
            "PedidoErpWriteBackHostedService iniciado (reintento cada {Min} min).",
            interval.TotalMinutes);

        // Primera pasada un poco después del arranque
        try { await Task.Delay(TimeSpan.FromSeconds(20), stoppingToken); }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { return; }

        await RunOnceSafeAsync(stoppingToken);

        using var timer = new PeriodicTimer(interval);
        while (await timer.WaitForNextTickAsync(stoppingToken))
            await RunOnceSafeAsync(stoppingToken);
    }

    private async Task RunOnceSafeAsync(CancellationToken stoppingToken)
    {
        try
        {
            await using var scope = _scopeFactory.CreateAsyncScope();
            var config = scope.ServiceProvider.GetRequiredService<IConfiguration>();
            // Write-back es independiente del inbound (ecom-etl). No exigir Erp:Enabled.
            if (!config.GetValue("Erp:AutoWriteBack", true))
                return;

            var db = scope.ServiceProvider.GetRequiredService<EcomDbContext>();
            var erpCs = scope.ServiceProvider.GetRequiredService<IErpConnectionResolver>();
            var pending = await (
                from p in db.PedidosMovil.AsNoTracking()
                join c in db.EmpresaConfiguracionesEcommerce.AsNoTracking() on p.IDEmpresa equals c.ID
                where p.Activo
                      && !p.Migrado
                      && p.OrigenPedido == OrigenPedidoMovil.Ecommerce
                      && c.MigrarPedidosAlGenerar
                      && c.ProcesaPedidosERP
                orderby p.ID
                select new { p.ID, p.IDEmpresa }
            ).Take(40).ToListAsync(stoppingToken);

            if (pending.Count == 0) return;

            var migration = scope.ServiceProvider.GetRequiredService<IPedidoErpMigrationService>();
            var tenant = scope.ServiceProvider.GetRequiredService<ITenantContext>();

            foreach (var group in pending.GroupBy(x => x.IDEmpresa))
            {
                var cs = await erpCs.ResolveAsync(group.Key, stoppingToken);
                var allow = await erpCs.AllowWriteBackAsync(group.Key, stoppingToken);
                if (string.IsNullOrWhiteSpace(cs) || !allow)
                {
                    _logger.LogDebug(
                        "Write-back retry omitido empresa={Empresa}: sin CS o AllowWriteBack.",
                        group.Key);
                    continue;
                }

                tenant.Set(group.Key);
                var ids = group.Select(x => x.ID).ToList();
                var result = await migration.MigrarAsync(ids, stoppingToken);
                _logger.LogInformation(
                    "Write-back retry empresa={Empresa}: {N} pedidos, ok={Ok}, err={Err}",
                    group.Key, ids.Count, result.Exitosos, result.Errores);
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error en reintento write-back pedidos ERP.");
        }
    }
}
