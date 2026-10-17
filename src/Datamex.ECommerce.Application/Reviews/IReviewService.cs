namespace Datamex.ECommerce.Application.Reviews;

public record ResenaDto(
    long Id,
    long IdArticulo,
    string Email,
    string CanalEcommerce,
    int Puntuacion,
    string? Titulo,
    string? Comentario,
    DateTime FechaResena,
    string? NombreAutor,
    bool EsMia = false);

public record ResumenCalificacionDto(
    long IdArticulo,
    double Promedio,
    int TotalResenas,
    int Estrellas5,
    int Estrellas4,
    int Estrellas3,
    int Estrellas2,
    int Estrellas1);

public record CrearResenaRequest(
    long IdArticulo,
    int Puntuacion,
    string? Titulo,
    string? Comentario,
    long? IdPedidoMovil = null,
    long? IdCarritoCompra = null);

public record ProductoElegibleResenaDto(
    long IdArticulo,
    string Nombre,
    string? Sku,
    string? ImagenUrl,
    long? IdPedidoMovil,
    long? IdCarritoCompra,
    long NroPedido,
    bool YaReseno);

public interface IReviewService
{
    Task<ResumenCalificacionDto> GetResumenAsync(long idArticulo, CancellationToken ct = default);
    Task<IReadOnlyList<ResenaDto>> ListByProductAsync(long idArticulo, int take = 50, CancellationToken ct = default);
    Task<IReadOnlyList<ProductoElegibleResenaDto>> ListElegiblesAsync(long idUsuario, CancellationToken ct = default);
    Task<ResenaDto> CreateAsync(long idUsuario, CrearResenaRequest request, CancellationToken ct = default);
    Task<IReadOnlyList<ResenaDto>> AdminListPendientesAsync(CancellationToken ct = default);
    Task AprobarAsync(long idResena, bool aprobar, CancellationToken ct = default);
}
