using Datamex.ECommerce.Domain.Common;

namespace Datamex.ECommerce.Domain.Entities;

/// <summary>
/// Usuario del backoffice (manager). Alineado a Seguridad ERP (<c>empresausuario</c>).
/// </summary>
public sealed class AdminUsuario : EntidadConEmpresa
{
    public string Usuario { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string? Nombre { get; set; }
    public string? Email { get; set; }
}

/// <summary>Espejo <c>empresarol</c>.</summary>
public sealed class AdminRol : EntidadConEmpresa
{
    public string CodigoRol { get; set; } = string.Empty;
    public string NombreRol { get; set; } = string.Empty;
}

/// <summary>
/// Operación / funcionalidad (espejo <c>operacion</c>).
/// Jerarquía vía <see cref="IDFuncionalidadPadre"/> como <c>IDOperacionPadre</c> del ERP.
/// </summary>
public sealed class AdminFuncionalidad : EntidadConEmpresa
{
    public string CodigoPermission { get; set; } = string.Empty;
    public string Descripcion { get; set; } = string.Empty;
    /// <summary>ERP <c>IDOperacionPadre</c> — null = raíz de módulo.</summary>
    public long? IDFuncionalidadPadre { get; set; }
    public int Orden { get; set; }
}

/// <summary>Espejo <c>empresausuariorol</c>.</summary>
public sealed class AdminUsuarioRol : EntidadConEmpresa
{
    public long IDUsuario { get; set; }
    public long IDRol { get; set; }
}

/// <summary>Espejo <c>empresaroloperacion</c>.</summary>
public sealed class AdminRolFuncionalidad : EntidadConEmpresa
{
    public long IDRol { get; set; }
    public long IDFuncionalidad { get; set; }
}

/// <summary>
/// Grants directos usuario↔operación (espejo <c>empresausuariooperacion</c>).
/// </summary>
public sealed class AdminUsuarioFuncionalidad : EntidadConEmpresa
{
    public long IDUsuario { get; set; }
    public long IDFuncionalidad { get; set; }
}
