namespace Datamex.ECommerce.Application.Favorites;

public record FavoritoItemDto(
    long IdDetalle,
    long IdArticulo,
    string? Sku,
    string Nombre,
    string? ImagenUrl,
    decimal? PrecioDesde,
    DateTime FechaAgregado,
    string? Nota);

public record FavoritoSeccionDto(
    long Id,
    string NombreLista,
    string CanalEcommerce,
    string Email,
    long? IdCuenta,
    int Orden,
    string? Color,
    int ItemsCount,
    IReadOnlyList<FavoritoItemDto> Items);

public record CrearSeccionFavoritoRequest(string NombreLista, string? Color = null, string? Canal = null);
public record AgregarFavoritoRequest(long IdArticulo, long? IdLista = null, string? NombreLista = null, string? Nota = null);
public record RenombrarSeccionRequest(string NombreLista, string? Color = null);
public record FavoritosDesdeCarritoRequest(string? NombreLista = null, string? Canal = null);

public interface IFavoriteService
{
    Task<IReadOnlyList<FavoritoSeccionDto>> ListSeccionesAsync(long idUsuario, string? canal = null, CancellationToken ct = default);
    Task<FavoritoSeccionDto> CrearSeccionAsync(long idUsuario, CrearSeccionFavoritoRequest request, CancellationToken ct = default);
    Task<FavoritoSeccionDto> RenombrarSeccionAsync(long idUsuario, long idLista, RenombrarSeccionRequest request, CancellationToken ct = default);
    Task EliminarSeccionAsync(long idUsuario, long idLista, CancellationToken ct = default);
    Task<FavoritoSeccionDto> AgregarAsync(long idUsuario, AgregarFavoritoRequest request, CancellationToken ct = default);
    Task QuitarAsync(long idUsuario, long idDetalle, CancellationToken ct = default);
    Task QuitarPorArticuloAsync(long idUsuario, long idArticulo, CancellationToken ct = default);
    Task<bool> EstaEnFavoritosAsync(long idUsuario, long idArticulo, CancellationToken ct = default);
    Task<FavoritoSeccionDto> CrearDesdeCarritoAsync(
        long idUsuario, string sessionId, FavoritosDesdeCarritoRequest request, CancellationToken ct = default);
}
