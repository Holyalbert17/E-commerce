namespace Datamex.ECommerce.Application.Geography;

public record CiudadDeliveryDto(long Id, string Nombre, string? DivisionPolitica);
public record BarrioDeliveryDto(long Id, long IDCiudad, string Nombre, decimal? ImporteDelivery);
public record TipoDocumentoDto(long Id, int Codigo, string Nombre, bool EsRuc);
public record AgenciaDto(
    long Id, string Nombre, string? Direccion, long? IDCiudad,
    decimal? Latitud, decimal? Longitud, long? IDDeposito = null);
public record HorarioAtencionDto(
    int DiaSemana,
    string Apertura,
    string Cierre,
    string? Apertura2,
    string? Cierre2);
public record MapDefaultsDto(decimal Lat, decimal Lng);
public record AgenciaAbiertaDto(bool Abierta, string? Motivo);

public interface IGeographyService
{
    /// <summary>Ciudades con tarifa delivery vigente (como EcommerceBaseService ERP).</summary>
    Task<IReadOnlyList<CiudadDeliveryDto>> GetCiudadesDeliveryAsync(CancellationToken ct = default);

    /// <summary>Barrios con tarifa vigente; opcional filtro por ciudad.</summary>
    Task<IReadOnlyList<BarrioDeliveryDto>> GetBarriosDeliveryAsync(long? idCiudad = null, CancellationToken ct = default);

    Task<IReadOnlyList<TipoDocumentoDto>> GetTiposDocumentoAsync(CancellationToken ct = default);
    Task<IReadOnlyList<AgenciaDto>> GetAgenciasPickupAsync(CancellationToken ct = default);
    Task<IReadOnlyList<HorarioAtencionDto>> GetAgenciaHorariosAsync(long idAgencia, CancellationToken ct = default);
    Task<AgenciaAbiertaDto> IsAgenciaOpenAsync(long idAgencia, DateTime? atLocal = null, CancellationToken ct = default);
    Task<MapDefaultsDto> GetMapDefaultsAsync(CancellationToken ct = default);
}
