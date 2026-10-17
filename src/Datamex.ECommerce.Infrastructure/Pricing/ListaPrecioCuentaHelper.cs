using Datamex.ECommerce.Domain.Entities;
using Datamex.ECommerce.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Datamex.ECommerce.Infrastructure.Pricing;

/// <summary>
/// Asignaciones listapreciocuenta (paridad ERP AsignarListaPrecioCuenta).
/// Una cuenta tiene a lo sumo una fila Activo=true vigente; el resto es historial.
/// </summary>
public static class ListaPrecioCuentaHelper
{
    /// <summary>
    /// Asegura fila activa cuenta↔lista; desactiva otras vigentes; sincroniza Cuenta.IDListaPrecio.
    /// Devuelve el ID de listapreciocuenta (FK de PedidoMovil).
    /// </summary>
    public static async Task<long> EnsureAssignmentAsync(
        EcomDbContext db,
        long idEmpresa,
        long idCuenta,
        long idListaPrecio,
        string? userName,
        CancellationToken ct = default)
    {
        if (idCuenta <= 0 || idListaPrecio <= 0)
            throw new ArgumentException("IDCuenta e IDListaPrecio requeridos.");

        var now = DateTime.UtcNow;
        var user = string.IsNullOrWhiteSpace(userName) ? "ecom" : userName.Trim();

        var vigentes = await db.ListasPrecioCuenta
            .Where(x => x.IDEmpresa == idEmpresa && x.IDCuenta == idCuenta && x.Activo)
            .ToListAsync(ct);

        var match = vigentes.FirstOrDefault(x => x.IDListaPrecio == idListaPrecio);
        foreach (var row in vigentes.Where(x => x.IDListaPrecio != idListaPrecio))
        {
            row.Activo = false;
            row.UltModificacion = now;
            row.UserName = user;
        }

        if (match is null)
        {
            match = new ListaPrecioCuenta
            {
                IDEmpresa = idEmpresa,
                IDCuenta = idCuenta,
                IDListaPrecio = idListaPrecio,
                Activo = true,
                UltModificacion = now,
                UserName = user,
            };
            db.ListasPrecioCuenta.Add(match);
        }
        else
        {
            match.Activo = true;
            match.UltModificacion = now;
            match.UserName = user;
        }

        var cuenta = await db.Cuentas
            .FirstOrDefaultAsync(c => c.ID == idCuenta && c.IDEmpresa == idEmpresa, ct);
        if (cuenta is not null)
        {
            cuenta.IDListaPrecio = idListaPrecio;
            cuenta.UltModificacion = now;
            cuenta.UserName = user;
        }

        await db.SaveChangesAsync(ct);
        return match.ID;
    }

    /// <summary>Lista vigente de la cuenta vía listapreciocuenta activa, o Cuenta.IDListaPrecio.</summary>
    public static async Task<long?> ResolveListaIdForCuentaAsync(
        EcomDbContext db,
        long idEmpresa,
        long idCuenta,
        CancellationToken ct = default)
    {
        var fromHist = await db.ListasPrecioCuenta.AsNoTracking()
            .Where(x => x.IDEmpresa == idEmpresa && x.IDCuenta == idCuenta && x.Activo)
            .OrderByDescending(x => x.UltModificacion)
            .Select(x => (long?)x.IDListaPrecio)
            .FirstOrDefaultAsync(ct);
        if (fromHist is > 0)
            return fromHist;

        return await db.Cuentas.AsNoTracking()
            .Where(c => c.ID == idCuenta && c.IDEmpresa == idEmpresa)
            .Select(c => c.IDListaPrecio)
            .FirstOrDefaultAsync(ct);
    }

    /// <summary>ID de listapreciocuenta activa para cuenta+lista (crea si hace falta).</summary>
    public static async Task<long?> ResolveOrEnsureAssignmentIdAsync(
        EcomDbContext db,
        long idEmpresa,
        long? idCuenta,
        long? idListaPrecio,
        string? userName,
        CancellationToken ct = default)
    {
        if (idCuenta is not > 0 || idListaPrecio is not > 0)
            return null;

        var existing = await db.ListasPrecioCuenta.AsNoTracking()
            .Where(x => x.IDEmpresa == idEmpresa
                        && x.IDCuenta == idCuenta.Value
                        && x.IDListaPrecio == idListaPrecio.Value
                        && x.Activo)
            .OrderByDescending(x => x.UltModificacion)
            .Select(x => (long?)x.ID)
            .FirstOrDefaultAsync(ct);
        if (existing is > 0)
            return existing;

        // Solo auto-crea si la cuenta ya apunta a esa lista (asignación implícita / sync parcial).
        var cuentaLista = await db.Cuentas.AsNoTracking()
            .Where(c => c.ID == idCuenta.Value && c.IDEmpresa == idEmpresa)
            .Select(c => c.IDListaPrecio)
            .FirstOrDefaultAsync(ct);
        if (cuentaLista != idListaPrecio)
            return null;

        return await EnsureAssignmentAsync(db, idEmpresa, idCuenta.Value, idListaPrecio.Value, userName, ct);
    }
}
