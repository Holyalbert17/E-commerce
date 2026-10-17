using Datamex.ECommerce.Domain.Common;

namespace Datamex.ECommerce.Domain.Entities;

/// <summary>Mensaje del formulario de contacto del storefront.</summary>
public class ContactoMensaje : EntidadConEmpresa
{
    public string Nombre { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Telefono { get; set; }
    public string Mensaje { get; set; } = string.Empty;
    public DateTime FechaUtc { get; set; } = DateTime.UtcNow;
    public bool Leido { get; set; }
}
