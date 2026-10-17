using Datamex.ECommerce.Application.Abstractions;
using Datamex.ECommerce.Application.Geography;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Datamex.ECommerce.Api.Controllers;

[ApiController]
[Route("api/geography")]
[EnableRateLimiting("api")]
public sealed class GeographyController : ControllerBase
{
    private readonly IGeographyService _geo;
    private readonly ITenantContext _tenant;

    public GeographyController(IGeographyService geo, ITenantContext tenant)
    {
        _geo = geo;
        _tenant = tenant;
    }

    [HttpGet("ciudades-delivery")]
    public async Task<ActionResult<IReadOnlyList<CiudadDeliveryDto>>> Ciudades(CancellationToken ct)
    {
        if (!_tenant.IsResolved) return BadRequest(new { error = "Falta tenant." });
        return Ok(await _geo.GetCiudadesDeliveryAsync(ct));
    }

    [HttpGet("barrios-delivery")]
    public async Task<ActionResult<IReadOnlyList<BarrioDeliveryDto>>> Barrios(
        [FromQuery] long? ciudadId, CancellationToken ct)
    {
        if (!_tenant.IsResolved) return BadRequest(new { error = "Falta tenant." });
        return Ok(await _geo.GetBarriosDeliveryAsync(ciudadId, ct));
    }

    [HttpGet("tipos-documento")]
    public async Task<ActionResult<IReadOnlyList<TipoDocumentoDto>>> TiposDocumento(CancellationToken ct)
    {
        if (!_tenant.IsResolved) return BadRequest(new { error = "Falta tenant." });
        return Ok(await _geo.GetTiposDocumentoAsync(ct));
    }

    [HttpGet("agencias")]
    public async Task<ActionResult<IReadOnlyList<AgenciaDto>>> Agencias(CancellationToken ct)
    {
        if (!_tenant.IsResolved) return BadRequest(new { error = "Falta tenant." });
        return Ok(await _geo.GetAgenciasPickupAsync(ct));
    }

    [HttpGet("agencias/{id:long}/horarios")]
    public async Task<ActionResult<IReadOnlyList<HorarioAtencionDto>>> AgenciaHorarios(long id, CancellationToken ct)
    {
        if (!_tenant.IsResolved) return BadRequest(new { error = "Falta tenant." });
        try
        {
            return Ok(await _geo.GetAgenciaHorariosAsync(id, ct));
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new { error = ex.Message });
        }
    }

    [HttpGet("map-defaults")]
    public async Task<ActionResult<MapDefaultsDto>> MapDefaults(CancellationToken ct)
    {
        return Ok(await _geo.GetMapDefaultsAsync(ct));
    }
}
