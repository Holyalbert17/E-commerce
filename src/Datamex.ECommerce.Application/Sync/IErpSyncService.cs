namespace Datamex.ECommerce.Application.Sync;

public record SyncCursorStatusDto(
    string EntityKey,
    DateTime? LastWatermarkUtc,
    long? LastId,
    DateTime? LastRunUtc,
    string? LastStatus,
    string? LastError,
    int RowsProcessed,
    int RowsRead = 0,
    int RowsInserted = 0,
    int RowsUpdated = 0,
    long DurationMs = 0);

public record ErpSyncStatusDto(
    bool ErpConnectionConfigured,
    IReadOnlyList<SyncCursorStatusDto> Cursors,
    string? ConnectionSource = null,
    bool WriteBackEffective = false,
    /// <summary>Motor inbound: siempre ecom-etl (API no hace pull).</summary>
    string InboundEngine = "ecom-etl");

public record ErpSyncRunResult(
    int EntitiesProcessed,
    int TotalRowsProcessed,
    IReadOnlyList<SyncCursorStatusDto> Cursors,
    long TotalDurationMs = 0);

public record ErpSyncRunRequest(IReadOnlyList<string>? EntityKeys = null);

/// <summary>Miss local → SELECT ERP → upsert espejo (híbrido).</summary>
public record ErpEnsureArticuloResult(
    bool Found,
    long? ArticuloId,
    bool Pulled,
    string? Message);

public interface IErpSyncService
{
    Task<ErpSyncStatusDto> GetStatusAsync(CancellationToken ct = default);
    Task<ErpSyncRunResult> RunDeltaAsync(IReadOnlyList<string>? entityKeys = null, CancellationToken ct = default);

    /// <summary>
    /// Lazy-pull por id o sku: articulo + embalajes + precios ecom + stock al espejo local.
    /// Solo lectura al ERP.
    /// </summary>
    Task<ErpEnsureArticuloResult> EnsureArticuloFromErpAsync(
        long? articuloId = null,
        string? sku = null,
        CancellationToken ct = default);
}
