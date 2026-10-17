using Datamex.ECommerce.Domain.Entities;

namespace Datamex.ECommerce.Infrastructure.Orders;

/// <summary>Estados de fulfillment e-com (tracking), independientes de PedidoMovil.Estado ERP.</summary>
public static class FulfillmentEstado
{
    public const string CompraRealizada = "compra_realizada";
    public const string Preparando = "preparando";
    public const string Preparado = "preparado";
    public const string Facturado = "facturado";
    public const string EnTransito = "en_transito";
    public const string Entregado = "entregado";
    public const string Cancelado = "cancelado";
    public const string Demorado = "demorado";
    public const string Excepcion = "excepcion";

    private static readonly string[] Order =
    [
        CompraRealizada, Preparando, Preparado, Facturado, EnTransito, Entregado
    ];

    public static string Normalize(string? estado)
    {
        var e = (estado ?? Preparando).Trim().ToLowerInvariant();
        return e switch
        {
            "en_camino" => EnTransito,
            "despachado" => EnTransito,
            "en_reparto" => EnTransito,
            "delay" => Demorado,
            "exception" => Excepcion,
            _ => e
        };
    }

    public static int Rank(string estado)
    {
        var n = Normalize(estado);
        if (n == Cancelado) return 100;
        if (n is Demorado or Excepcion) return Array.IndexOf(Order, EnTransito); // paralelo a tránsito
        var i = Array.IndexOf(Order, n);
        return i < 0 ? -1 : i;
    }

    public static bool CanTransition(string? from, string to)
    {
        to = Normalize(to);
        var fromN = string.IsNullOrWhiteSpace(from) ? null : Normalize(from);

        if (to == Cancelado)
            return fromN is not (Entregado or Cancelado);

        if (fromN is Cancelado or Entregado)
            return false;

        // Excepciones: se pueden marcar desde preparación/tránsito y luego reanudar
        if (to is Demorado or Excepcion)
            return fromN is not null && fromN is not (Entregado or Cancelado);

        if (fromN is Demorado or Excepcion)
        {
            // Reanudar hacia tránsito/preparación/entregado/cancelado
            return to is Preparando or Preparado or Facturado or EnTransito or Entregado or Cancelado;
        }

        var fromR = fromN is null ? -1 : Rank(fromN);
        var toR = Rank(to);
        if (toR < 0) return false;
        if (fromR < 0) return toR <= 1; // inicio → compra/preparando
        return toR >= fromR && toR <= fromR + 2;
    }

    public static void ApplyPedidoSideEffect(PedidoMovil pedido, string estado)
    {
        var n = Normalize(estado);
        if (n == Cancelado && pedido.Estado != EstadoPedidoMovil.Anulado)
        {
            pedido.Estado = EstadoPedidoMovil.Anulado;
            pedido.UltModificacion = DateTime.UtcNow;
            pedido.UserName = "admin-tracking";
        }
    }
}
