using Datamex.ECommerce.Application.Engagement;

namespace Datamex.ECommerce.Infrastructure.Engagement;

public sealed class NullChatRealtimeNotifier : IChatRealtimeNotifier, ITrackingRealtimeNotifier
{
    public Task NotifyMessageAsync(
        long idEmpresa, long idConversacion, ChatMensajeDto mensaje, CancellationToken ct = default)
        => Task.CompletedTask;

    public Task NotifyTrackingAsync(
        long idEmpresa, long idPedidoMovil, SeguimientoDto evento, CancellationToken ct = default)
        => Task.CompletedTask;
}
