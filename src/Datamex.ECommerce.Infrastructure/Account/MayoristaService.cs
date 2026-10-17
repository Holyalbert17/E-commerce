using Datamex.ECommerce.Application.Account;
using Datamex.ECommerce.Application.Notifications;
using Datamex.ECommerce.Domain.Entities;
using Datamex.ECommerce.Infrastructure.Persistence;
using Datamex.ECommerce.Infrastructure.Pricing;
using Microsoft.EntityFrameworkCore;

namespace Datamex.ECommerce.Infrastructure.Account;

public sealed class MayoristaService : IMayoristaService
{
    private readonly EcomDbContext _db;
    private readonly IEmailSender _email;

    public MayoristaService(EcomDbContext db, IEmailSender email)
    {
        _db = db;
        _email = email;
    }

    public async Task EvaluateAfterPurchaseAsync(long idEmpresa, long? idCuenta, CancellationToken ct = default)
    {
        if (!idCuenta.HasValue) return;

        var config = await _db.EmpresaConfiguracionesEcommerce
            .FirstOrDefaultAsync(c => c.ID == idEmpresa, ct);
        if (config is null || config.MontoUmbralMayorista <= 0)
            return;

        var userIds = await _db.UsuariosEcommerceCuenta.AsNoTracking()
            .Where(x => x.IDEmpresa == idEmpresa && x.IDCuenta == idCuenta.Value && x.Activo)
            .Select(x => x.IDUsuarioEcommerce)
            .Distinct()
            .ToListAsync(ct);

        var total = await _db.CarritosCompra.AsNoTracking()
            .Where(c => c.IDEmpresa == idEmpresa
                        && c.IDCuenta == idCuenta.Value
                        && c.Estado != EstadoCarrito.Abierto
                        && c.Activo)
            .SumAsync(c => (decimal?)c.MontoTotal, ct) ?? 0m;

        foreach (var uid in userIds)
        {
            var user = await _db.UsuariosEcommerce
                .FirstOrDefaultAsync(u => u.ID == uid && u.IDEmpresa == idEmpresa, ct);
            if (user is null) continue;

            user.MontoComprasAcumulado = total;
            if (user.EstadoMayorista != "Aprobado" && total >= config.MontoUmbralMayorista)
            {
                await ApproveInternalAsync(user, config, null, ct);
                await NotifyMayoristaAsync(user, true, "Se habilitó por volumen de compras.", ct);
            }
            else
                user.UltModificacion = DateTime.UtcNow;
        }

        await _db.SaveChangesAsync(ct);
    }

    public async Task ApproveAsync(long idUsuarioEcommerce, long? idListaPrecio, CancellationToken ct = default)
    {
        var user = await _db.UsuariosEcommerce
            .FirstOrDefaultAsync(u => u.ID == idUsuarioEcommerce, ct)
            ?? throw new InvalidOperationException("Usuario no encontrado.");

        var config = await _db.EmpresaConfiguracionesEcommerce
            .FirstAsync(c => c.ID == user.IDEmpresa, ct);

        await ApproveInternalAsync(user, config, idListaPrecio, ct);
        await _db.SaveChangesAsync(ct);
        await NotifyMayoristaAsync(user, true, "Tu solicitud de precio mayorista fue aprobada.", ct);
    }

    public async Task RejectAsync(long idUsuarioEcommerce, CancellationToken ct = default)
    {
        var user = await _db.UsuariosEcommerce
            .FirstOrDefaultAsync(u => u.ID == idUsuarioEcommerce, ct)
            ?? throw new InvalidOperationException("Usuario no encontrado.");

        user.EstadoMayorista = "Rechazado";
        user.CanalPreferido = "B2C";
        user.UltModificacion = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);
        await NotifyMayoristaAsync(user, false, "Tu solicitud de mayorista fue rechazada. Podés volver a solicitarla desde Mi cuenta.", ct);
    }

    private async Task NotifyMayoristaAsync(UsuarioEcommerce user, bool aprobado, string mensaje, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(user.Email)) return;
        await _email.SendMayoristaDecisionAsync(new MayoristaDecisionEmail(
            user.Email,
            $"{user.Nombre} {user.Apellido}".Trim(),
            aprobado,
            mensaje), ct);
    }

    private async Task ApproveInternalAsync(
        UsuarioEcommerce user,
        EmpresaConfiguracionEcommerce config,
        long? idListaPrecio,
        CancellationToken ct)
    {
        user.EstadoMayorista = "Aprobado";
        user.CanalPreferido = "B2B";
        user.UltModificacion = DateTime.UtcNow;

        var link = await _db.UsuariosEcommerceCuenta
            .Where(x => x.IDUsuarioEcommerce == user.ID && x.IDEmpresa == user.IDEmpresa && x.Activo)
            .OrderByDescending(x => x.EsPrincipal)
            .FirstOrDefaultAsync(ct);

        if (link is null) return;

        var cuenta = await _db.Cuentas
            .FirstOrDefaultAsync(c => c.ID == link.IDCuenta && c.IDEmpresa == user.IDEmpresa, ct);
        if (cuenta is null) return;

        var listaId = idListaPrecio
                      ?? config.IDListaPrecioMayoristaDefault
                      ?? await _db.ListasPrecio.AsNoTracking()
                          .Where(l => l.IDEmpresa == user.IDEmpresa
                                      && l.Activo
                                      && l.EsListaPrecioEcommerce
                                      && l.Estado == 2
                                      && (l.CanalEcommerce == "B2B" || l.CanalEcommerce == "BOTH"))
                          .OrderBy(l => l.NroListaPrecio)
                          .Select(l => (long?)l.ID)
                          .FirstOrDefaultAsync(ct);

        if (listaId.HasValue && !cuenta.IDListaPrecio.HasValue)
        {
            await ListaPrecioCuentaHelper.EnsureAssignmentAsync(
                _db, user.IDEmpresa, cuenta.ID, listaId.Value, "mayorista", ct);
        }
    }
}
