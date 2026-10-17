using Datamex.ECommerce.Domain.Common;

namespace Datamex.ECommerce.Domain.Entities;

/// <summary>Espejo de usuarioecommerce.</summary>
public class UsuarioEcommerce : EntidadConEmpresa
{
    public string CodigoUsuario { get; set; } = string.Empty;
    public string Usuario { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string Nombre { get; set; } = string.Empty;
    public string Apellido { get; set; } = string.Empty;
    public long? IDTipoDocumento { get; set; }
    public string? Email { get; set; }
    public string? NumeroTelefono { get; set; }
    public string? NumeroMovil { get; set; }
    public string? NumeroDocumento { get; set; }
    public DateTime FechaRegistro { get; set; } = DateTime.UtcNow;
    public bool Activado { get; set; }
    public bool Bloqueado { get; set; }
    public string? CodigoActivacion { get; set; }
    /// <summary>Espejo ChecksumActivacion ERP (MD5 del GUID completo).</summary>
    public string? ChecksumActivacion { get; set; }
    public string? CheckSum { get; set; }

    /// <summary>Token de reset de contraseña (GUID).</summary>
    public string? CodigoResetPassword { get; set; }
    public string? ChecksumResetPassword { get; set; }
    public DateTime? FechaExpiraResetPassword { get; set; }

    public TipoDocumento? TipoDocumento { get; set; }

    /// <summary>Extensión: canal preferido B2C/B2B.</summary>
    public string CanalPreferido { get; set; } = "B2C";

    /// <summary>None | Solicitado | Aprobado | Rechazado</summary>
    public string EstadoMayorista { get; set; } = "None";
    public DateTime? FechaSolicitudMayorista { get; set; }
    public string? MotivoSolicitudMayorista { get; set; }
    public decimal MontoComprasAcumulado { get; set; }

    public ICollection<UsuarioEcommerceCuenta> UsuarioEcommerceCuentas { get; set; } = new List<UsuarioEcommerceCuenta>();
}

/// <summary>Espejo de usuarioecommercecuenta — vínculo usuario ↔ cuenta ERP.</summary>
public class UsuarioEcommerceCuenta : EntidadConEmpresa
{
    public long IDUsuarioEcommerce { get; set; }
    public long IDCuenta { get; set; }
    public bool EsPrincipal { get; set; }

    public UsuarioEcommerce? UsuarioEcommerce { get; set; }
}
