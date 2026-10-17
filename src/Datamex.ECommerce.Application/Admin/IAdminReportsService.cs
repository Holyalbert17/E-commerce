namespace Datamex.ECommerce.Application.Admin;

public record VentasDiaDto(DateTime Fecha, int PedidosCount, decimal MontoTotal);

public record TopSkuDto(
    long IdArticulo,
    string? Sku,
    string Nombre,
    int CantidadVendida,
    decimal MontoTotal);

public record VentasResumenDto(
    DateTime Desde,
    DateTime Hasta,
    int TotalPedidos,
    decimal MontoTotal,
    decimal TicketPromedio,
    int CarritosAbiertos,
    int CarritosCerrados,
    decimal ConversionPct,
    IReadOnlyList<VentasDiaDto> PorDia,
    IReadOnlyList<TopSkuDto> TopSkus);

public interface IAdminReportsService
{
    Task<VentasResumenDto> GetVentasResumenAsync(
        DateTime from, DateTime to, CancellationToken ct = default);
}
