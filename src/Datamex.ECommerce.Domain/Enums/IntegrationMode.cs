namespace Datamex.ECommerce.Domain.Enums;

/// <summary>
/// Integrated: dual-backoffice — ERP Site (área ECommerce) y admin Angular
/// escriben; ecom-etl replica en ambos sentidos. El stock lo manda el ERP.
/// Standalone: opera solo; schema espejo del ERP.
/// External: adapter hacia otro ERP/sistema.
/// </summary>
public enum IntegrationMode
{
    Integrated = 0,
    Standalone = 1,
    External = 2
}

public enum CanalVenta
{
    B2C = 0,
    B2B = 1
}
