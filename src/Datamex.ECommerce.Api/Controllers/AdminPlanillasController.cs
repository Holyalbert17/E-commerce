using Datamex.ECommerce.Api.Authorization;
using Datamex.ECommerce.Application.Abstractions;
using Datamex.ECommerce.Application.Admin;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Datamex.ECommerce.Api.Controllers;

[ApiController]
[Route("api/admin/planillas")]
[EnableRateLimiting("api")]
[Authorize]
public sealed class AdminPlanillasController : ControllerBase
{
    private readonly IAdminPlanillaService _planillas;
    private readonly ITenantContext _tenant;

    public AdminPlanillasController(IAdminPlanillaService planillas, ITenantContext tenant)
    {
        _planillas = planillas;
        _tenant = tenant;
    }

    [HttpGet("clientes/export")]
    [RequirePermission("Site_App_Ecommerce_Clientes")]
    public async Task<IActionResult> ExportClientes([FromQuery] string format = "csv", CancellationToken ct = default)
    {
        if (!_tenant.IsResolved) return BadRequest(new { error = "Falta tenant." });
        if (IsExcel(format))
        {
            var xlsx = await _planillas.ExportClientesExcelAsync(ct);
            return File(xlsx,
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                $"clientes-{DateTime.UtcNow:yyyyMMddHHmm}.xlsx");
        }
        var bytes = await _planillas.ExportClientesCsvAsync(ct);
        return File(bytes, "text/csv", $"clientes-{DateTime.UtcNow:yyyyMMddHHmm}.csv");
    }

    [HttpPost("clientes/import")]
    [RequirePermission("Site_App_Ecommerce_Clientes_ClientesCuentas_Add")]
    public async Task<ActionResult<PlanillaImportResult>> ImportClientes(IFormFile file, CancellationToken ct)
    {
        if (!_tenant.IsResolved) return BadRequest(new { error = "Falta tenant." });
        if (file is null || file.Length == 0)
            return BadRequest(new { error = "Archivo requerido." });

        await using var stream = file.OpenReadStream();
        return Ok(await _planillas.ImportClientesCsvAsync(stream, ct));
    }

    [HttpGet("listas-precios/{listaId:long}/export")]
    [RequirePermission("Site_App_Ecommerce_ListasPrecios")]
    public async Task<IActionResult> ExportListaPrecios(
        long listaId, [FromQuery] string format = "csv", CancellationToken ct = default)
    {
        if (!_tenant.IsResolved) return BadRequest(new { error = "Falta tenant." });
        try
        {
            if (IsExcel(format))
            {
                var xlsx = await _planillas.ExportListaPreciosExcelAsync(listaId, ct);
                return File(xlsx,
                    "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                    $"lista-precios-{listaId}-{DateTime.UtcNow:yyyyMMddHHmm}.xlsx");
            }
            var bytes = await _planillas.ExportListaPreciosCsvAsync(listaId, ct);
            return File(bytes, "text/csv", $"lista-precios-{listaId}-{DateTime.UtcNow:yyyyMMddHHmm}.csv");
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpPost("listas-precios/{listaId:long}/import")]
    [RequirePermission("Site_App_Ecommerce_ListasPrecios_DetListPrecioCondicionVenta_Add")]
    public async Task<ActionResult<PlanillaImportResult>> ImportListaPrecios(
        long listaId, IFormFile file, CancellationToken ct)
    {
        if (!_tenant.IsResolved) return BadRequest(new { error = "Falta tenant." });
        if (file is null || file.Length == 0)
            return BadRequest(new { error = "Archivo requerido." });

        try
        {
            await using var stream = file.OpenReadStream();
            return Ok(await _planillas.ImportListaPreciosCsvAsync(listaId, stream, ct));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpGet("articulos/export")]
    [RequirePermission("Site_App_Ecommerce_Productos")]
    public async Task<IActionResult> ExportArticulos([FromQuery] string format = "csv", CancellationToken ct = default)
    {
        if (!_tenant.IsResolved) return BadRequest(new { error = "Falta tenant." });
        if (IsExcel(format))
        {
            var xlsx = await _planillas.ExportArticulosExcelAsync(ct);
            return File(xlsx,
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                $"articulos-{DateTime.UtcNow:yyyyMMddHHmm}.xlsx");
        }
        var bytes = await _planillas.ExportArticulosCsvAsync(ct);
        return File(bytes, "text/csv", $"articulos-{DateTime.UtcNow:yyyyMMddHHmm}.csv");
    }

    [HttpPost("articulos/import-activo")]
    [RequirePermission("Site_App_Ecommerce_Productos_ActivarInactivar")]
    public async Task<ActionResult<PlanillaImportResult>> ImportArticulosActivo(
        IFormFile file, CancellationToken ct)
    {
        if (!_tenant.IsResolved) return BadRequest(new { error = "Falta tenant." });
        if (file is null || file.Length == 0)
            return BadRequest(new { error = "Archivo requerido." });

        await using var stream = file.OpenReadStream();
        return Ok(await _planillas.ImportArticulosActivoCsvAsync(stream, ct));
    }

    private static bool IsExcel(string? format) =>
        string.Equals(format, "xlsx", StringComparison.OrdinalIgnoreCase)
        || string.Equals(format, "excel", StringComparison.OrdinalIgnoreCase);
}
