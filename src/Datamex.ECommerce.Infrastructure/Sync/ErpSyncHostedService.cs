using Datamex.ECommerce.Application.Abstractions;
using Datamex.ECommerce.Application.Sync;
using Datamex.ECommerce.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Datamex.ECommerce.Infrastructure.Sync;

/// <summary>
/// Sync continuo ERP → espejo local. El pull de stock respeta
/// <c>EmpresaConfiguracionEcommerce.SyncStockContinuo</c> por tenant y
/// la connection string por empresa (o fallback global).
/// </summary>
public sealed class ErpSyncHostedService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<ErpSyncHostedService> _logger;

    public ErpSyncHostedService(IServiceScopeFactory scopeFactory, ILogger<ErpSyncHostedService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var minutes = 5;
        try
        {
            await using var scope = _scopeFactory.CreateAsyncScope();
            var config = scope.ServiceProvider.GetRequiredService<IConfiguration>();
            minutes = Math.Clamp(config.GetValue("Erp:SyncIntervalMinutes", 5), 1, 120);
        }
        catch
        {
            // keep default
        }

        // Tick corto: cada empresa aplica su SyncStockIntervaloMinutos (mín. 1).
        // 30s permite near real-time sin martillar cuando el intervalo es mayor.
        var tick = TimeSpan.FromSeconds(30);
        _logger.LogInformation(
            "ErpSyncHostedService iniciado (tick {Tick} min; SyncIntervalMinutes global={Global}).",
            tick.TotalMinutes, minutes);

        await RunOnceSafeAsync(stoppingToken);

        using var timer = new PeriodicTimer(tick);
        while (await timer.WaitForNextTickAsync(stoppingToken))
            await RunOnceSafeAsync(stoppingToken);
    }

    private async Task RunOnceSafeAsync(CancellationToken stoppingToken)
    {
        try
        {
            await using var scope = _scopeFactory.CreateAsyncScope();
            var config = scope.ServiceProvider.GetRequiredService<IConfiguration>();
            if (!config.GetValue("Erp:Enabled", true))
            {
                _logger.LogDebug("Sync ERP omitido: Erp:Enabled=false.");
                return;
            }

            var db = scope.ServiceProvider.GetRequiredService<EcomDbContext>();
            var sync = scope.ServiceProvider.GetRequiredService<IErpSyncService>();
            var erpCs = scope.ServiceProvider.GetRequiredService<IErpConnectionResolver>();
            var tenant = scope.ServiceProvider.GetRequiredService<ITenantContext>();
            var now = DateTime.UtcNow;

            var empresas = await db.EmpresaConfiguracionesEcommerce.AsNoTracking()
                .Where(c => c.Activo && c.SyncStockContinuo)
                .Select(c => new { c.ID, c.SyncStockIntervaloMinutos })
                .ToListAsync(stoppingToken);

            if (empresas.Count == 0)
            {
                _logger.LogDebug("Sync stock continuo: ninguna empresa con SyncStockContinuo=true.");
                return;
            }

            foreach (var emp in empresas)
            {
                stoppingToken.ThrowIfCancellationRequested();
                var cs = await erpCs.ResolveAsync(emp.ID, stoppingToken);
                if (string.IsNullOrWhiteSpace(cs))
                {
                    _logger.LogDebug("Sync stock omitido empresa={Empresa}: sin CS ERP.", emp.ID);
                    continue;
                }

                var intervalMin = Math.Clamp(emp.SyncStockIntervaloMinutos <= 0 ? 5 : emp.SyncStockIntervaloMinutos, 1, 120);

                var last = await db.SyncCursors.AsNoTracking()
                    .Where(x => x.IDEmpresa == emp.ID && x.EntityKey == "stock")
                    .Select(x => x.LastRunUtc)
                    .FirstOrDefaultAsync(stoppingToken);
                if (last.HasValue && now - last.Value < TimeSpan.FromMinutes(intervalMin))
                    continue;

                tenant.Set(emp.ID);
                var keys = new[] { "stock" };
                var result = await sync.RunDeltaAsync(keys, stoppingToken);
                _logger.LogInformation(
                    "Sync stock continuo empresa={Empresa}: {Rows} filas (intervalo {Min} min).",
                    emp.ID, result.TotalRowsProcessed, intervalMin);
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            // shutdown
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error en sync ERP programado.");
        }
    }
}
