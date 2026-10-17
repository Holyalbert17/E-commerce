using Datamex.ECommerce.Application.Abstractions;
using Datamex.ECommerce.Application.Shipping;
using Datamex.ECommerce.Domain.Entities;
using Datamex.ECommerce.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Datamex.ECommerce.Infrastructure.Shipping;

/// <summary>
/// Alineado a CarritoCompraService.AjustarTarifa: agrega/actualiza línea del artículo delivery
/// con ImporteDelivery de tarifadelivery vigente por barrio.
/// </summary>
public sealed class ShippingService : IShippingService
{
    private readonly EcomDbContext _db;
    private readonly ITenantContext _tenant;

    public ShippingService(EcomDbContext db, ITenantContext tenant)
    {
        _db = db;
        _tenant = tenant;
    }

    public async Task<IReadOnlyList<TarifaDeliveryDto>> GetActiveTarifasAsync(CancellationToken ct = default)
    {
        var idEmpresa = RequireTenant();
        var today = DateTime.UtcNow.Date;

        return await _db.TarifasDelivery.AsNoTracking()
            .Where(t => t.IDEmpresa == idEmpresa
                        && t.Activo
                        && t.Estado == 1
                        && t.FechaInicioVigencia <= today
                        && t.FechaFinVigencia >= today)
            .OrderBy(t => t.NombreCiudad)
            .ThenBy(t => t.NombreBarrio)
            .Select(t => new TarifaDeliveryDto(
                t.ID, t.IDCiudad, t.IDBarrio, t.NombreCiudad, t.NombreBarrio,
                t.ImporteDelivery, t.FechaInicioVigencia, t.FechaFinVigencia))
            .ToListAsync(ct);
    }

    public async Task ApplyToCartAsync(string sessionId, ApplyShippingRequest request, CancellationToken ct = default)
    {
        var idEmpresa = RequireTenant();
        var config = await _db.EmpresaConfiguracionesEcommerce.FirstAsync(c => c.ID == idEmpresa, ct);
        if (!config.RealizaDelivery)
            throw new InvalidOperationException("La tienda no realiza delivery.");

        if (config.IDArticuloDelivery is null or 0)
            throw new InvalidOperationException("No hay artículo de delivery configurado (IDArticuloDelivery).");

        var cart = await GetOpenCartAsync(sessionId, ct);
        if (cart is null)
            throw new InvalidOperationException("No hay carrito abierto. Agregá productos primero.");

        var hasProducts = await _db.DetallesCarritoCompra.AnyAsync(d =>
            d.IDCarritoCompra == cart.ID
            && d.Activo
            && d.IDArticulo != config.IDArticuloDelivery, ct);
        if (!hasProducts)
            throw new InvalidOperationException("Agregá productos al carrito antes de elegir delivery.");

        TarifaDelivery? tarifa = null;
        if (request.IDTarifaDelivery.HasValue)
        {
            tarifa = await _db.TarifasDelivery.AsNoTracking()
                .FirstOrDefaultAsync(t =>
                    t.ID == request.IDTarifaDelivery && t.IDEmpresa == idEmpresa && t.Activo && t.Estado == 1, ct);
        }
        else if (request.IDBarrio.HasValue)
        {
            var today = DateTime.UtcNow.Date;
            tarifa = await _db.TarifasDelivery.AsNoTracking()
                .Where(t => t.IDEmpresa == idEmpresa
                            && t.IDBarrio == request.IDBarrio
                            && t.Activo
                            && t.Estado == 1
                            && t.FechaInicioVigencia <= today
                            && t.FechaFinVigencia >= today)
                .OrderByDescending(t => t.FechaInicioVigencia)
                .FirstOrDefaultAsync(ct);
        }

        if (tarifa is null)
            throw new InvalidOperationException("Tarifa de delivery no encontrada o fuera de vigencia.");

        var artId = config.IDArticuloDelivery.Value;
        var art = await _db.Articulos.AsNoTracking()
                      .FirstOrDefaultAsync(a => a.ID == artId && a.IDEmpresa == idEmpresa, ct)
                  ?? throw new InvalidOperationException("Artículo de delivery no existe.");

        var price = tarifa.ImporteDelivery;
        var sinIva = Math.Round(price / 1.1m, 2);
        var now = DateTime.UtcNow;

        var line = await _db.DetallesCarritoCompra
            .FirstOrDefaultAsync(d => d.IDCarritoCompra == cart.ID && d.IDArticulo == artId, ct);

        if (line is null)
        {
            line = new DetalleCarritoCompra
            {
                IDEmpresa = idEmpresa,
                IDCarritoCompra = cart.ID,
                IDArticulo = artId,
                Activo = true,
                UltModificacion = now,
                CantidadxEmbalaje = 1,
                IDRegimenImpositivo = 1,
                Cantidad = 1,
                CantidadEmbalada = 1,
                PorcentajeImpuesto = 10,
                PrecioBrutoConImpuesto = price,
                PrecioBrutoSinImpuesto = sinIva,
                PrecioNetoConImpuesto = price,
                PrecioNetoSinImpuesto = sinIva,
                MontoTotal = price,
                MontoIVA = Math.Round(price - sinIva, 2)
            };
            _db.DetallesCarritoCompra.Add(line);
        }
        else
        {
            line.PrecioBrutoConImpuesto = price;
            line.PrecioNetoConImpuesto = price;
            line.PrecioBrutoSinImpuesto = sinIva;
            line.PrecioNetoSinImpuesto = sinIva;
            line.MontoTotal = price;
            line.MontoIVA = Math.Round(price - sinIva, 2);
            line.UltModificacion = now;
            line.Activo = true;
        }

        cart.LugarEntrega = $"{tarifa.NombreBarrio}, {tarifa.NombreCiudad}";
        cart.TipoEntrega = TipoEntregaEcommerce.Delivery;
        if (tarifa.IDAgencia.HasValue)
            cart.IDAgenciaFacturacion = tarifa.IDAgencia.Value;

        await _db.SaveChangesAsync(ct);
        await RecalcAsync(cart, ct);
        await _db.SaveChangesAsync(ct);
    }

    public async Task ClearFromCartAsync(string sessionId, CancellationToken ct = default)
    {
        var idEmpresa = RequireTenant();
        var config = await _db.EmpresaConfiguracionesEcommerce.AsNoTracking()
            .FirstOrDefaultAsync(c => c.ID == idEmpresa, ct);
        if (config?.IDArticuloDelivery is null or 0)
            return;

        var cart = await GetOpenCartAsync(sessionId, ct);
        if (cart is null)
            return;

        var lines = await _db.DetallesCarritoCompra
            .Where(d => d.IDCarritoCompra == cart.ID && d.IDArticulo == config.IDArticuloDelivery)
            .ToListAsync(ct);
        if (lines.Count == 0)
            return;

        _db.DetallesCarritoCompra.RemoveRange(lines);
        await _db.SaveChangesAsync(ct);
        await RecalcAsync(cart, ct);
        await _db.SaveChangesAsync(ct);
    }

    private async Task<CarritoCompra?> GetOpenCartAsync(string sessionId, CancellationToken ct)
    {
        var idEmpresa = RequireTenant();
        return await _db.CarritosCompra
            .FirstOrDefaultAsync(c =>
                c.IDEmpresa == idEmpresa
                && c.IDSesion == sessionId
                && c.Estado == EstadoCarrito.Abierto
                && !c.Migrado
                && c.Activo, ct);
    }

    private async Task RecalcAsync(CarritoCompra cart, CancellationToken ct)
    {
        var lines = await _db.DetallesCarritoCompra
            .Where(d => d.IDCarritoCompra == cart.ID && d.Activo)
            .ToListAsync(ct);
        cart.MontoTotal = lines.Sum(l => l.MontoTotal);
        cart.MontoIVA = lines.Sum(l => l.MontoIVA);
        cart.MontoGravado = cart.MontoTotal - cart.MontoIVA;
        cart.UltModificacion = DateTime.UtcNow;
    }

    private long RequireTenant()
    {
        if (!_tenant.IsResolved || !_tenant.IDEmpresa.HasValue)
            throw new InvalidOperationException("Tenant no resuelto.");
        return _tenant.IDEmpresa.Value;
    }
}
