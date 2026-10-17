namespace Datamex.ECommerce.Application.Shipping;

public record TarifaDeliveryDto(
    long Id,
    long IDCiudad,
    long IDBarrio,
    string Ciudad,
    string Barrio,
    decimal ImporteDelivery,
    DateTime FechaInicioVigencia,
    DateTime FechaFinVigencia);

public record ApplyShippingRequest(long? IDTarifaDelivery = null, long? IDBarrio = null);

public interface IShippingService
{
    Task<IReadOnlyList<TarifaDeliveryDto>> GetActiveTarifasAsync(CancellationToken ct = default);
    Task ApplyToCartAsync(string sessionId, ApplyShippingRequest request, CancellationToken ct = default);
    Task ClearFromCartAsync(string sessionId, CancellationToken ct = default);
}
