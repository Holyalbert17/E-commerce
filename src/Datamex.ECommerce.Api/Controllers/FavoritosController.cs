using Datamex.ECommerce.Application.Abstractions;
using Datamex.ECommerce.Application.Favorites;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Datamex.ECommerce.Api.Controllers;

[ApiController]
[Route("api/favoritos")]
[EnableRateLimiting("api")]
[Authorize]
public sealed class FavoritosController : ControllerBase
{
    private readonly IFavoriteService _favorites;
    private readonly ITenantContext _tenant;

    public FavoritosController(IFavoriteService favorites, ITenantContext tenant)
    {
        _favorites = favorites;
        _tenant = tenant;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<FavoritoSeccionDto>>> List([FromQuery] string? canal, CancellationToken ct)
    {
        if (!_tenant.IsResolved) return BadRequest(new { error = "Falta tenant." });
        if (!_tenant.IDUsuarioEcommerce.HasValue) return Unauthorized();
        return Ok(await _favorites.ListSeccionesAsync(_tenant.IDUsuarioEcommerce.Value, canal, ct));
    }

    [HttpPost("secciones")]
    public async Task<ActionResult<FavoritoSeccionDto>> CrearSeccion([FromBody] CrearSeccionFavoritoRequest request, CancellationToken ct)
    {
        if (!_tenant.IsResolved) return BadRequest(new { error = "Falta tenant." });
        if (!_tenant.IDUsuarioEcommerce.HasValue) return Unauthorized();
        try
        {
            return Ok(await _favorites.CrearSeccionAsync(_tenant.IDUsuarioEcommerce.Value, request, ct));
        }
        catch (Exception ex) when (ex is InvalidOperationException or ArgumentException)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpPut("secciones/{id:long}")]
    public async Task<ActionResult<FavoritoSeccionDto>> Renombrar(long id, [FromBody] RenombrarSeccionRequest request, CancellationToken ct)
    {
        if (!_tenant.IsResolved) return BadRequest(new { error = "Falta tenant." });
        if (!_tenant.IDUsuarioEcommerce.HasValue) return Unauthorized();
        try
        {
            return Ok(await _favorites.RenombrarSeccionAsync(_tenant.IDUsuarioEcommerce.Value, id, request, ct));
        }
        catch (Exception ex) when (ex is InvalidOperationException or ArgumentException)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpDelete("secciones/{id:long}")]
    public async Task<IActionResult> EliminarSeccion(long id, CancellationToken ct)
    {
        if (!_tenant.IsResolved) return BadRequest(new { error = "Falta tenant." });
        if (!_tenant.IDUsuarioEcommerce.HasValue) return Unauthorized();
        try
        {
            await _favorites.EliminarSeccionAsync(_tenant.IDUsuarioEcommerce.Value, id, ct);
            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpPost("items")]
    public async Task<ActionResult<FavoritoSeccionDto>> Agregar([FromBody] AgregarFavoritoRequest request, CancellationToken ct)
    {
        if (!_tenant.IsResolved) return BadRequest(new { error = "Falta tenant." });
        if (!_tenant.IDUsuarioEcommerce.HasValue) return Unauthorized();
        try
        {
            return Ok(await _favorites.AgregarAsync(_tenant.IDUsuarioEcommerce.Value, request, ct));
        }
        catch (Exception ex) when (ex is InvalidOperationException or ArgumentException)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpDelete("items/{idDetalle:long}")]
    public async Task<IActionResult> Quitar(long idDetalle, CancellationToken ct)
    {
        if (!_tenant.IsResolved) return BadRequest(new { error = "Falta tenant." });
        if (!_tenant.IDUsuarioEcommerce.HasValue) return Unauthorized();
        try
        {
            await _favorites.QuitarAsync(_tenant.IDUsuarioEcommerce.Value, idDetalle, ct);
            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpDelete("items/by-articulo/{idArticulo:long}")]
    public async Task<IActionResult> QuitarPorArticulo(long idArticulo, CancellationToken ct)
    {
        if (!_tenant.IsResolved) return BadRequest(new { error = "Falta tenant." });
        if (!_tenant.IDUsuarioEcommerce.HasValue) return Unauthorized();
        try
        {
            await _favorites.QuitarPorArticuloAsync(_tenant.IDUsuarioEcommerce.Value, idArticulo, ct);
            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpGet("contains/{idArticulo:long}")]
    public async Task<ActionResult<object>> Contains(long idArticulo, CancellationToken ct)
    {
        if (!_tenant.IsResolved) return BadRequest(new { error = "Falta tenant." });
        if (!_tenant.IDUsuarioEcommerce.HasValue) return Unauthorized();
        var ok = await _favorites.EstaEnFavoritosAsync(_tenant.IDUsuarioEcommerce.Value, idArticulo, ct);
        return Ok(new { enFavoritos = ok });
    }

    [HttpPost("desde-carrito")]
    public async Task<ActionResult<FavoritoSeccionDto>> DesdeCarrito(
        [FromBody] FavoritosDesdeCarritoRequest? request, CancellationToken ct)
    {
        if (!_tenant.IsResolved) return BadRequest(new { error = "Falta tenant." });
        if (!_tenant.IDUsuarioEcommerce.HasValue) return Unauthorized();
        if (!Request.Headers.TryGetValue("X-Cart-Session", out var values)
            || string.IsNullOrWhiteSpace(values.FirstOrDefault()))
            return BadRequest(new { error = "Falta X-Cart-Session." });
        try
        {
            return Ok(await _favorites.CrearDesdeCarritoAsync(
                _tenant.IDUsuarioEcommerce.Value,
                values.ToString().Trim(),
                request ?? new FavoritosDesdeCarritoRequest(),
                ct));
        }
        catch (Exception ex) when (ex is InvalidOperationException or ArgumentException)
        {
            return BadRequest(new { error = ex.Message });
        }
    }
}
