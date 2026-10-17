using Datamex.ECommerce.Application.Abstractions;
using Datamex.ECommerce.Application.Admin;
using Datamex.ECommerce.Domain.Entities;
using Datamex.ECommerce.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Datamex.ECommerce.Infrastructure.Admin;

public sealed class AdminChoferService : IAdminChoferService
{
    private readonly EcomDbContext _db;
    private readonly ITenantContext _tenant;

    public AdminChoferService(EcomDbContext db, ITenantContext tenant)
    {
        _db = db;
        _tenant = tenant;
    }

    private long EmpresaId => _tenant.IDEmpresa
        ?? throw new InvalidOperationException("Tenant no resuelto.");

    public async Task<IReadOnlyList<AdminChoferDto>> ListAsync(CancellationToken ct = default)
    {
        var idEmpresa = EmpresaId;
        return await _db.ChoferesEcommerce.AsNoTracking()
            .Where(c => c.IDEmpresa == idEmpresa)
            .OrderBy(c => c.NroChofer)
            .Select(c => new AdminChoferDto(
                c.ID, c.NroChofer, c.Nombre, c.Documento, c.Telefono, c.Observacion, c.Activo))
            .ToListAsync(ct);
    }

    public async Task<AdminChoferDto> UpsertAsync(long? id, UpsertChoferRequest request, CancellationToken ct = default)
    {
        var idEmpresa = EmpresaId;
        if (string.IsNullOrWhiteSpace(request.Nombre))
            throw new ArgumentException("Nombre del chofer requerido.");

        ChoferEcommerce row;
        if (id is > 0)
        {
            row = await _db.ChoferesEcommerce.FirstOrDefaultAsync(c => c.ID == id && c.IDEmpresa == idEmpresa, ct)
                ?? throw new InvalidOperationException("Chofer no encontrado.");
        }
        else
        {
            var next = await _db.ChoferesEcommerce.Where(c => c.IDEmpresa == idEmpresa)
                .Select(c => (int?)c.NroChofer).MaxAsync(ct) ?? 0;
            row = new ChoferEcommerce
            {
                IDEmpresa = idEmpresa,
                NroChofer = request.NroChofer > 0 ? request.NroChofer : next + 1,
                Activo = true,
                UserName = "admin"
            };
            _db.ChoferesEcommerce.Add(row);
        }

        row.Nombre = request.Nombre.Trim();
        row.Documento = string.IsNullOrWhiteSpace(request.Documento) ? null : request.Documento.Trim();
        row.Telefono = string.IsNullOrWhiteSpace(request.Telefono) ? null : request.Telefono.Trim();
        row.Observacion = string.IsNullOrWhiteSpace(request.Observacion) ? null : request.Observacion.Trim();
        if (request.NroChofer > 0) row.NroChofer = request.NroChofer;
        if (request.Activo.HasValue) row.Activo = request.Activo.Value;
        row.UltModificacion = DateTime.UtcNow;
        row.UserName = "admin";
        await _db.SaveChangesAsync(ct);
        return new AdminChoferDto(row.ID, row.NroChofer, row.Nombre, row.Documento, row.Telefono, row.Observacion, row.Activo);
    }

    public async Task DeleteAsync(long id, CancellationToken ct = default)
    {
        var idEmpresa = EmpresaId;
        var row = await _db.ChoferesEcommerce.FirstOrDefaultAsync(c => c.ID == id && c.IDEmpresa == idEmpresa, ct)
            ?? throw new InvalidOperationException("Chofer no encontrado.");
        row.Activo = false;
        row.UltModificacion = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);
    }

    public async Task AsignarPedidoAsync(long idPedidoMovil, long? idChofer, CancellationToken ct = default)
    {
        var idEmpresa = EmpresaId;
        var pedido = await _db.PedidosMovil.FirstOrDefaultAsync(p => p.ID == idPedidoMovil && p.IDEmpresa == idEmpresa, ct)
            ?? throw new InvalidOperationException("Pedido no encontrado.");

        string? nombreChofer = null;
        if (idChofer is > 0)
        {
            var ch = await _db.ChoferesEcommerce.AsNoTracking()
                .FirstOrDefaultAsync(c => c.ID == idChofer && c.IDEmpresa == idEmpresa && c.Activo, ct)
                ?? throw new InvalidOperationException("Chofer no encontrado o inactivo.");
            pedido.IDChofer = ch.ID;
            nombreChofer = ch.Nombre;
        }
        else
        {
            pedido.IDChofer = null;
        }

        pedido.UltModificacion = DateTime.UtcNow;
        pedido.UserName = "admin";

        if (idChofer is > 0)
        {
            _db.SeguimientosPedidoEcommerce.Add(new SeguimientoPedidoEcommerce
            {
                IDEmpresa = idEmpresa,
                IDPedidoMovil = pedido.ID,
                Estado = "en_transito",
                Mensaje = $"Asignado a chofer {nombreChofer}",
                CodigoTracking = $"ECO-{pedido.NroPedido}",
                FechaEstado = DateTime.UtcNow,
                Activo = true,
                UltModificacion = DateTime.UtcNow,
                UserName = "admin"
            });
        }

        await _db.SaveChangesAsync(ct);
    }
}
