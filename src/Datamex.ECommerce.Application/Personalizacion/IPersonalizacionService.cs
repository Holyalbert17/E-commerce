namespace Datamex.ECommerce.Application.Personalizacion;

public static class PersonalizacionClaves
{
    public const string HomeLayout = "home.layout";
    public const string TemaActivo = "tema.activo";
    public const string CheckoutTextos = "checkout.textos";
    public const string CarritoTextos = "carrito.textos";
    public const string WidgetsToggles = "widgets.toggles";

    public static readonly IReadOnlyList<string> Publicas =
    [
        HomeLayout, TemaActivo, CheckoutTextos, CarritoTextos, WidgetsToggles
    ];

    public static readonly IReadOnlyList<string> Todas =
    [
        HomeLayout, TemaActivo, CheckoutTextos, CarritoTextos, WidgetsToggles
    ];
}

public record PersonalizacionItemDto(
    string Clave,
    string ValorJson,
    int Version,
    DateTime? FechaInicioVigencia,
    DateTime? FechaFinVigencia,
    bool Activo,
    DateTime FechaCreacion,
    string? CreadoPor,
    string? Nota);

public record UpsertPersonalizacionRequest(
    string ValorJson,
    DateTime? FechaInicioVigencia = null,
    DateTime? FechaFinVigencia = null,
    string? Nota = null,
    bool? Activo = true);

public record ProgramarPersonalizacionRequest(
    string ValorJson,
    DateTime FechaInicioVigencia,
    DateTime FechaFinVigencia,
    string? Nota = null);

public record StorefrontPersonalizacionDto(
    IReadOnlyDictionary<string, string> Claves);

public interface IPersonalizacionService
{
    Task<PersonalizacionItemDto?> GetActivaAsync(string clave, CancellationToken ct = default);
    Task<IReadOnlyList<PersonalizacionItemDto>> ListVersionesAsync(string clave, CancellationToken ct = default);
    Task<PersonalizacionItemDto> UpsertAsync(string clave, UpsertPersonalizacionRequest request, string? userName, CancellationToken ct = default);
    Task<PersonalizacionItemDto> ProgramarAsync(string clave, ProgramarPersonalizacionRequest request, string? userName, CancellationToken ct = default);
    Task<StorefrontPersonalizacionDto> GetPublicasAsync(CancellationToken ct = default);
    IReadOnlyDictionary<string, string> GetDefaultSchemas();
}
