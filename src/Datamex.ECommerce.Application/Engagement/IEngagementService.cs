namespace Datamex.ECommerce.Application.Engagement;

public record TrackInteraccionRequest(
    long IdArticulo,
    string TipoInteraccion,
    string Canal = "B2C",
    string? IdSesion = null);

public record RecomendacionDto(
    long Id,
    string? Sku,
    string Nombre,
    string? ImagenUrl,
    decimal? PrecioDesde,
    double? PromedioCalificacion,
    int TotalResenas,
    string Motivo);

public record CuponDto(
    long Id,
    string Codigo,
    string Descripcion,
    string TipoDescuento,
    decimal ValorDescuento,
    decimal MontoMinimo,
    decimal? MontoMaximoDescuento,
    string CanalEcommerce,
    DateTime FechaInicio,
    DateTime FechaFin,
    int? UsosMaximos,
    int UsosActuales,
    bool Activo);

public record CrearCuponRequest(
    string Codigo,
    string Descripcion,
    string TipoDescuento,
    decimal ValorDescuento,
    decimal MontoMinimo = 0,
    decimal? MontoMaximoDescuento = null,
    string CanalEcommerce = "ALL",
    DateTime? FechaInicio = null,
    DateTime? FechaFin = null,
    int? UsosMaximos = null,
    int UsosPorUsuario = 1);

public record AplicarCuponResult(bool Ok, string Mensaje, decimal MontoDescuento, string? Codigo);

public record SeguimientoDto(
    long Id,
    long IdPedidoMovil,
    string Estado,
    string? Mensaje,
    DateTime FechaEstado,
    string? CodigoTracking);

public record TrackingPublicoDto(
    long NroPedido,
    string EstadoActual,
    string? MensajeActual,
    DateTime? FechaEstadoActual,
    IReadOnlyList<SeguimientoDto> Eventos);

public record DevolucionDto(
    long Id,
    long IdPedidoMovil,
    long? IdArticulo,
    string Motivo,
    string? Detalle,
    string Estado,
    DateTime FechaSolicitud,
    long? NroPedido = null,
    string? Email = null,
    string? ArticuloNombre = null,
    string? UrlAdjunto = null,
    long? IdNotaCreditoErp = null,
    long? NroNotaCredito = null,
    bool PuedeDescargarNotaCredito = false,
    string? ErpMensaje = null);

public record CrearDevolucionRequest(long IdPedidoMovil, long? IdArticulo, string Motivo, string? Detalle);

public record ChatAdjuntoDto(string? Url, string? MimeType, string? NombreArchivo);

public record ChatMensajeDto(
    long Id,
    long? IdConversacion,
    long? IdPedidoMovil,
    string Email,
    string Origen,
    string TipoMensaje,
    string Mensaje,
    DateTime FechaMensaje,
    bool Leido,
    ChatAdjuntoDto? Adjunto = null,
    string? NombreAutor = null);

public record ChatConversacionDto(
    long Id,
    long IdUsuarioEcommerce,
    string Email,
    string? NombreCliente,
    string Asunto,
    string Estado,
    long? IdPedidoMovil,
    long? IdAdminAsignado,
    string? NombreAdminAsignado,
    DateTime FechaInicio,
    DateTime FechaUltimoMensaje,
    string? PreviewUltimoMensaje,
    int NoLeidosCliente,
    int NoLeidosSoporte);

public record EnviarChatRequest(
    string? Mensaje = null,
    long? IdPedidoMovil = null,
    string Asunto = "consulta",
    long? IdConversacion = null,
    bool ForzarNueva = false);

public record AdminResponderChatRequest(string? Mensaje = null, string? Estado = null);

public record SeguirProductoDto(
    long IdArticulo,
    string Nombre,
    string? ImagenUrl,
    DateTime FechaSeguimiento,
    string? Sku = null,
    decimal? PrecioDesde = null,
    bool StockOk = true,
    decimal? StockLibre = null);

public interface IEngagementService
{
    Task TrackAsync(TrackInteraccionRequest request, long? idUsuario, string? email, CancellationToken ct = default);
    Task<IReadOnlyList<RecomendacionDto>> GetRecomendacionesAsync(long? idArticulo, string canal = "B2C", int take = 8, CancellationToken ct = default);

    Task<IReadOnlyList<CuponDto>> AdminListCuponesAsync(CancellationToken ct = default);
    Task<CuponDto> AdminCrearCuponAsync(CrearCuponRequest request, CancellationToken ct = default);
    Task AdminToggleCuponAsync(long id, bool activo, CancellationToken ct = default);
    Task<AplicarCuponResult> AplicarCuponAsync(string sessionId, string codigo, string canal, long? idUsuario, CancellationToken ct = default);
    Task QuitarCuponAsync(string sessionId, CancellationToken ct = default);

    Task SeguirProductoAsync(long idUsuario, long idArticulo, CancellationToken ct = default);
    Task DejarDeSeguirAsync(long idUsuario, long idArticulo, CancellationToken ct = default);
    Task<IReadOnlyList<SeguirProductoDto>> ListSeguidosAsync(long idUsuario, CancellationToken ct = default);
    Task<bool> EstaSeguidoAsync(long idUsuario, long idArticulo, CancellationToken ct = default);

    Task<IReadOnlyList<SeguimientoDto>> GetTrackingAsync(long idPedidoMovil, CancellationToken ct = default);
    Task AdminAddTrackingAsync(long idPedidoMovil, string estado, string? mensaje, string? codigoTracking, CancellationToken ct = default);
    Task<string> EnsureTrackingShareTokenAsync(long idUsuario, long idPedidoMovil, CancellationToken ct = default);
    Task<TrackingPublicoDto?> GetTrackingByShareTokenAsync(string token, CancellationToken ct = default);

    Task<DevolucionDto> CrearDevolucionAsync(long idUsuario, CrearDevolucionRequest request, CancellationToken ct = default);
    Task<DevolucionDto> CrearDevolucionConAdjuntoAsync(
        long idUsuario,
        CrearDevolucionRequest request,
        Stream file,
        string contentType,
        string fileName,
        CancellationToken ct = default);
    Task<IReadOnlyList<DevolucionDto>> MisDevolucionesAsync(long idUsuario, CancellationToken ct = default);
    Task<IReadOnlyList<DevolucionDto>> AdminDevolucionesAsync(CancellationToken ct = default);
    Task AdminUpdateDevolucionAsync(long id, string estado, CancellationToken ct = default);

    Task<ChatConversacionDto> GetOrCreateMiConversacionAsync(long idUsuario, string asunto = "consulta", long? idPedidoMovil = null, CancellationToken ct = default);
    Task<IReadOnlyList<ChatConversacionDto>> MisConversacionesAsync(long idUsuario, CancellationToken ct = default);
    Task<ChatMensajeDto> EnviarChatAsync(long idUsuario, EnviarChatRequest request, CancellationToken ct = default);
    Task<ChatMensajeDto> EnviarChatConAdjuntoAsync(
        long idUsuario, Stream file, string contentType, string fileName,
        string? mensaje, string asunto, long? idConversacion, long? idPedidoMovil,
        bool forzarNueva = false, CancellationToken ct = default);
    Task<IReadOnlyList<ChatMensajeDto>> MisChatsAsync(long idUsuario, long? idPedidoMovil, long? idConversacion = null, CancellationToken ct = default);
    Task MarcarLeidosClienteAsync(long idUsuario, long idConversacion, CancellationToken ct = default);
    Task CerrarConversacionClienteAsync(long idUsuario, long idConversacion, CancellationToken ct = default);

    Task<IReadOnlyList<ChatConversacionDto>> AdminListConversacionesAsync(string? estado = null, string? q = null, CancellationToken ct = default);
    Task<IReadOnlyList<ChatMensajeDto>> AdminGetConversacionMensajesAsync(long idConversacion, CancellationToken ct = default);
    Task<ChatMensajeDto> AdminResponderConversacionAsync(
        long idConversacion, string? mensaje, long? idAdmin, string? nombreAdmin, string? nuevoEstado, CancellationToken ct = default);
    Task<ChatMensajeDto> AdminResponderConAdjuntoAsync(
        long idConversacion, Stream file, string contentType, string fileName,
        string? mensaje, long? idAdmin, string? nombreAdmin, CancellationToken ct = default);
    Task AdminAsignarConversacionAsync(long idConversacion, long? idAdmin, string? nombreAdmin, CancellationToken ct = default);
    Task AdminCambiarEstadoConversacionAsync(long idConversacion, string estado, CancellationToken ct = default);

    /// <summary>Compat: lista plana de mensajes (legacy marketplace).</summary>
    Task<IReadOnlyList<ChatMensajeDto>> AdminChatsAsync(CancellationToken ct = default);
    Task AdminResponderChatAsync(long idMensajeHilo, string mensaje, CancellationToken ct = default);
}

/// <summary>Push en tiempo real (SignalR). NoOp si no hay hub registrado.</summary>
public interface IChatRealtimeNotifier
{
    Task NotifyMessageAsync(long idEmpresa, long idConversacion, ChatMensajeDto mensaje, CancellationToken ct = default);
}

/// <summary>Notifica hitos de tracking de pedido (reusa el mismo hub SignalR).</summary>
public interface ITrackingRealtimeNotifier
{
    Task NotifyTrackingAsync(long idEmpresa, long idPedidoMovil, SeguimientoDto evento, CancellationToken ct = default);
}

