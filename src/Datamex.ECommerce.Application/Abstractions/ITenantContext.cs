namespace Datamex.ECommerce.Application.Abstractions;

public interface ITenantContext
{
    long? IDEmpresa { get; }
    string? Slug { get; }
    long? IDUsuarioEcommerce { get; }
    long? IDAdminUsuario { get; }
    bool IsResolved { get; }
    void Set(long idEmpresa, string? slug = null);
    void SetUser(long? idUsuarioEcommerce);
    void SetAdminUser(long? idAdminUsuario);
}
