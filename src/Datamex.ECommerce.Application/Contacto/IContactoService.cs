namespace Datamex.ECommerce.Application.Contacto;

public record ContactoRequest(
    string Nombre,
    string Email,
    string Mensaje,
    string? Telefono = null);

public record ContactoResultDto(bool Ok, string Mensaje, long? Id = null);

public record ContactoMensajeDto(
    long Id,
    string Nombre,
    string Email,
    string? Telefono,
    string Mensaje,
    DateTime FechaUtc,
    bool Leido);

public interface IContactoService
{
    Task<ContactoResultDto> EnviarAsync(ContactoRequest request, CancellationToken ct = default);
    Task<IReadOnlyList<ContactoMensajeDto>> ListAdminAsync(bool? leido = null, CancellationToken ct = default);
    Task MarcarLeidoAsync(long id, bool leido = true, CancellationToken ct = default);
}
