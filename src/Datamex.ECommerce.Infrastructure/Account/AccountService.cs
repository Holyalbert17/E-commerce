using Datamex.ECommerce.Application.Account;
using Datamex.ECommerce.Application.Abstractions;
using Datamex.ECommerce.Application.Integration;
using Datamex.ECommerce.Application.Inventory;
using Datamex.ECommerce.Application.Options;
using Datamex.ECommerce.Application.Sync;
using Datamex.ECommerce.Domain.Entities;
using Datamex.ECommerce.Domain.Enums;
using Datamex.ECommerce.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Datamex.ECommerce.Infrastructure.Account;

public sealed class AccountService : IAccountService
{
    private readonly EcomDbContext _db;
    private readonly ITenantContext _tenant;
    private readonly IInventoryService _inventory;
    private readonly ECommerceOptions _options;
    private readonly PentahoOptions _pentaho;
    private readonly IErpPedidoLifecycleWriter _erpLife;

    public AccountService(
        EcomDbContext db,
        ITenantContext tenant,
        IInventoryService inventory,
        IOptions<ECommerceOptions> options,
        IOptions<PentahoOptions> pentaho,
        IErpPedidoLifecycleWriter erpLife)
    {
        _db = db;
        _tenant = tenant;
        _inventory = inventory;
        _options = options.Value;
        _pentaho = pentaho.Value;
        _erpLife = erpLife;
    }

    public async Task<AccountProfileDto?> GetProfileAsync(long idUsuarioEcommerce, CancellationToken ct = default)
    {
        var idEmpresa = RequireTenant();
        var u = await _db.UsuariosEcommerce.AsNoTracking()
            .Include(x => x.UsuarioEcommerceCuentas)
            .FirstOrDefaultAsync(x => x.ID == idUsuarioEcommerce && x.IDEmpresa == idEmpresa, ct);
        if (u is null)
            return null;

        var cuentaLink = u.UsuarioEcommerceCuentas
            .OrderByDescending(c => c.EsPrincipal)
            .ThenBy(c => c.ID)
            .FirstOrDefault();

        Cuenta? cuenta = null;
        string? listaDesc = null;
        if (cuentaLink != null)
        {
            cuenta = await _db.Cuentas.AsNoTracking()
                .FirstOrDefaultAsync(c => c.ID == cuentaLink.IDCuenta && c.IDEmpresa == idEmpresa, ct);
            if (cuenta?.IDListaPrecio is long idLista)
            {
                listaDesc = await _db.ListasPrecio.AsNoTracking()
                    .Where(l => l.ID == idLista)
                    .Select(l => l.Descripcion)
                    .FirstOrDefaultAsync(ct);
            }
        }

        var config = await _db.EmpresaConfiguracionesEcommerce.AsNoTracking()
            .FirstOrDefaultAsync(c => c.ID == idEmpresa, ct);

        return new AccountProfileDto(
            u.ID, u.Usuario, u.Nombre, u.Apellido, u.Email,
            u.NumeroMovil ?? u.NumeroTelefono, u.NumeroDocumento,
            u.EstadoMayorista == "Aprobado" ? u.CanalPreferido : "B2C",
            cuentaLink?.IDCuenta,
            u.EstadoMayorista,
            u.MontoComprasAcumulado,
            config?.MontoUmbralMayorista ?? 0,
            cuenta?.IDListaPrecio,
            listaDesc);
    }

    public async Task<AccountProfileDto> UpdateProfileAsync(
        long idUsuarioEcommerce, UpdateProfileRequest request, CancellationToken ct = default)
    {
        var idEmpresa = RequireTenant();
        var u = await _db.UsuariosEcommerce
            .FirstOrDefaultAsync(x => x.ID == idUsuarioEcommerce && x.IDEmpresa == idEmpresa, ct)
            ?? throw new InvalidOperationException("Usuario no encontrado.");

        if (string.IsNullOrWhiteSpace(request.Nombre) || request.Nombre.Trim().Length < 2)
            throw new ArgumentException("Nombre inválido.");
        if (string.IsNullOrWhiteSpace(request.Apellido) || request.Apellido.Trim().Length < 2)
            throw new ArgumentException("Apellido inválido.");

        u.Nombre = request.Nombre.Trim();
        u.Apellido = request.Apellido.Trim();
        if (request.Telefono != null)
        {
            u.NumeroMovil = string.IsNullOrWhiteSpace(request.Telefono) ? null : request.Telefono.Trim();
            u.NumeroTelefono = u.NumeroMovil;
        }
        if (request.NumeroDocumento != null)
            u.NumeroDocumento = string.IsNullOrWhiteSpace(request.NumeroDocumento) ? null : request.NumeroDocumento.Trim();
        u.UltModificacion = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);
        return (await GetProfileAsync(idUsuarioEcommerce, ct))!;
    }

    public async Task<AccountProfileDto> SolicitarMayoristaAsync(
        long idUsuarioEcommerce, SolicitarMayoristaRequest request, CancellationToken ct = default)
    {
        var idEmpresa = RequireTenant();
        var u = await _db.UsuariosEcommerce
            .FirstOrDefaultAsync(x => x.ID == idUsuarioEcommerce && x.IDEmpresa == idEmpresa, ct)
            ?? throw new InvalidOperationException("Usuario no encontrado.");

        if (u.EstadoMayorista == "Aprobado")
            return (await GetProfileAsync(idUsuarioEcommerce, ct))!;

        u.EstadoMayorista = "Solicitado";
        u.FechaSolicitudMayorista = DateTime.UtcNow;
        u.MotivoSolicitudMayorista = string.IsNullOrWhiteSpace(request.Motivo)
            ? null
            : request.Motivo.Trim();
        u.UltModificacion = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);
        return (await GetProfileAsync(idUsuarioEcommerce, ct))!;
    }

    public async Task<IReadOnlyList<AccountOrderDto>> GetOrdersAsync(
        long idUsuarioEcommerce, CancellationToken ct = default)
    {
        var idEmpresa = RequireTenant();
        var cuentaIds = await _db.UsuariosEcommerceCuenta.AsNoTracking()
            .Where(c => c.IDEmpresa == idEmpresa && c.IDUsuarioEcommerce == idUsuarioEcommerce && c.Activo)
            .Select(c => c.IDCuenta)
            .ToListAsync(ct);

        if (cuentaIds.Count == 0)
            return Array.Empty<AccountOrderDto>();

        var carts = await _db.CarritosCompra.AsNoTracking()
            .Where(c => c.IDEmpresa == idEmpresa
                        && c.Estado != EstadoCarrito.Abierto
                        && c.IDCuenta != null
                        && cuentaIds.Contains(c.IDCuenta.Value))
            .OrderByDescending(c => c.FechaPedido)
            .ThenByDescending(c => c.ID)
            .Take(50)
            .ToListAsync(ct);

        var pedidoIds = carts.Where(c => c.IDPedidoMovil.HasValue)
            .Select(c => c.IDPedidoMovil!.Value)
            .Distinct()
            .ToList();

        var migradoMap = pedidoIds.Count == 0
            ? new Dictionary<long, bool>()
            : await _db.PedidosMovil.AsNoTracking()
                .Where(p => pedidoIds.Contains(p.ID))
                .ToDictionaryAsync(p => p.ID, p => p.Migrado, ct);

        var facturaMap = pedidoIds.Count == 0
            ? new Dictionary<long, long?>()
            : await _db.PedidosMovil.AsNoTracking()
                .Where(p => pedidoIds.Contains(p.ID))
                .ToDictionaryAsync(p => p.ID, p => p.NroFactura, ct);

        // Pedidos ya migrados al ERP: traer nrofactura fiscal si el ETL aún no actualizó el espejo.
        foreach (var pid in pedidoIds.Where(id =>
                     migradoMap.GetValueOrDefault(id)
                     && !FacturaSerie.EsFacturado(facturaMap.GetValueOrDefault(id))))
        {
            var sync = await _erpLife.SincronizarFacturaAsync(idEmpresa, pid, ct);
            if (sync.Ok && sync.NroFactura is > 0)
                facturaMap[pid] = sync.NroFactura;
        }

        var anuladoPedidoIds = pedidoIds.Count == 0
            ? new HashSet<long>()
            : (await _db.PedidosMovil.AsNoTracking()
                .Where(p => pedidoIds.Contains(p.ID) && p.Estado == EstadoPedidoMovil.Anulado)
                .Select(p => p.ID)
                .ToListAsync(ct)).ToHashSet();

        var canceladoSegIds = pedidoIds.Count == 0
            ? new HashSet<long>()
            : (await _db.SeguimientosPedidoEcommerce.AsNoTracking()
                .Where(s => s.IDEmpresa == idEmpresa
                            && pedidoIds.Contains(s.IDPedidoMovil)
                            && s.Activo
                            && s.Estado == "cancelado")
                .Select(s => s.IDPedidoMovil)
                .Distinct()
                .ToListAsync(ct)).ToHashSet();

        var ncByPm = pedidoIds.Count == 0
            ? new Dictionary<long, long>()
            : await _db.DevolucionesEcommerce.AsNoTracking()
                .Where(d => d.IDEmpresa == idEmpresa
                            && pedidoIds.Contains(d.IDPedidoMovil)
                            && d.IdNotaCreditoErp != null)
                .GroupBy(d => d.IDPedidoMovil)
                .Select(g => new { Pm = g.Key, IdNc = g.Max(x => x.IdNotaCreditoErp)!.Value })
                .ToDictionaryAsync(x => x.Pm, x => x.IdNc, ct);

        return carts.Select(c =>
        {
            var migrado = c.IDPedidoMovil.HasValue && migradoMap.GetValueOrDefault(c.IDPedidoMovil.Value);
            var nroFactura = c.IDPedidoMovil.HasValue
                ? facturaMap.GetValueOrDefault(c.IDPedidoMovil.Value)
                : null;
            var facturado = FacturaSerie.EsFacturado(nroFactura);
            var cancelado = c.Estado == EstadoCarrito.Anulado
                            || (c.IDPedidoMovil.HasValue && (
                                anuladoPedidoIds.Contains(c.IDPedidoMovil.Value)
                                || canceladoSegIds.Contains(c.IDPedidoMovil.Value)));
            long? idNc = c.IDPedidoMovil.HasValue
                ? ncByPm.GetValueOrDefault(c.IDPedidoMovil.Value) is long nc && nc > 0 ? nc : null
                : null;
            if (idNc is 0) idNc = null;
            var puedeNc = idNc is > 0 || (cancelado && facturado && migrado);
            // Pedido devuelto/anulado: no se reimprime la factura fiscal, sino la NC (media hoja).
            var puedeDescargar = facturado && migrado && !cancelado && idNc is not > 0;
            var puedeCancelar = !cancelado
                                && !facturado
                                && (!migrado || c.TipoEntrega == TipoEntregaEcommerce.Pickup);
            return new AccountOrderDto(
                c.ID,
                c.NroPedidoCarritoCompra,
                c.IDPedidoMovil,
                c.FechaPedido,
                c.RazonSocial,
                c.NumeroDocumento,
                c.TipoEntrega,
                c.TipoPago,
                c.TipoEntrega == TipoEntregaEcommerce.Delivery ? c.LugarEntrega : "Pickup / local",
                c.MontoTotal,
                c.Estado,
                migrado,
                cancelado,
                nroFactura,
                facturado,
                puedeDescargar,
                puedeCancelar,
                idNc,
                null,
                puedeNc);
        }).ToList();
    }

    public async Task CancelOrderAsync(
        long idUsuarioEcommerce, long nroPedido, string? motivo, CancellationToken ct = default)
    {
        var idEmpresa = RequireTenant();
        var cuentaIds = await _db.UsuariosEcommerceCuenta.AsNoTracking()
            .Where(c => c.IDEmpresa == idEmpresa && c.IDUsuarioEcommerce == idUsuarioEcommerce && c.Activo)
            .Select(c => c.IDCuenta)
            .ToListAsync(ct);

        if (cuentaIds.Count == 0)
            throw new InvalidOperationException("Pedido no encontrado.");

        var cart = await _db.CarritosCompra
            .FirstOrDefaultAsync(c => c.IDEmpresa == idEmpresa
                                      && c.NroPedidoCarritoCompra == nroPedido
                                      && c.IDCuenta != null
                                      && cuentaIds.Contains(c.IDCuenta.Value), ct)
            ?? throw new InvalidOperationException("Pedido no encontrado.");

        if (cart.Estado == EstadoCarrito.Abierto)
            throw new InvalidOperationException("El pedido aún está abierto.");

        if (cart.Estado == EstadoCarrito.Anulado)
            throw new InvalidOperationException("El pedido ya está cancelado.");

        PedidoMovil? pedido = null;
        if (cart.IDPedidoMovil.HasValue)
        {
            pedido = await _db.PedidosMovil
                .FirstOrDefaultAsync(p => p.ID == cart.IDPedidoMovil && p.IDEmpresa == idEmpresa, ct);

            if (pedido?.Estado == EstadoPedidoMovil.Anulado)
                throw new InvalidOperationException("El pedido ya está cancelado.");

            if (FacturaSerie.EsFiscalErp(pedido?.NroFactura))
                throw new InvalidOperationException(
                    "No se puede cancelar: ya hay factura fiscal en ERP. Pedí una devolución.");

            var esPickup = cart.TipoEntrega == TipoEntregaEcommerce.Pickup;
            if (pedido?.Migrado == true && !esPickup)
                throw new InvalidOperationException(
                    "No se puede cancelar un delivery ya migrado al ERP. Pedí una devolución.");

            var yaCancelado = await _db.SeguimientosPedidoEcommerce.AnyAsync(
                s => s.IDEmpresa == idEmpresa
                     && s.IDPedidoMovil == cart.IDPedidoMovil
                     && s.Activo
                     && s.Estado == "cancelado", ct);
            if (yaCancelado)
                throw new InvalidOperationException("El pedido ya está cancelado.");
        }

        var now = DateTime.UtcNow;
        cart.Estado = EstadoCarrito.Anulado;
        cart.UltModificacion = now;

        if (pedido != null)
        {
            // Si ya estaba en ERP (pickup migrado): anular allá primero.
            if (pedido.Migrado)
            {
                var erp = await _erpLife.AnularPedidoAsync(idEmpresa, pedido.ID, motivo, ct);
                if (!erp.Ok && !erp.Skipped)
                    throw new InvalidOperationException(erp.Mensaje);
            }

            pedido.Estado = EstadoPedidoMovil.Anulado;
            if (!string.IsNullOrWhiteSpace(motivo))
                pedido.Observacion = $"Cancelado cliente: {motivo.Trim()}";
            pedido.UltModificacion = now;

            _db.SeguimientosPedidoEcommerce.Add(new SeguimientoPedidoEcommerce
            {
                IDEmpresa = idEmpresa,
                IDPedidoMovil = pedido.ID,
                IDCarritoCompra = cart.ID,
                Estado = "cancelado",
                Mensaje = string.IsNullOrWhiteSpace(motivo) ? "Cancelado por el cliente" : motivo.Trim(),
                FechaEstado = now,
                Activo = true,
                UltModificacion = now,
                UserName = "cliente"
            });
        }

        // Standalone: devolver stock local. Integrated: lo maneja ERP (AnularPedidoAsync).
        var config = await _db.EmpresaConfiguracionesEcommerce.AsNoTracking()
            .FirstOrDefaultAsync(c => c.ID == idEmpresa, ct);
        if (TenantIntegrationMode.MutatesLocalStock(config, _options.IntegrationMode))
        {
            var lines = await _db.DetallesCarritoCompra.AsNoTracking()
                .Where(d => d.IDCarritoCompra == cart.ID && d.Activo)
                .Select(d => new StockLineMove(d.IDArticulo, d.Cantidad))
                .ToListAsync(ct);
            await _inventory.RestorePedidoStockAsync(
                cart.IDDeposito, lines, config!.IDArticuloDelivery, ct);
        }

        await _db.SaveChangesAsync(ct);
    }

    private long RequireTenant()
    {
        if (!_tenant.IsResolved || !_tenant.IDEmpresa.HasValue)
            throw new InvalidOperationException("Tenant no resuelto.");
        return _tenant.IDEmpresa.Value;
    }
}
