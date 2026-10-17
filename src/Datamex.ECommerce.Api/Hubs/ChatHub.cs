using Datamex.ECommerce.Application.Engagement;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace Datamex.ECommerce.Api.Hubs;

[Authorize]
public sealed class ChatHub : Hub
{
    public Task JoinEmpresa(long idEmpresa) =>
        Groups.AddToGroupAsync(Context.ConnectionId, EmpresaGroup(idEmpresa));

    public Task JoinConversacion(long idConversacion) =>
        Groups.AddToGroupAsync(Context.ConnectionId, ConversacionGroup(idConversacion));

    public Task LeaveConversacion(long idConversacion) =>
        Groups.RemoveFromGroupAsync(Context.ConnectionId, ConversacionGroup(idConversacion));

    public Task JoinPedido(long idPedidoMovil) =>
        Groups.AddToGroupAsync(Context.ConnectionId, PedidoGroup(idPedidoMovil));

    public Task LeavePedido(long idPedidoMovil) =>
        Groups.RemoveFromGroupAsync(Context.ConnectionId, PedidoGroup(idPedidoMovil));

    public static string EmpresaGroup(long idEmpresa) => $"empresa:{idEmpresa}";
    public static string ConversacionGroup(long idConversacion) => $"conv:{idConversacion}";
    public static string PedidoGroup(long idPedidoMovil) => $"pedido:{idPedidoMovil}";
}

public sealed class SignalRChatRealtimeNotifier : IChatRealtimeNotifier, ITrackingRealtimeNotifier
{
    private readonly IHubContext<ChatHub> _hub;

    public SignalRChatRealtimeNotifier(IHubContext<ChatHub> hub) => _hub = hub;

    public async Task NotifyMessageAsync(
        long idEmpresa, long idConversacion, ChatMensajeDto mensaje, CancellationToken ct = default)
    {
        await _hub.Clients.Group(ChatHub.ConversacionGroup(idConversacion))
            .SendAsync("mensaje", mensaje, ct);
        await _hub.Clients.Group(ChatHub.EmpresaGroup(idEmpresa))
            .SendAsync("conversacionUpdated", new { idConversacion, mensaje }, ct);
    }

    public async Task NotifyTrackingAsync(
        long idEmpresa, long idPedidoMovil, SeguimientoDto evento, CancellationToken ct = default)
    {
        await _hub.Clients.Group(ChatHub.PedidoGroup(idPedidoMovil))
            .SendAsync("trackingUpdated", evento, ct);
        await _hub.Clients.Group(ChatHub.EmpresaGroup(idEmpresa))
            .SendAsync("pedidoTrackingUpdated", new { idPedidoMovil, evento }, ct);
    }
}
