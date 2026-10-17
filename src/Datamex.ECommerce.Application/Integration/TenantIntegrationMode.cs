using Datamex.ECommerce.Domain.Entities;
using Datamex.ECommerce.Domain.Enums;

namespace Datamex.ECommerce.Application.Integration;

/// <summary>
/// Resuelve IntegrationMode por tenant (empresa). El fallback global solo aplica
/// si la empresa aún no tiene modo explícito (legacy).
/// </summary>
public static class TenantIntegrationMode
{
    /// <summary>Standalone por defecto: demos locales mutan stock.</summary>
    public const int DefaultModo = (int)IntegrationMode.Standalone;

    public static IntegrationMode Resolve(EmpresaConfiguracionEcommerce? cfg, IntegrationMode globalFallback)
    {
        if (cfg is null)
            return globalFallback;

        // 0/1/2 válidos; valores fuera de rango → fallback
        var raw = cfg.ModoIntegracion;
        if (raw is >= 0 and <= 2)
            return (IntegrationMode)raw;

        return globalFallback;
    }

    /// <summary>
    /// Solo Standalone + ControlaStockEnCarrito mueve TotalExistencia/PedidoVenta local.
    /// Integrated/External: el stock lo refleja el sync ERP (o sistema externo).
    /// </summary>
    public static bool MutatesLocalStock(EmpresaConfiguracionEcommerce? cfg, IntegrationMode globalFallback)
    {
        if (cfg is null || !cfg.ControlaStockEnCarrito)
            return false;
        return Resolve(cfg, globalFallback) == IntegrationMode.Standalone;
    }

    public static bool CreatesLocalPedidoMovil(EmpresaConfiguracionEcommerce cfg, IntegrationMode globalFallback)
    {
        // Siempre crear PedidoMovil local para Mis pedidos / tracking.
        // El write-back ERP sigue controlado por ProcesaPedidosERP + AllowWriteBack.
        _ = cfg;
        _ = globalFallback;
        return true;
    }

    public static bool TryParse(string? value, out IntegrationMode mode)
    {
        mode = IntegrationMode.Standalone;
        if (string.IsNullOrWhiteSpace(value))
            return false;
        if (Enum.TryParse(value.Trim(), ignoreCase: true, out mode)
            && Enum.IsDefined(typeof(IntegrationMode), mode))
            return true;
        return false;
    }
}
