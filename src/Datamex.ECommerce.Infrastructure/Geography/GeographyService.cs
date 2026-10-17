using Datamex.ECommerce.Application.Abstractions;
using Datamex.ECommerce.Application.Geography;
using Datamex.ECommerce.Application.Options;
using Datamex.ECommerce.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Datamex.ECommerce.Infrastructure.Geography;

public sealed class GeographyService : IGeographyService
{
    private readonly EcomDbContext _db;
    private readonly ITenantContext _tenant;
    private readonly ECommerceOptions _options;

    public GeographyService(EcomDbContext db, ITenantContext tenant, IOptions<ECommerceOptions> options)
    {
        _db = db;
        _tenant = tenant;
        _options = options.Value;
    }

    public async Task<IReadOnlyList<CiudadDeliveryDto>> GetCiudadesDeliveryAsync(CancellationToken ct = default)
    {
        var idEmpresa = RequireTenant();
        var today = DateTime.UtcNow.Date;

        var ciudadIds = await _db.TarifasDelivery.AsNoTracking()
            .Where(t => t.IDEmpresa == idEmpresa
                        && t.Activo
                        && t.Estado == 1 // CERRADO / autorizado (igual ShippingService)
                        && t.FechaInicioVigencia <= today
                        && t.FechaFinVigencia >= today)
            .Select(t => t.IDCiudad)
            .Distinct()
            .ToListAsync(ct);

        return await _db.Ciudades.AsNoTracking()
            .Include(c => c.DivisionPolitica)
            .Where(c => c.Activo && ciudadIds.Contains(c.ID))
            .OrderBy(c => c.DescripcionCiudad)
            .Select(c => new CiudadDeliveryDto(
                c.ID,
                (c.DescripcionCiudad ?? string.Empty).Trim(),
                c.DivisionPolitica != null ? c.DivisionPolitica.NombreAbreviado.Trim() : null))
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<BarrioDeliveryDto>> GetBarriosDeliveryAsync(
        long? idCiudad = null, CancellationToken ct = default)
    {
        var idEmpresa = RequireTenant();
        var today = DateTime.UtcNow.Date;

        var tarifas = await _db.TarifasDelivery.AsNoTracking()
            .Where(t => t.IDEmpresa == idEmpresa
                        && t.Activo
                        && t.Estado == 1 // CERRADO / autorizado
                        && t.FechaInicioVigencia <= today
                        && t.FechaFinVigencia >= today
                        && (idCiudad == null || t.IDCiudad == idCiudad))
            .Select(t => new { t.IDBarrio, t.IDCiudad, t.ImporteDelivery, t.NombreBarrio })
            .ToListAsync(ct);

        return tarifas
            .GroupBy(t => t.IDBarrio)
            .Select(g =>
            {
                var first = g.OrderByDescending(x => x.ImporteDelivery).First();
                return new BarrioDeliveryDto(
                    first.IDBarrio,
                    first.IDCiudad,
                    (first.NombreBarrio ?? string.Empty).Trim(),
                    first.ImporteDelivery);
            })
            .OrderBy(b => b.Nombre)
            .ToList();
    }

    public async Task<IReadOnlyList<TipoDocumentoDto>> GetTiposDocumentoAsync(CancellationToken ct = default)
    {
        var idEmpresa = RequireTenant();
        return await _db.TiposDocumento.AsNoTracking()
            .Where(t => t.IDEmpresa == idEmpresa && t.Activo)
            .OrderBy(t => t.CodTipoDocumento)
            .Select(t => new TipoDocumentoDto(t.ID, t.CodTipoDocumento, t.DescripcionTipoDocumento, t.EsRuc))
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<AgenciaDto>> GetAgenciasPickupAsync(CancellationToken ct = default)
    {
        var idEmpresa = RequireTenant();
        return await _db.Agencias.AsNoTracking()
            .Where(a => a.IDEmpresa == idEmpresa && a.Activo && a.EsSucursalEcommerce)
            .OrderBy(a => a.DescripcionAgencia)
            .Select(a => new AgenciaDto(a.ID, a.DescripcionAgencia, a.Direccion, a.IDCiudad, a.Latitud, a.Longitud, a.IDDeposito))
            .ToListAsync(ct);
    }

    public Task<MapDefaultsDto> GetMapDefaultsAsync(CancellationToken ct = default) =>
        Task.FromResult(new MapDefaultsDto(_options.DefaultMapLat, _options.DefaultMapLng));

    public async Task<IReadOnlyList<HorarioAtencionDto>> GetAgenciaHorariosAsync(
        long idAgencia, CancellationToken ct = default)
    {
        var idEmpresa = RequireTenant();
        var exists = await _db.Agencias.AsNoTracking()
            .AnyAsync(a => a.ID == idAgencia && a.IDEmpresa == idEmpresa && a.Activo, ct);
        if (!exists)
            throw new InvalidOperationException("Sucursal no encontrada.");

        var rows = await _db.HorariosAtencion.AsNoTracking()
            .Where(h => h.IDEmpresa == idEmpresa && h.IDAgencia == idAgencia && h.Activo)
            .OrderBy(h => h.DiaSemana)
            .ToListAsync(ct);

        return rows.Select(h => new HorarioAtencionDto(
            h.DiaSemana,
            FormatTime(h.HoraPrimeraApertura),
            FormatTime(h.HoraPrimerCierre),
            h.HoraSegundaApertura > TimeSpan.Zero && h.HoraSegundoCierre > TimeSpan.Zero
                ? FormatTime(h.HoraSegundaApertura)
                : null,
            h.HoraSegundaApertura > TimeSpan.Zero && h.HoraSegundoCierre > TimeSpan.Zero
                ? FormatTime(h.HoraSegundoCierre)
                : null)).ToList();
    }

    public async Task<AgenciaAbiertaDto> IsAgenciaOpenAsync(
        long idAgencia, DateTime? atLocal = null, CancellationToken ct = default)
    {
        var idEmpresa = RequireTenant();
        var exists = await _db.Agencias.AsNoTracking()
            .AnyAsync(a => a.ID == idAgencia && a.IDEmpresa == idEmpresa && a.Activo, ct);
        if (!exists)
            return new AgenciaAbiertaDto(false, "Sucursal no encontrada.");

        var now = atLocal ?? DateTime.Now;
        var dia = (byte)now.DayOfWeek; // 0=Dom … 6=Sáb (igual ERP)
        var hour = now.TimeOfDay;

        var rows = await _db.HorariosAtencion.AsNoTracking()
            .Where(h => h.IDEmpresa == idEmpresa && h.IDAgencia == idAgencia && h.Activo && h.DiaSemana == dia)
            .ToListAsync(ct);

        if (rows.Count == 0)
            return new AgenciaAbiertaDto(true, null); // sin horario cargado → no bloquear

        foreach (var h in rows)
        {
            if (InRange(hour, h.HoraPrimeraApertura, h.HoraPrimerCierre))
                return new AgenciaAbiertaDto(true, null);
            if (h.HoraSegundaApertura > TimeSpan.Zero
                && h.HoraSegundoCierre > TimeSpan.Zero
                && InRange(hour, h.HoraSegundaApertura, h.HoraSegundoCierre))
                return new AgenciaAbiertaDto(true, null);
        }

        return new AgenciaAbiertaDto(false, "La sucursal está cerrada en este horario.");
    }

    private static bool InRange(TimeSpan t, TimeSpan from, TimeSpan to)
    {
        if (to <= from) return t >= from || t <= to; // cruza medianoche
        return t >= from && t <= to;
    }

    private static string FormatTime(TimeSpan t) => $"{(int)t.TotalHours:D2}:{t.Minutes:D2}";

    private long RequireTenant()
    {
        if (!_tenant.IsResolved || !_tenant.IDEmpresa.HasValue)
            throw new InvalidOperationException("Tenant no resuelto.");
        return _tenant.IDEmpresa.Value;
    }
}
