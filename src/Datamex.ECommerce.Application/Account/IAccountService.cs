namespace Datamex.ECommerce.Application.Account;

public record SolicitarMayoristaRequest(string? Motivo = null);

public record UpdateProfileRequest(
    string Nombre,
    string Apellido,
    string? Telefono,
    string? NumeroDocumento);

public record AccountProfileDto(
    long IDUsuarioEcommerce,
    string Usuario,
    string Nombre,
    string Apellido,
    string? Email,
    string? Telefono,
    string? NumeroDocumento,
    string CanalPreferido,
    long? IDCuentaPrincipal,
    string EstadoMayorista = "None",
    decimal MontoComprasAcumulado = 0,
    decimal MontoUmbralMayorista = 0,
    long? IDListaPrecio = null,
    string? ListaPrecioDescripcion = null);

public record AccountOrderDto(
    long IDCarrito,
    long NroPedido,
    long? IDPedidoMovil,
    DateTime FechaPedido,
    string RazonSocial,
    string NumeroDocumento,
    int TipoEntrega,
    int TipoPago,
    string? Direccion,
    decimal MontoTotal,
    int Estado,
    bool Migrado,
    bool Cancelado,
    long? NroFactura = null,
    bool Facturado = false,
    bool PuedeDescargarFactura = false,
    bool PuedeCancelar = false,
    long? IdNotaCreditoErp = null,
    long? NroNotaCredito = null,
    bool PuedeDescargarNotaCredito = false);

public record CancelOrderRequest(string? Motivo = null);

public interface IAccountService
{
    Task<AccountProfileDto?> GetProfileAsync(long idUsuarioEcommerce, CancellationToken ct = default);
    Task<AccountProfileDto> UpdateProfileAsync(long idUsuarioEcommerce, UpdateProfileRequest request, CancellationToken ct = default);
    Task<IReadOnlyList<AccountOrderDto>> GetOrdersAsync(long idUsuarioEcommerce, CancellationToken ct = default);
    Task CancelOrderAsync(long idUsuarioEcommerce, long nroPedido, string? motivo, CancellationToken ct = default);
    Task<AccountProfileDto> SolicitarMayoristaAsync(long idUsuarioEcommerce, SolicitarMayoristaRequest request, CancellationToken ct = default);
}

public interface IMayoristaService
{
    Task EvaluateAfterPurchaseAsync(long idEmpresa, long? idCuenta, CancellationToken ct = default);
    Task ApproveAsync(long idUsuarioEcommerce, long? idListaPrecio, CancellationToken ct = default);
    Task RejectAsync(long idUsuarioEcommerce, CancellationToken ct = default);
}
