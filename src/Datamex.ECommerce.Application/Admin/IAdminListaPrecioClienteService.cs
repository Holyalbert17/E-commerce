using Datamex.ECommerce.Application.Catalog;

namespace Datamex.ECommerce.Application.Admin;

public record AdminListaPrecioDto(
    long Id,
    int NroListaPrecio,
    string Descripcion,
    bool EsListaPrecioEcommerce,
    bool PrecioConImpuestos,
    string CanalEcommerce,
    DateTime? FechaInicioVigencia,
    DateTime? FechaFinVigencia,
    bool Activo,
    int Estado,
    int DetalleCount,
    decimal CantidadMinimaMayorista = 0,
    decimal PorDescuentoMinorista = 0,
    decimal PorDescuentoMayorista = 0);

public record UpsertListaPrecioRequest(
    string Descripcion,
    int? NroListaPrecio = null,
    bool EsListaPrecioEcommerce = true,
    bool PrecioConImpuestos = true,
    string CanalEcommerce = "B2C",
    DateTime? FechaInicioVigencia = null,
    DateTime? FechaFinVigencia = null,
    bool Activo = true,
    int? Estado = null,
    decimal? CantidadMinimaMayorista = null,
    decimal? PorDescuentoMinorista = null,
    decimal? PorDescuentoMayorista = null);

public record CambioVigenciaListasRequest(
    IReadOnlyList<long> Ids,
    DateTime? FechaInicioVigencia,
    DateTime? FechaFinVigencia);

public record AdminDetalleListaDto(
    long Id,
    long IDArticuloEmbalaje,
    long IDArticulo,
    string? Sku,
    string NombreArticulo,
    string? DescripcionEmbalaje,
    decimal PrecioContado,
    decimal PrecioCheque,
    decimal PrecioCredito,
    decimal CantidadMinimaMayorista,
    bool Activo);

public record UpsertDetalleListaRequest(
    long IDArticuloEmbalaje,
    decimal PrecioContado,
    decimal PrecioCheque = 0,
    decimal PrecioCredito = 0,
    decimal CantidadMinimaMayorista = 0,
    bool Activo = true);

public record UpdateDetalleListaRequest(
    decimal? PrecioContado = null,
    decimal? PrecioCheque = null,
    decimal? PrecioCredito = null,
    decimal? CantidadMinimaMayorista = null,
    bool? Activo = null);

public record AdminEmbalajeOptionDto(
    long IDArticuloEmbalaje,
    long IDArticulo,
    string? Sku,
    string NombreArticulo,
    string? DescripcionEmbalaje,
    decimal? PrecioDemo);

public interface IAdminListaPrecioService
{
    Task<IReadOnlyList<AdminListaPrecioDto>> ListAsync(CancellationToken ct = default);
    Task<AdminListaPrecioDto?> GetAsync(long id, CancellationToken ct = default);
    Task<AdminListaPrecioDto> CreateAsync(UpsertListaPrecioRequest request, CancellationToken ct = default);
    Task<AdminListaPrecioDto> UpdateAsync(long id, UpsertListaPrecioRequest request, CancellationToken ct = default);
    Task DeleteAsync(long id, CancellationToken ct = default);

    Task<IReadOnlyList<AdminDetalleListaDto>> ListDetallesAsync(long idLista, CancellationToken ct = default);
    Task<AdminDetalleListaDto> UpsertDetalleAsync(long idLista, UpsertDetalleListaRequest request, CancellationToken ct = default);
    Task<AdminDetalleListaDto> UpdateDetalleAsync(long idLista, long detalleId, UpdateDetalleListaRequest request, CancellationToken ct = default);
    Task DeleteDetalleAsync(long idLista, long detalleId, CancellationToken ct = default);
    Task<IReadOnlyList<AdminEmbalajeOptionDto>> SearchEmbalajesAsync(string? search, int take = 30, CancellationToken ct = default);
    Task<AdminListaPrecioDto> AutorizarAsync(long id, CancellationToken ct = default);
    Task<AdminListaPrecioDto> AnularAsync(long id, CancellationToken ct = default);
    Task<AdminListaPrecioDto> CancelarAsync(long id, CancellationToken ct = default);
    Task<int> CambioVigenciaAsync(CambioVigenciaListasRequest request, CancellationToken ct = default);
}

public record AdminClienteDto(
    long Id,
    long NroCuenta,
    string RazonSocial,
    string? NombreCuenta,
    string? ApellidoCuenta,
    string? Email,
    string? Direccion,
    long? IDListaPrecio,
    string? ListaDescripcion,
    long? IDAgencia,
    bool Activo,
    DateTime UltModificacion,
    long? IdTipoDocumento = null,
    string? NumeroDocumento = null,
    string? Telefono = null,
    long? IdUsuarioEcommerce = null);

public record UpsertClienteRequest(
    string RazonSocial,
    string? NombreCuenta = null,
    string? ApellidoCuenta = null,
    string? Email = null,
    string? Direccion = null,
    long? IDListaPrecio = null,
    long? IDAgencia = null,
    bool Activo = true,
    long? IDTipoDocumento = null,
    string? NumeroDocumento = null,
    string? Telefono = null);

public record AsignarListaPrecioRequest(long IDListaPrecio);

public record AdminListaPrecioCuentaDto(
    long Id,
    long IDCuenta,
    long IDListaPrecio,
    string? ListaDescripcion,
    int? NroListaPrecio,
    bool Activo,
    DateTime UltModificacion,
    string? UserName);

public record AdminMayoristaSolicitudDto(
    long IdUsuarioEcommerce,
    string Nombre,
    string? Email,
    string EstadoMayorista,
    DateTime? FechaSolicitud,
    string? Motivo,
    decimal MontoComprasAcumulado,
    long? IdCuenta,
    string? RazonSocial,
    long? IdListaPrecio);

public record ResolverMayoristaRequest(long? IdListaPrecio = null);

public interface IAdminClienteService
{
    Task<PagedResult<AdminClienteDto>> SearchAsync(
        string? search, int page, int pageSize, CancellationToken ct = default);
    Task<AdminClienteDto?> GetAsync(long id, CancellationToken ct = default);
    Task<AdminClienteDto> CreateAsync(UpsertClienteRequest request, CancellationToken ct = default);
    Task<AdminClienteDto> UpdateAsync(long id, UpsertClienteRequest request, CancellationToken ct = default);
    Task DeleteAsync(long id, CancellationToken ct = default);
    Task<AdminClienteDto> AsignarListaPrecioAsync(long id, AsignarListaPrecioRequest request, CancellationToken ct = default);
    Task<IReadOnlyList<AdminListaPrecioCuentaDto>> HistorialListasAsync(long idCuenta, CancellationToken ct = default);
    Task<IReadOnlyList<AdminListaPrecioDto>> ListasVigentesAsync(CancellationToken ct = default);
    Task<IReadOnlyList<AdminMayoristaSolicitudDto>> ListSolicitudesMayoristaAsync(CancellationToken ct = default);
}
