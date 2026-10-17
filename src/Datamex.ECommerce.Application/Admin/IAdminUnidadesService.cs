namespace Datamex.ECommerce.Application.Admin;

public record AdminUnidadMedidaDto(
    long Id,
    string Codigo,
    string Nombre,
    string? Simbolo,
    bool PermiteDecimales,
    decimal IncrementoDefault,
    int Orden,
    bool Activo);

public record UpsertUnidadMedidaRequest(
    string Codigo,
    string Nombre,
    string? Simbolo = null,
    bool PermiteDecimales = false,
    decimal IncrementoDefault = 1,
    int Orden = 0,
    bool Activo = true);

public record AdminProductEmbalajeDto(
    long Id,
    long IdArticulo,
    string? Descripcion,
    double CantidadPorEmbalaje,
    string? CodigoBarras,
    string CodigoUnidadMedida,
    bool PermiteFraccion,
    decimal IncrementoVenta,
    decimal CantidadMinimaVenta,
    bool ParaFacturacion,
    bool ParaStock,
    bool Activo,
    decimal? PrecioDemo);

public record UpsertProductEmbalajeRequest(
    string? Descripcion,
    double CantidadPorEmbalaje = 1,
    string? CodigoBarras = null,
    string CodigoUnidadMedida = "UN",
    bool PermiteFraccion = false,
    decimal IncrementoVenta = 1,
    decimal CantidadMinimaVenta = 1,
    bool ParaFacturacion = true,
    bool ParaStock = true,
    bool Activo = true,
    decimal? PrecioDemo = null);

public record AdminCodigoBarraDto(
    long Id,
    long IdArticuloEmbalaje,
    string CodigoBarra,
    bool ParaSupermercado,
    bool Activo);

public record UpsertCodigoBarraRequest(
    string CodigoBarra,
    bool ParaSupermercado = false,
    bool Activo = true);

public interface IAdminUnidadesService
{
    Task<IReadOnlyList<AdminUnidadMedidaDto>> ListUnidadesAsync(CancellationToken ct = default);
    Task<AdminUnidadMedidaDto> UpsertUnidadAsync(long? id, UpsertUnidadMedidaRequest request, CancellationToken ct = default);
    Task DeleteUnidadAsync(long id, CancellationToken ct = default);

    Task<IReadOnlyList<AdminProductEmbalajeDto>> ListEmbalajesAsync(long idArticulo, CancellationToken ct = default);
    Task<AdminProductEmbalajeDto> UpsertEmbalajeAsync(long idArticulo, long? id, UpsertProductEmbalajeRequest request, CancellationToken ct = default);
    Task DeleteEmbalajeAsync(long idArticulo, long id, CancellationToken ct = default);

    Task<IReadOnlyList<AdminCodigoBarraDto>> ListCodigosBarraAsync(long idArticuloEmbalaje, CancellationToken ct = default);
    Task<AdminCodigoBarraDto> UpsertCodigoBarraAsync(long idArticuloEmbalaje, long? id, UpsertCodigoBarraRequest request, CancellationToken ct = default);
    Task DeleteCodigoBarraAsync(long idArticuloEmbalaje, long id, CancellationToken ct = default);
}
