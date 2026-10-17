namespace Datamex.ECommerce.Application.Admin;

public record PlanillaImportResult(int Inserted, int Updated, int Skipped, IReadOnlyList<string> Errors);

public interface IAdminPlanillaService
{
    Task<byte[]> ExportClientesCsvAsync(CancellationToken ct = default);
    Task<byte[]> ExportClientesExcelAsync(CancellationToken ct = default);
    Task<byte[]> ExportListaPreciosCsvAsync(long listaId, CancellationToken ct = default);
    Task<byte[]> ExportListaPreciosExcelAsync(long listaId, CancellationToken ct = default);
    Task<byte[]> ExportArticulosCsvAsync(CancellationToken ct = default);
    Task<byte[]> ExportArticulosExcelAsync(CancellationToken ct = default);
    Task<PlanillaImportResult> ImportClientesCsvAsync(Stream csv, CancellationToken ct = default);
    Task<PlanillaImportResult> ImportListaPreciosCsvAsync(long listaId, Stream csv, CancellationToken ct = default);
    Task<PlanillaImportResult> ImportArticulosActivoCsvAsync(Stream csv, CancellationToken ct = default);
}
