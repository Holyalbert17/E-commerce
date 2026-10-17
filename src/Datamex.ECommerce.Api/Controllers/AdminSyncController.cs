using Datamex.ECommerce.Api.Authorization;
using Datamex.ECommerce.Application.Abstractions;
using Datamex.ECommerce.Application.Sync;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Datamex.ECommerce.Api.Controllers;

[ApiController]
[Route("api/admin")]
[EnableRateLimiting("api")]
[Authorize]
public sealed class AdminSyncController : ControllerBase
{
    private readonly IPedidoErpMigrationService _migration;
    private readonly IErpSyncService _sync;
    private readonly ITenantContext _tenant;

    public AdminSyncController(
        IPedidoErpMigrationService migration,
        IErpSyncService sync,
        ITenantContext tenant)
    {
        _migration = migration;
        _sync = sync;
        _tenant = tenant;
    }

    [HttpPost("orders/migrar-erp")]
    [RequirePermission("Site_App_Ecommerce_Pedidos")]
    public async Task<ActionResult<MigrarPedidosResult>> MigrarErp(
        [FromBody] MigrarPedidosRequest body, CancellationToken ct)
    {
        if (!_tenant.IsResolved) return BadRequest(new { error = "Falta tenant." });
        try
        {
            return Ok(await _migration.MigrarAsync(body.Ids, ct));
        }
        catch (Exception ex) when (ex is InvalidOperationException or ArgumentException)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpPost("orders/{id:long}/reintentar-migracion")]
    [RequirePermission("Site_App_Ecommerce_Pedidos")]
    public async Task<ActionResult<MigrarPedidosResult>> ReintentarMigracion(long id, CancellationToken ct)
    {
        if (!_tenant.IsResolved) return BadRequest(new { error = "Falta tenant." });
        try
        {
            return Ok(await _migration.MigrarAsync(new[] { id }, ct));
        }
        catch (Exception ex) when (ex is InvalidOperationException or ArgumentException)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpGet("orders/migracion-logs")]
    [RequirePermission("Site_App_Ecommerce_Pedidos")]
    public async Task<ActionResult<IReadOnlyList<MigracionPedidoLogDto>>> MigracionLogs(
        [FromQuery] long? idPedidoMovil,
        [FromQuery] int limit = 100,
        CancellationToken ct = default)
    {
        if (!_tenant.IsResolved) return BadRequest(new { error = "Falta tenant." });
        return Ok(await _migration.ListLogsAsync(idPedidoMovil, limit, ct));
    }

    [HttpGet("sync/status")]
    [RequirePermission("Site_App_Ecommerce", "Site_App_Ecommerce_Productos", "manager:tenants")]
    public async Task<ActionResult<ErpSyncStatusDto>> SyncStatus(CancellationToken ct)
    {
        if (!_tenant.IsResolved) return BadRequest(new { error = "Falta tenant." });
        try
        {
            return Ok(await _sync.GetStatusAsync(ct));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpPost("sync/run")]
    [RequirePermission("Site_App_Ecommerce", "Site_App_Ecommerce_Productos", "manager:tenants")]
    public ActionResult SyncRun([FromBody] ErpSyncRunRequest? body, CancellationToken ct)
    {
        // Inbound solo vía ecom-etl (Python). Esta API solo expone cursors / write-back pedidos.
        return StatusCode(StatusCodes.Status410Gone, new
        {
            error = "El pull ERP→ecom ya no corre en la API. Usá la app ecom-etl (once/continuous).",
            inboundEngine = "ecom-etl",
            hint = "cd ecom-etl && ecom-etl -c config/local.yaml once --mode delta --tenants multi"
        });
    }

    /// <summary>Deshabilitado: lazy-pull reemplazado por ecom-etl.</summary>
    [HttpPost("sync/ensure-articulo")]
    [RequirePermission("Site_App_Ecommerce", "Site_App_Ecommerce_Productos", "manager:tenants")]
    public ActionResult EnsureArticulo([FromBody] EnsureArticuloRequest body, CancellationToken ct)
    {
        return StatusCode(StatusCodes.Status410Gone, new
        {
            error = "ensure-articulo deshabilitado. El catálogo lo sincroniza ecom-etl.",
            inboundEngine = "ecom-etl"
        });
    }
}

public record EnsureArticuloRequest(long? ArticuloId = null, string? Sku = null);
