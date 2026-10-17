using Datamex.ECommerce.Application.Sync;

namespace Datamex.ECommerce.Application.Admin;

public record AdminEmpresaConfigDto(
    long IdEmpresa,
    string RazonSocial,
    string? NombreFantasia,
    string? NumeroDocumento,
    string? SlugTienda,
    string? DominioCustom,
    bool RealizaDelivery,
    bool RealizaPickup,
    bool CobraEnLaEntrega,
    bool CobraAlHacerPedido,
    decimal MontoPedidoMinimo,
    bool ProcesaPedidosPorPlanilla,
    bool ProcesaPedidosERP,
    bool ControlaStockEnCarrito,
    /// <summary>Integrated | Standalone | External</summary>
    string ModoIntegracion,
    long IDDeposito,
    long? IDArticuloDelivery,
    string? DireccionURLFacebook,
    string? DireccionURLTwitter,
    string? DireccionURLInstagram,
    string? DireccionURLYoutube,
    string? DireccionURLWhatsapp,
    string? ClavePublicaProcesadoraTarjeta = null,
    bool TieneClavePrivada = false,
    string? DireccionURLCheckout = null,
    long? CodigoComercio = null,
    long? CodigoSucursal = null,
    decimal MontoUmbralMayorista = 0,
    bool UsarListaPrecioCuentaErp = false,
    long? IDListaPrecioMayoristaDefault = null,
    // —— Paridad ERP maestro empresa ——
    int CodEmpresa = 0,
    string? DireccionEmpresa = null,
    string? Telefono = null,
    string? Fax = null,
    string? EMail = null,
    string? RepresentanteLegal = null,
    long? IDPais = null,
    string? LogoEmpresa = null,
    // —— Paridad ERP config ecom ——
    string? TiempoAproxEntregaPickup = null,
    string? TiempoAproxEntregaDelivery = null,
    string? NombreServidorCorreoElectronico = null,
    string? NombreUsuarioCorreoElectronico = null,
    bool TienePasswordCorreo = false,
    long? PuertoCorreoElectronico = null,
    long NroUltPedidoCarritoCompra = 0,
    long UltIDProcesoNegocio = 0,
    IReadOnlyList<AdminEmpresaRedSocialDto>? RedesSociales = null,
    string? ContenidoFaqHtml = null,
    string? ContenidoTerminosHtml = null,
    string? ContenidoComoComprarHtml = null,
    bool DeliveryEmiteFactura = true,
    bool DeliveryEmiteTicket = false,
    bool PickupEmiteFactura = true,
    bool PickupEmiteTicket = false,
    string? CreditoDesarrolladoPor = null,
    string? CreditoDesarrolladoPorUrl = null,
    /// <summary>Pull periódico de stock ERP → espejo local.</summary>
    bool SyncStockContinuo = true,
    /// <summary>Write-back PedidoMovil al confirmar el pedido.</summary>
    bool MigrarPedidosAlGenerar = true,
    /// <summary>Intervalo minutos lectura stock (1–120).</summary>
    int SyncStockIntervaloMinutos = 5,
    /// <summary>Host ERP Postgres (override ConnectionStrings:Erp).</summary>
    string? ErpHost = null,
    int? ErpPort = null,
    string? ErpDatabase = null,
    string? ErpUsername = null,
    bool TieneErpPassword = false,
    string? ErpSslMode = null,
    /// <summary>Permite write-back real a ERP para esta empresa.</summary>
    bool ErpAllowWriteBack = false,
    bool ErpConnectionConfigured = false,
    /// <summary>Tenant | Global | None</summary>
    string ErpConnectionSource = "None",
    /// <summary>Write-back efectivo (flag empresa o global).</summary>
    bool ErpWriteBackEffective = false);

public record AdminErpHealthDto(
    bool HasConnection,
    string ConnectionSource,
    bool WriteBackEffective,
    bool SyncStockContinuo,
    int SyncStockIntervaloMinutos,
    bool MigrarPedidosAlGenerar,
    bool ProcesaPedidosERP,
    string ModoIntegracion,
    long IdDeposito,
    DateTime? LastStockSyncUtc,
    string? LastStockStatus,
    int LastStockRows,
    string? LastStockError,
    int PendingOrdersWriteBack,
    string? Hint,
    DateTime? NextStockDueUtc = null,
    int? SecondsUntilNextStock = null,
    bool StockDueNow = false);

public record AdminEmpresaRedSocialDto(
    long Id,
    string NombreRedSocial,
    string DireccionURLRedSocial,
    string LogoRedSocial,
    bool Activo);

public record UpdateEmpresaConfigRequest(
    string? RazonSocial = null,
    string? NombreFantasia = null,
    string? NumeroDocumento = null,
    string? SlugTienda = null,
    string? DominioCustom = null,
    bool? RealizaDelivery = null,
    bool? RealizaPickup = null,
    bool? CobraEnLaEntrega = null,
    bool? CobraAlHacerPedido = null,
    decimal? MontoPedidoMinimo = null,
    bool? ProcesaPedidosPorPlanilla = null,
    bool? ProcesaPedidosERP = null,
    bool? ControlaStockEnCarrito = null,
    /// <summary>Integrated | Standalone | External</summary>
    string? ModoIntegracion = null,
    long? IDDeposito = null,
    long? IDArticuloDelivery = null,
    string? DireccionURLFacebook = null,
    string? DireccionURLTwitter = null,
    string? DireccionURLInstagram = null,
    string? DireccionURLYoutube = null,
    string? DireccionURLWhatsapp = null,
    string? ClavePublicaProcesadoraTarjeta = null,
    string? ClavePrivadaProcesadoraTarjeta = null,
    string? DireccionURLCheckout = null,
    long? CodigoComercio = null,
    long? CodigoSucursal = null,
    decimal? MontoUmbralMayorista = null,
    bool? UsarListaPrecioCuentaErp = null,
    long? IDListaPrecioMayoristaDefault = null,
    int? CodEmpresa = null,
    string? DireccionEmpresa = null,
    string? Telefono = null,
    string? Fax = null,
    string? EMail = null,
    string? RepresentanteLegal = null,
    long? IDPais = null,
    string? LogoEmpresa = null,
    string? TiempoAproxEntregaPickup = null,
    string? TiempoAproxEntregaDelivery = null,
    string? NombreServidorCorreoElectronico = null,
    string? NombreUsuarioCorreoElectronico = null,
    string? PasswordCorreoElectronico = null,
    long? PuertoCorreoElectronico = null,
    string? ContenidoFaqHtml = null,
    string? ContenidoTerminosHtml = null,
    string? ContenidoComoComprarHtml = null,
    bool? DeliveryEmiteFactura = null,
    bool? DeliveryEmiteTicket = null,
    bool? PickupEmiteFactura = null,
    bool? PickupEmiteTicket = null,
    string? CreditoDesarrolladoPor = null,
    string? CreditoDesarrolladoPorUrl = null,
    bool? SyncStockContinuo = null,
    bool? MigrarPedidosAlGenerar = null,
    int? SyncStockIntervaloMinutos = null,
    string? ErpHost = null,
    int? ErpPort = null,
    string? ErpDatabase = null,
    string? ErpUsername = null,
    /// <summary>Si viene vacío no cambia; si viene texto se cifra y guarda.</summary>
    string? ErpPassword = null,
    string? ErpSslMode = null,
    bool? ErpAllowWriteBack = null,
    /// <summary>Si true, limpia password ERP guardado.</summary>
    bool? ClearErpPassword = null,
    /// <summary>Limpia host/db/user/password/ssl (vuelve al fallback global).</summary>
    bool? ClearErpConnection = null,
    /// <summary>One-shot: Integrated + stock 1min + migrar + write-back + procesa ERP.</summary>
    bool? ActivarIntegracionErpCompleta = null);

public record AdminAgenciaDto(
    long Id,
    int CodAgencia,
    string DescripcionAgencia,
    string? Direccion,
    long? IDCiudad,
    long? IDDeposito,
    decimal? Latitud,
    decimal? Longitud,
    bool EsSucursalEcommerce,
    bool Activo);

public record UpsertAgenciaRequest(
    int? CodAgencia,
    string DescripcionAgencia,
    string? Direccion = null,
    long? IDCiudad = null,
    long? IDDeposito = null,
    decimal? Latitud = null,
    decimal? Longitud = null,
    bool EsSucursalEcommerce = true,
    bool Activo = true);

public record AdminHorarioDto(
    long Id,
    long IDAgencia,
    string? NombreAgencia,
    byte DiaSemana,
    string HoraPrimeraApertura,
    string HoraPrimerCierre,
    string HoraSegundaApertura,
    string HoraSegundoCierre,
    bool Activo);

public record UpsertHorarioRequest(
    long IDAgencia,
    byte DiaSemana,
    string HoraPrimeraApertura,
    string HoraPrimerCierre,
    string? HoraSegundaApertura = null,
    string? HoraSegundoCierre = null,
    bool Activo = true);

public record AdminDepositoDto(long Id, int Codigo, string Descripcion, bool EsDepositoEcommerce);

public interface IAdminEmpresaService
{
    Task<AdminEmpresaConfigDto?> GetConfigAsync(CancellationToken ct = default);
    Task<AdminEmpresaConfigDto> UpdateConfigAsync(UpdateEmpresaConfigRequest request, CancellationToken ct = default);
    Task<ErpConnectionTestResult> TestErpConnectionAsync(
        TestErpConnectionRequest? draft = null, CancellationToken ct = default);
    Task<AdminErpHealthDto> GetErpHealthAsync(CancellationToken ct = default);
    Task<MigrarPedidosResult> FlushPendingOrdersAsync(CancellationToken ct = default);

    Task<IReadOnlyList<AdminDepositoDto>> ListDepositosAsync(CancellationToken ct = default);
    Task<IReadOnlyList<AdminAgenciaDto>> ListAgenciasAsync(CancellationToken ct = default);
    Task<AdminAgenciaDto> CreateAgenciaAsync(UpsertAgenciaRequest request, CancellationToken ct = default);
    Task<AdminAgenciaDto> UpdateAgenciaAsync(long id, UpsertAgenciaRequest request, CancellationToken ct = default);
    Task DeleteAgenciaAsync(long id, CancellationToken ct = default);

    Task<IReadOnlyList<AdminHorarioDto>> ListHorariosAsync(long? idAgencia = null, CancellationToken ct = default);
    Task<AdminHorarioDto> CreateHorarioAsync(UpsertHorarioRequest request, CancellationToken ct = default);
    Task<AdminHorarioDto> UpdateHorarioAsync(long id, UpsertHorarioRequest request, CancellationToken ct = default);
    Task DeleteHorarioAsync(long id, CancellationToken ct = default);
}
