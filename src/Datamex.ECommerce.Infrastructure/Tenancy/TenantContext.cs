using Datamex.ECommerce.Application.Abstractions;

namespace Datamex.ECommerce.Infrastructure.Tenancy;

public sealed class TenantContext : ITenantContext
{
    public long? IDEmpresa { get; private set; }
    public string? Slug { get; private set; }
    public long? IDUsuarioEcommerce { get; private set; }
    public long? IDAdminUsuario { get; private set; }
    public bool IsResolved => IDEmpresa.HasValue;

    public void Set(long idEmpresa, string? slug = null)
    {
        IDEmpresa = idEmpresa;
        Slug = slug;
    }

    public void SetUser(long? idUsuarioEcommerce) => IDUsuarioEcommerce = idUsuarioEcommerce;
    public void SetAdminUser(long? idAdminUsuario) => IDAdminUsuario = idAdminUsuario;
}
