using Datamex.ECommerce.Domain.Entities;

namespace Datamex.ECommerce.Application.Sync;

/// <summary>
/// Alta de cuenta espejo en Postgres ERP (tabla cuenta) al activar registro e-com.
/// Solo aplica si la empresa tiene ProcesaPedidosERP y hay ConnectionStrings:Erp + AllowWriteBack.
/// Paridad ERP: Cliente+Cuenta nacen en activación; el PedidoMovil los reutiliza.
/// </summary>
public interface ICuentaErpWriter
{
    /// <returns>ID en ERP (suele coincidir con el local) o null si no se escribió.</returns>
    Task<long?> EnsureCuentaInErpAsync(Cuenta cuenta, UsuarioEcommerce user, CancellationToken ct = default);

    /// <summary>
    /// Resuelve cuenta+usuario local y asegura espejo ERP (antes de INSERT PedidoMovil).
    /// </summary>
    Task<long?> EnsureCuentaInErpByIdAsync(long idEmpresa, long idCuenta, CancellationToken ct = default);
}
