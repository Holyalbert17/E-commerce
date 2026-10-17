using Datamex.ECommerce.Domain.Enums;

namespace Datamex.ECommerce.Application.Provisioning;

/// <summary>
/// Alta de tienda/empresa. Ambas vías escriben las MISMAS tablas ERP
/// (empresa, empresaconfiguracionecommerce, deposito, tipodocumento, etc.).
/// </summary>
public enum ProvisioningSource
{
    /// <summary>Alta manual sin ERP (Standalone).</summary>
    Manual = 0,
    /// <summary>Provisiona con IDEmpresa del ERP (Integrated).</summary>
    FromErp = 1
}

public record ManualTenantRequest(
    string RazonSocial,
    string? NombreFantasia,
    string NumeroDocumento,
    string SlugTienda,
    string? DominioCustom = null,
    bool RealizaDelivery = true,
    bool RealizaPickup = true,
    bool ControlaStockEnCarrito = true,
    decimal MontoPedidoMinimo = 0,
    string? AdminUsuario = null,
    string? AdminPassword = null,
    string? AdminEmail = null);

/// <summary>
/// Payload espejo de datos que llegarían del ERP / ETL.
/// IDEmpresa obligatorio (PK ERP). No inventa IDs distintos.
/// </summary>
public record ErpTenantRequest(
    long IDEmpresa,
    string RazonSocial,
    string? NombreFantasia,
    string? NumeroDocumento,
    string SlugTienda,
    string? DominioCustom = null,
    long? IDDepositoErp = null,
    long? IDArticuloDeliveryErp = null,
    bool RealizaDelivery = true,
    bool RealizaPickup = true,
    bool ControlaStockEnCarrito = true,
    bool ProcesaPedidosERP = true,
    decimal MontoPedidoMinimo = 0,
    string? ClavePublicaProcesadoraTarjeta = null,
    string? ClavePrivadaProcesadoraTarjeta = null,
    string? DireccionURLCheckout = null,
    long? CodigoComercio = null,
    long? CodigoSucursal = null,
    bool CreateLocalDefaultsIfMissing = true);

public record TenantSummaryDto(
    long IDEmpresa,
    string RazonSocial,
    string? NombreFantasia,
    string? NumeroDocumento,
    string? SlugTienda,
    string? DominioCustom,
    bool Activo,
    bool ProcesaPedidosERP,
    bool RealizaDelivery,
    bool RealizaPickup,
    bool ControlaStockEnCarrito,
    long IDDeposito,
    long? IDArticuloDelivery,
    ProvisioningSource SourceHint,
    IntegrationMode RuntimeMode);

public record ProvisionResultDto(
    long IDEmpresa,
    string SlugTienda,
    ProvisioningSource Source,
    string Mensaje,
    string? AdminUsuario,
    string? AdminPasswordPlainDev);

public interface ITenantProvisioningService
{
    Task<IReadOnlyList<TenantSummaryDto>> ListAsync(CancellationToken ct = default);
    Task<TenantSummaryDto?> GetAsync(long idEmpresa, CancellationToken ct = default);

    /// <summary>Alta manual Standalone — genera IDEmpresa local, mismas tablas.</summary>
    Task<ProvisionResultDto> ProvisionManualAsync(ManualTenantRequest request, CancellationToken ct = default);

    /// <summary>Alta/actualización Integrated — usa IDEmpresa del ERP, mismas tablas.</summary>
    Task<ProvisionResultDto> ProvisionFromErpAsync(ErpTenantRequest request, CancellationToken ct = default);
}
