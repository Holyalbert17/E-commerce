namespace Datamex.ECommerce.Application.Pricing;

public record PriceQuoteRequest(
    string Canal = "B2C",
    long? IDListaPrecio = null,
    long? IDCuenta = null,
    decimal Cantidad = 1);

public record EmbalajePriceDto(
    long IDArticuloEmbalaje,
    long IDArticulo,
    decimal PrecioContado,
    decimal? PrecioCredito,
    decimal CantidadMinimaMayorista,
    long IDListaPrecio,
    string ListaDescripcion,
    string Canal,
    bool PrecioConImpuestos,
    decimal? PrecioLista = null,
    decimal? PorcentajeDescuento = null);

public record ProductPriceSummaryDto(
    long IDArticulo,
    decimal? PrecioDesde,
    long? IDListaPrecio,
    string? ListaDescripcion,
    string Canal,
    decimal? PrecioLista = null,
    decimal? PorcentajeDescuento = null);

public interface IPricingService
{
    /// <summary>B2B solo si el usuario del tenant está mayorista aprobado; si no, fuerza B2C.</summary>
    Task<string> ResolveEffectiveCanalAsync(string? requestedCanal, CancellationToken ct = default);

    Task<ListaPrecioInfo?> ResolveListaAsync(string canal, long? idListaOverride = null, CancellationToken ct = default);
    Task<IReadOnlyDictionary<long, ProductPriceSummaryDto>> GetProductPriceSummariesAsync(
        IEnumerable<long> articuloIds, string canal, CancellationToken ct = default);
    Task<IReadOnlyList<EmbalajePriceDto>> GetEmbalajePricesAsync(
        long articuloId, string canal, CancellationToken ct = default);
}

public record ListaPrecioInfo(long Id, string Descripcion, string Canal, bool PrecioConImpuestos);
