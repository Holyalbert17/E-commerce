using Datamex.ECommerce.Domain.Entities;

namespace Datamex.ECommerce.Application.Sync;

/// <summary>
/// Lookup e import de cliente ERP en el registro storefront (correo / RUC).
/// </summary>
public interface IRegistroClienteErpLookup
{
    Task<ErpClienteMatch> FindAsync(
        long idEmpresa,
        string email,
        string numeroDocumentoNormalizado,
        CancellationToken ct = default);

    /// <summary>Copia cuenta ERP → e-com con el mismo ID (datos de facturación).</summary>
    Task<Cuenta?> ImportCuentaAsync(long idEmpresa, long idCuentaErp, CancellationToken ct = default);
}

/// <param name="IdCuenta">Cuenta ERP a reutilizar, o null si no hay match.</param>
/// <param name="MatchedBy">email | ruc | ambos</param>
/// <param name="Error">Si email y RUC apuntan a clientes distintos.</param>
public sealed record ErpClienteMatch(long? IdCuenta, string? MatchedBy, string? Error = null)
{
    public static ErpClienteMatch None { get; } = new(null, null);
}
