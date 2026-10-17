namespace Datamex.ECommerce.Application.Sync;

/// <summary>
/// Resuelve la connection string ERP por empresa (override admin) o fallback global.
/// </summary>
public interface IErpConnectionResolver
{
    /// <summary>CS usable para sync/write-back, o null si no hay.</summary>
    Task<string?> ResolveAsync(long idEmpresa, CancellationToken ct = default);

    /// <summary>Origen de la CS resuelta: Tenant | Global | None.</summary>
    Task<string> ResolveSourceAsync(long idEmpresa, CancellationToken ct = default);

    /// <summary>True si esta empresa puede INSERT/UPDATE en ERP.</summary>
    Task<bool> AllowWriteBackAsync(long idEmpresa, CancellationToken ct = default);

    /// <summary>Prueba CS guardada (o draft sin persistir) + probe lectura ERP.</summary>
    Task<ErpConnectionTestResult> TestAsync(
        long idEmpresa,
        TestErpConnectionRequest? draft = null,
        CancellationToken ct = default);
}

/// <summary>Credenciales en borrador para probar sin guardar.</summary>
public record TestErpConnectionRequest(
    string? ErpHost = null,
    int? ErpPort = null,
    string? ErpDatabase = null,
    string? ErpUsername = null,
    string? ErpPassword = null,
    string? ErpSslMode = null,
    /// <summary>Si password vacío, reutiliza el guardado de la empresa.</summary>
    bool UseSavedPasswordIfEmpty = true);

public record ErpConnectionTestResult(
    bool Ok,
    string Message,
    int? ServerVersionMajor = null,
    /// <summary>Tenant | Global | Draft | None</summary>
    string? Source = null,
    string? Host = null,
    string? Database = null,
    bool? CanReadEmpresa = null,
    bool? CanReadStock = null,
    long? StockRowsForDeposito = null,
    long? IdDepositoProbed = null);
