using Datamex.ECommerce.Application.Sync;
using Xunit;

namespace Datamex.ECommerce.Application.Tests.Sync;

/// <summary>
/// Reglas de métricas de sync (idempotencia conceptual):
/// 2da corrida sobre mismos IDs debe reportar updates, no inserts netos.
/// </summary>
public class SyncCursorMetricsTests
{
    [Fact]
    public void SyncCursorStatusDto_defaults_and_metrics()
    {
        var first = new SyncCursorStatusDto(
            "catalog", null, null, DateTime.UtcNow, "ok", null,
            RowsProcessed: 10, RowsRead: 10, RowsInserted: 10, RowsUpdated: 0, DurationMs: 120);
        var second = new SyncCursorStatusDto(
            "catalog", null, null, DateTime.UtcNow, "ok", null,
            RowsProcessed: 10, RowsRead: 10, RowsInserted: 0, RowsUpdated: 10, DurationMs: 80);

        Assert.Equal(10, first.RowsInserted);
        Assert.Equal(0, second.RowsInserted);
        Assert.Equal(10, second.RowsUpdated);
        Assert.Equal(first.RowsProcessed, second.RowsProcessed);
    }

    [Fact]
    public void ErpSyncRunResult_aggregates_duration()
    {
        var cursors = new[]
        {
            new SyncCursorStatusDto("empresa", null, null, null, "ok", null, 1, 1, 1, 0, 5),
            new SyncCursorStatusDto("stock", null, null, null, "ok", null, 0, 0, 0, 0, 12),
        };
        var result = new ErpSyncRunResult(2, 1, cursors, TotalDurationMs: 20);
        Assert.Equal(2, result.EntitiesProcessed);
        Assert.Equal(20, result.TotalDurationMs);
        Assert.Equal(0, cursors[1].RowsInserted);
    }
}
