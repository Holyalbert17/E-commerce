using Datamex.ECommerce.Application.Catalog;

namespace Datamex.ECommerce.Application.Admin;

public record AdminProductDto(
    long Id,
    string? Sku,
    string Nombre,
    string? Marca,
    bool Activo,
    bool ActivoEcommerce,
    string EstadoEcommerce,
    decimal? PrecioDesdeB2C,
    decimal StockExistencia,
    long? IDDeposito,
    string OrigenDatos = "Local",
    bool IsReadOnly = false,
    string? SlugSeo = null);

public record UpdateAdminProductRequest(
    bool? ActivoEcommerce = null,
    string? EstadoEcommerce = null,
    string? Nombre = null,
    string? NombreCorto = null);

public record UpdateStockRequest(decimal TotalExistencia);

public record AdminOrderListItemDto(
    long IDPedidoMovil,
    long NroPedido,
    DateTime FechaPedido,
    string RazonSocial,
    string NumeroDocumento,
    decimal MontoTotal,
    int TipoEntrega,
    int TipoPago,
    int Estado,
    bool Migrado,
    int ItemsCount,
    string? LugarEntrega,
    string? EstadoFulfillment = null,
    long? NroFactura = null,
    bool Facturado = false,
    bool PuedeDescargarFactura = false);

public record AdminOrderDetailDto(
    long IDPedidoMovil,
    long NroPedido,
    long? IDCarrito,
    DateTime FechaPedido,
    DateTime? FechaPrevistaEntrega,
    string RazonSocial,
    string NumeroDocumento,
    string? Observacion,
    string? LugarEntrega,
    int TipoEntrega,
    int TipoPago,
    int Estado,
    bool Migrado,
    int OrigenPedido,
    decimal MontoTotal,
    decimal MontoIVA,
    long IDDeposito,
    IReadOnlyList<AdminOrderLineDto> Items,
    decimal? Latitud = null,
    decimal? Longitud = null,
    string? DireccionEntrega = null,
    long? IdChofer = null,
    string? NombreChofer = null,
    string? EstadoFulfillment = null,
    long? NroFactura = null,
    bool Facturado = false,
    bool PuedeDescargarFactura = false);

public record AdminOrderLineDto(
    long IDArticulo,
    string? Sku,
    string Nombre,
    int CantidadEmbalada,
    decimal PrecioUnitario,
    decimal MontoLinea);

public record AdminDashboardDto(
    int ArticulosEcommerce,
    int ArticulosAgotados,
    int PedidosHoy,
    int PedidosPendientesMigracion,
    decimal MontoPedidosHoy);

public record AdminProductImageDto(
    long Id,
    long IDArticulo,
    string Url,
    string? Titulo,
    int Orden,
    bool EsPrincipal,
    bool Activo);

public record UpsertProductImageRequest(
    string Url,
    string? Titulo = null,
    int Orden = 1,
    bool EsPrincipal = false,
    bool Activo = true);

public record BulkActivoEcommerceRequest(
    IReadOnlyList<long> Ids,
    bool ActivoEcommerce,
    string? EstadoEcommerce = null);

public record BulkActivoEcommerceResult(int UpdatedCount);

public interface IAdminCatalogService
{
    Task<PagedResult<AdminProductDto>> SearchAsync(
        string? search, string? estado, int page, int pageSize,
        bool? activo = null, bool? activoEcommerce = null,
        string? calidad = null, CancellationToken ct = default);
    Task<AdminProductDto?> GetAsync(long id, CancellationToken ct = default);
    Task<AdminArticuloDetailDto?> GetDetailAsync(long id, CancellationToken ct = default);
    Task<AdminArticuloDetailDto> CreateAsync(CreateAdminArticuloRequest request, CancellationToken ct = default);
    Task<AdminArticuloDetailDto> UpsertMasterAsync(long id, UpsertAdminArticuloRequest request, CancellationToken ct = default);
    Task DeleteAsync(long id, CancellationToken ct = default);
    Task<AdminProductDto> UpdateAsync(long id, UpdateAdminProductRequest request, CancellationToken ct = default);
    Task<AdminProductDto> UpdateStockAsync(long id, UpdateStockRequest request, CancellationToken ct = default);

    Task<BulkActivoEcommerceResult> BulkSetActivoEcommerceAsync(
        BulkActivoEcommerceRequest request, CancellationToken ct = default);

    Task<IReadOnlyList<AdminProductImageDto>> ListImagesAsync(long articuloId, CancellationToken ct = default);
    Task<AdminProductImageDto> CreateImageAsync(long articuloId, UpsertProductImageRequest request, CancellationToken ct = default);
    Task<AdminProductImageDto> UploadImageAsync(
        long articuloId,
        Stream content,
        string contentType,
        string fileName,
        string? titulo,
        bool esPrincipal,
        CancellationToken ct = default);
    Task<AdminProductImageDto> UpdateImageAsync(long articuloId, long imageId, UpsertProductImageRequest request, CancellationToken ct = default);
    Task DeleteImageAsync(long articuloId, long imageId, CancellationToken ct = default);
    Task<AdminProductImageDto> SetPrincipalImageAsync(long articuloId, long imageId, CancellationToken ct = default);
    Task ReorderImagesAsync(long articuloId, IReadOnlyList<long> orderedIds, CancellationToken ct = default);
}

public interface IAdminOrdersService
{
    Task<PagedResult<AdminOrderListItemDto>> SearchAsync(
        string? search, bool? migrado, int page, int pageSize,
        string? estadoFulfillment = null, CancellationToken ct = default);
    Task<AdminOrderDetailDto?> GetAsync(long idPedidoMovil, CancellationToken ct = default);
    Task<MarkMigradosResult> MarkMigradosAsync(IReadOnlyList<long> ids, CancellationToken ct = default);
    Task<IReadOnlyList<AdminOrderExportRowDto>> GetExportAsync(
        DateTime? fechaDesde = null, DateTime? fechaHasta = null, CancellationToken ct = default);
    /// <summary>Asigna N° factura local SOLO si no hay integración ERP. Preferí SyncFacturaErp.</summary>
    Task<AsignarFacturaResult> AsignarFacturaAsync(
        long idPedidoMovil, long? nroFactura = null, CancellationToken ct = default);
    Task<AsignarFacturaBulkResult> AsignarFacturaBulkAsync(
        IReadOnlyList<long> ids, CancellationToken ct = default);
    /// <summary>Trae N° factura emitida en ERP al espejo local.</summary>
    Task<ErpFacturaSyncResultDto> SyncFacturaErpAsync(long idPedidoMovil, CancellationToken ct = default);
}

public record AsignarFacturaResult(long IdPedidoMovil, long NroPedido, long NroFactura, bool YaFacturado);
public record AsignarFacturaBulkResult(int Procesados, int Exitosos, int Errores, IReadOnlyList<AsignarFacturaResult> Items);
public record ErpFacturaSyncResultDto(bool Ok, string Mensaje, long? NroFactura, long? IdPedidoErp, bool EsFiscalErp);

public record AdminOrderExportRowDto(
    long IdPedidoMovil,
    long NroPedido,
    DateTime FechaPedido,
    string RazonSocial,
    string NumeroDocumento,
    decimal MontoTotal,
    int TipoEntrega,
    int TipoPago,
    int Estado,
    bool Migrado,
    string? LugarEntrega,
    long? NroFactura);

public interface IAdminDashboardService
{
    Task<AdminDashboardDto> GetAsync(CancellationToken ct = default);
}
