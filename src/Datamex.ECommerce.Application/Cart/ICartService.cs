namespace Datamex.ECommerce.Application.Cart;

public record CartLineDto(
    long Id,
    long IDArticulo,
    string? Sku,
    string Nombre,
    string? ImagenUrl,
    long? IDArticuloEmbalaje,
    string? Embalaje,
    decimal Cantidad,
    decimal PrecioUnitario,
    decimal MontoTotal,
    decimal StockLibre,
    bool StockOk);

public record CartDto(
    long Id,
    string? IDSesion,
    string Canal,
    long IDDeposito,
    int Estado,
    decimal MontoTotal,
    decimal MontoIVA,
    int ItemsCount,
    bool ControlaStock,
    IReadOnlyList<CartLineDto> Items,
    decimal MontoDescuento = 0,
    string? CodigoCupon = null,
    long? IDAgenciaFacturacion = null);

public record AddCartItemRequest(
    long IDArticulo,
    decimal Cantidad = 1,
    long? IDArticuloEmbalaje = null,
    string Canal = "B2C");

public record UpdateCartItemRequest(decimal Cantidad);

public record SetPickupRequest(long IDAgencia);

public interface ICartService
{
    Task<CartDto> GetOrCreateAsync(string sessionId, string canal = "B2C", CancellationToken ct = default);
    Task<CartDto> AddItemAsync(string sessionId, AddCartItemRequest request, CancellationToken ct = default);
  Task<CartDto> UpdateItemAsync(string sessionId, long detalleId, decimal cantidad, string canal = "B2C", CancellationToken ct = default);
  Task<CartDto> RemoveItemAsync(string sessionId, long detalleId, string canal = "B2C", CancellationToken ct = default);
    Task ClearAsync(string sessionId, CancellationToken ct = default);
    /// <summary>Paridad ERP SetSucursal: asigna agencia pickup y depósito de stock.</summary>
    Task<CartDto> SetPickupAsync(string sessionId, long idAgencia, CancellationToken ct = default);
    /// <summary>Paridad ERP VolverAPedir: copia líneas de un pedido cerrado al carrito abierto.</summary>
    Task<CartDto> ReorderAsync(string sessionId, long nroPedido, string canal = "B2C", CancellationToken ct = default);
}
