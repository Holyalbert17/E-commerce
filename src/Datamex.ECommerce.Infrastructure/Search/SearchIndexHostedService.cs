using Datamex.ECommerce.Application.Abstractions;
using Datamex.ECommerce.Application.Options;
using Datamex.ECommerce.Application.Search;
using Datamex.ECommerce.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Datamex.ECommerce.Infrastructure.Search;

/// <summary>Rebuild periódico del índice local (Standalone / Integrated sin OpenSearch).</summary>
public sealed class SearchIndexHostedService : BackgroundService
{
    private readonly IServiceScopeFactory _scopes;
    private readonly SearchOptions _opts;
    private readonly ILogger<SearchIndexHostedService> _log;

    public SearchIndexHostedService(
        IServiceScopeFactory scopes,
        IOptions<SearchOptions> opts,
        ILogger<SearchIndexHostedService> log)
    {
        _scopes = scopes;
        _opts = opts.Value;
        _log = log;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_opts.Enabled) return;

        await Task.Delay(TimeSpan.FromSeconds(8), stoppingToken);
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await using var scope = _scopes.CreateAsyncScope();
                var db = scope.ServiceProvider.GetRequiredService<EcomDbContext>();
                var search = scope.ServiceProvider.GetRequiredService<IProductSearchService>();
                var tenant = scope.ServiceProvider.GetRequiredService<ITenantContext>();
                var ids = await db.Empresas.AsNoTracking()
                    .Where(e => e.Activo && e.ID > 0)
                    .Select(e => e.ID)
                    .Take(50)
                    .ToListAsync(stoppingToken);

                foreach (var id in ids)
                {
                    try
                    {
                        // Pricing/inventory require a resolved tenant even when idEmpresa is passed.
                        tenant.Set(id);
                        await search.RebuildIndexAsync(id, stoppingToken);
                    }
                    catch (Exception ex)
                    {
                        _log.LogWarning(ex, "Search rebuild failed empresa {E}", id);
                    }
                }
            }
            catch (Exception ex)
            {
                _log.LogWarning(ex, "SearchIndexHostedService cycle failed");
            }

            var minutes = Math.Clamp(_opts.IndexRefreshMinutes, 1, 120);
            await Task.Delay(TimeSpan.FromMinutes(minutes), stoppingToken);
        }
    }
}
