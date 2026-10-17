using Datamex.ECommerce.Domain.Common;

namespace Datamex.ECommerce.Domain.Entities;

/// <summary>Espejo de <c>dbo.empresaconfiguracionecommerceredsocial</c>.</summary>
public class EmpresaConfiguracionEcommerceRedSocial : EntidadSinEmpresa
{
    public long IDEmpresaConfiguracionEcommerce { get; set; }
    public string NombreRedSocial { get; set; } = string.Empty;
    public string DireccionURLRedSocial { get; set; } = string.Empty;
    public string LogoRedSocial { get; set; } = string.Empty;

    public EmpresaConfiguracionEcommerce? EmpresaConfiguracionEcommerce { get; set; }
}
