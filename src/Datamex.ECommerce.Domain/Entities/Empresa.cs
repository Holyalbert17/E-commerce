using Datamex.ECommerce.Domain.Common;

namespace Datamex.ECommerce.Domain.Entities;

/// <summary>
/// Espejo de <c>dbo.empresa</c> (campos usados por ECommerce ERP Site + maestro básico).
/// <see cref="RazonSocial"/> = <c>nombreempresa</c> del ERP.
/// <see cref="NumeroDocumento"/> = <c>ruc</c>.
/// </summary>
public class Empresa : EntidadSinEmpresa
{
    /// <summary>ERP <c>codempresa</c>.</summary>
    public int CodEmpresa { get; set; }

    /// <summary>ERP <c>nombreempresa</c> (también expuesto como razón social en admin).</summary>
    public string RazonSocial { get; set; } = string.Empty;

    /// <summary>Extensión ecom / fantasia local (no siempre en ERP).</summary>
    public string? NombreFantasia { get; set; }

    /// <summary>ERP <c>ruc</c>.</summary>
    public string? NumeroDocumento { get; set; }

    /// <summary>ERP <c>direccionempresa</c>.</summary>
    public string? DireccionEmpresa { get; set; }

    /// <summary>ERP <c>telefono</c>.</summary>
    public string? Telefono { get; set; }

    /// <summary>ERP <c>fax</c>.</summary>
    public string? Fax { get; set; }

    /// <summary>ERP <c>email</c>.</summary>
    public string? EMail { get; set; }

    /// <summary>ERP <c>representantelegal</c>.</summary>
    public string? RepresentanteLegal { get; set; }

    /// <summary>ERP <c>idpais</c>.</summary>
    public long? IDPais { get; set; }

    /// <summary>ERP <c>logoempresa</c> (URL o path).</summary>
    public string? LogoEmpresa { get; set; }

    public EmpresaConfiguracionEcommerce? ConfiguracionEcommerce { get; set; }
}
