using System.Text;
using Datamex.ECommerce.Application.Abstractions;
using Datamex.ECommerce.Application.Admin;
using Datamex.ECommerce.Application.Catalog;
using Datamex.ECommerce.Api.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Datamex.ECommerce.Api.Controllers;

[ApiController]
[Route("api/admin/carritos")]
[EnableRateLimiting("api")]
[Authorize]
public sealed class AdminCarritoController : ControllerBase
{
    private readonly IAdminCarritoService _svc;
    private readonly ITenantContext _tenant;

    public AdminCarritoController(IAdminCarritoService svc, ITenantContext tenant)
    {
        _svc = svc;
        _tenant = tenant;
    }

    [HttpGet]
    [RequirePermission("Site_App_Ecommerce_CarritoCompras")]
    public async Task<ActionResult<PagedResult<AdminCarritoListItemDto>>> Search(
        [FromQuery] string? search,
        [FromQuery] int? estado,
        [FromQuery] bool? migrado,
        [FromQuery] DateTime? fechaDesde,
        [FromQuery] DateTime? fechaHasta,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        if (!_tenant.IsResolved) return BadRequest(new { error = "Falta tenant." });
        return Ok(await _svc.SearchAsync(search, estado, migrado, fechaDesde, fechaHasta, page, pageSize, ct));
    }

    [HttpGet("{id:long}")]
    [RequirePermission("Site_App_Ecommerce_CarritoCompras_Trazabilidad")]
    public async Task<ActionResult<AdminCarritoDetailDto>> Get(long id, CancellationToken ct)
    {
        if (!_tenant.IsResolved) return BadRequest(new { error = "Falta tenant." });
        var row = await _svc.GetAsync(id, ct);
        return row is null ? NotFound() : Ok(row);
    }

    [HttpPost("migrar")]
    [RequirePermission("Site_App_Ecommerce_CarritoCompras_Exportacion")]
    public async Task<ActionResult<MarkMigradosResult>> Migrar(
        [FromBody] MarkMigradosRequest body, CancellationToken ct)
    {
        if (!_tenant.IsResolved) return BadRequest(new { error = "Falta tenant." });
        return Ok(await _svc.MarkMigradosAsync(body.Ids, ct));
    }

    /// <summary>Exporta carritos cerrados no migrados (planilla) y opcionalmente marca Migrado.</summary>
    [HttpPost("exportar")]
    [RequirePermission("Site_App_Ecommerce_CarritoCompras_Exportacion")]
    public async Task<IActionResult> Exportar(
        [FromBody] ExportCarritosRequest body,
        CancellationToken ct)
    {
        if (!_tenant.IsResolved) return BadRequest(new { error = "Falta tenant." });

        var rows = await _svc.GetPendingExportAsync(body.Ids, ct);
        if (body.MarcarMigrado && rows.Count > 0)
            await _svc.MarkMigradosAsync(rows.Select(r => r.Id).ToList(), ct);

        var format = (body.Format ?? "csv").Trim().ToLowerInvariant();
        return format switch
        {
            "json" => Ok(rows),
            _ => File(Encoding.UTF8.GetBytes(ToCsv(rows)), "text/csv",
                $"carritos-export-{DateTime.UtcNow:yyyyMMddHHmm}.csv")
        };
    }

    /// <summary>Carritos abiertos sin actividad reciente (abandonados).</summary>
    [HttpGet("abandonados/export")]
    [RequirePermission("Site_App_Ecommerce_CarritoCompras_Exportacion")]
    public async Task<IActionResult> ExportAbandonados(
        [FromQuery] int hours = 24,
        CancellationToken ct = default)
    {
        if (!_tenant.IsResolved) return BadRequest(new { error = "Falta tenant." });
        var bytes = await _svc.ExportAbandonadosCsvAsync(hours, ct);
        return File(bytes, "text/csv", $"carritos-abandonados-{DateTime.UtcNow:yyyyMMddHHmm}.csv");
    }

    private static string ToCsv(IReadOnlyList<AdminCarritoExportRowDto> rows)
    {
        var sb = new StringBuilder();
        sb.AppendLine("Id;NroPedido;FechaPedido;RazonSocial;Documento;MontoTotal;TipoEntrega;TipoPago;Estado;IDPedidoMovil;LugarEntrega");
        foreach (var r in rows)
        {
            sb.Append(r.Id).Append(';')
                .Append(r.NroPedidoCarritoCompra).Append(';')
                .Append(r.FechaPedido.ToString("o")).Append(';')
                .Append(Csv(r.RazonSocial)).Append(';')
                .Append(Csv(r.NumeroDocumento)).Append(';')
                .Append(r.MontoTotal.ToString(System.Globalization.CultureInfo.InvariantCulture)).Append(';')
                .Append(r.TipoEntrega).Append(';')
                .Append(r.TipoPago).Append(';')
                .Append(r.Estado).Append(';')
                .Append(r.IDPedidoMovil?.ToString() ?? "").Append(';')
                .Append(Csv(r.LugarEntrega ?? ""))
                .AppendLine();
        }
        return sb.ToString();
    }

    private static string Csv(string v) =>
        "\"" + v.Replace("\"", "\"\"") + "\"";
}

public record ExportCarritosRequest(
    IReadOnlyList<long>? Ids = null,
    string Format = "csv",
    bool MarcarMigrado = true);
