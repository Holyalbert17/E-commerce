using System.Data;
using Datamex.ECommerce.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace Datamex.ECommerce.Infrastructure.Cart;

/// <summary>
/// Reserva atómica de N° de pedido e-com (evita duplicados por carrera al abrir carrito).
/// </summary>
internal static class PedidoNumeroAllocator
{
    public static async Task<long> NextAsync(EcomDbContext db, long idEmpresa, CancellationToken ct)
    {
        var conn = db.Database.GetDbConnection();
        if (conn.State != ConnectionState.Open)
            await db.Database.OpenConnectionAsync(ct);

        await using var cmd = conn.CreateCommand();
        cmd.CommandText =
            """
            UPDATE dbo.empresaconfiguracionecommerce
            SET "NroUltPedidoCarritoCompra" = "NroUltPedidoCarritoCompra" + 1,
                "UltModificacion" = NOW() AT TIME ZONE 'utc'
            WHERE "ID" = @id
            RETURNING "NroUltPedidoCarritoCompra"
            """;

        var p = cmd.CreateParameter();
        p.ParameterName = "id";
        p.Value = idEmpresa;
        cmd.Parameters.Add(p);

        var currentTx = db.Database.CurrentTransaction?.GetDbTransaction();
        if (currentTx is not null)
            cmd.Transaction = currentTx;

        var scalar = await cmd.ExecuteScalarAsync(ct)
                     ?? throw new InvalidOperationException(
                         $"No se pudo reservar N° de pedido (empresa {idEmpresa}).");
        return Convert.ToInt64(scalar);
    }

    /// <summary>
    /// Si el N° del carrito ya está usado por otro carrito cerrado / PedidoMovil, reserva uno nuevo.
    /// </summary>
    public static async Task EnsureUniqueForCartAsync(
        EcomDbContext db, Domain.Entities.CarritoCompra cart, CancellationToken ct)
    {
        var idEmpresa = cart.IDEmpresa;
        var nro = cart.NroPedidoCarritoCompra;

        var takenByCart = await db.CarritosCompra.AsNoTracking()
            .AnyAsync(c => c.IDEmpresa == idEmpresa
                           && c.ID != cart.ID
                           && c.NroPedidoCarritoCompra == nro
                           && c.Estado != 0, ct);

        var takenByPedido = await db.PedidosMovil.AsNoTracking()
            .AnyAsync(p => p.IDEmpresa == idEmpresa
                           && p.NroPedido == nro
                           && p.OrigenPedido == Domain.Entities.OrigenPedidoMovil.Ecommerce
                           && (cart.IDPedidoMovil == null || p.ID != cart.IDPedidoMovil), ct);

        if (!takenByCart && !takenByPedido)
            return;

        var next = await NextAsync(db, idEmpresa, ct);
        cart.NroPedidoCarritoCompra = next;
        cart.UltModificacion = DateTime.UtcNow;
    }
}
