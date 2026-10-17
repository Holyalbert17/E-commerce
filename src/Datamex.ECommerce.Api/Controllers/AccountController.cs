using Datamex.ECommerce.Application.Account;
using Datamex.ECommerce.Application.Abstractions;
using Datamex.ECommerce.Application.Invoices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Datamex.ECommerce.Api.Controllers;

[ApiController]
[Route("api/account")]
[EnableRateLimiting("api")]
[Authorize]
public sealed class AccountController : ControllerBase
{
    private readonly IAccountService _account;
    private readonly IInvoiceService _invoices;
    private readonly ITenantContext _tenant;

    public AccountController(IAccountService account, IInvoiceService invoices, ITenantContext tenant)
    {
        _account = account;
        _invoices = invoices;
        _tenant = tenant;
    }

    [HttpGet("me")]
    public async Task<ActionResult<AccountProfileDto>> Me(CancellationToken ct)
    {
        if (!_tenant.IsResolved)
            return BadRequest(new { error = "Falta tenant." });
        if (!_tenant.IDUsuarioEcommerce.HasValue)
            return Unauthorized(new { error = "Usuario no autenticado." });

        var profile = await _account.GetProfileAsync(_tenant.IDUsuarioEcommerce.Value, ct);
        return profile is null ? NotFound(new { error = "Usuario no encontrado." }) : Ok(profile);
    }

    [HttpPut("me")]
    public async Task<ActionResult<AccountProfileDto>> UpdateMe(
        [FromBody] UpdateProfileRequest request, CancellationToken ct)
    {
        if (!_tenant.IsResolved)
            return BadRequest(new { error = "Falta tenant." });
        if (!_tenant.IDUsuarioEcommerce.HasValue)
            return Unauthorized(new { error = "Usuario no autenticado." });

        try
        {
            return Ok(await _account.UpdateProfileAsync(_tenant.IDUsuarioEcommerce.Value, request, ct));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpPost("solicitar-mayorista")]
    public async Task<ActionResult<AccountProfileDto>> SolicitarMayorista(
        [FromBody] SolicitarMayoristaRequest? request, CancellationToken ct)
    {
        if (!_tenant.IsResolved)
            return BadRequest(new { error = "Falta tenant." });
        if (!_tenant.IDUsuarioEcommerce.HasValue)
            return Unauthorized(new { error = "Usuario no autenticado." });

        try
        {
            return Ok(await _account.SolicitarMayoristaAsync(
                _tenant.IDUsuarioEcommerce.Value,
                request ?? new SolicitarMayoristaRequest(),
                ct));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpGet("orders")]
    public async Task<ActionResult<IReadOnlyList<AccountOrderDto>>> Orders(CancellationToken ct)
    {
        if (!_tenant.IsResolved)
            return BadRequest(new { error = "Falta tenant." });
        if (!_tenant.IDUsuarioEcommerce.HasValue)
            return Unauthorized(new { error = "Usuario no autenticado." });

        return Ok(await _account.GetOrdersAsync(_tenant.IDUsuarioEcommerce.Value, ct));
    }

    [HttpPost("orders/{nroPedido:long}/cancelar")]
    public async Task<IActionResult> CancelOrder(
        long nroPedido, [FromBody] CancelOrderRequest? request, CancellationToken ct)
    {
        if (!_tenant.IsResolved)
            return BadRequest(new { error = "Falta tenant." });
        if (!_tenant.IDUsuarioEcommerce.HasValue)
            return Unauthorized(new { error = "Usuario no autenticado." });

        try
        {
            await _account.CancelOrderAsync(
                _tenant.IDUsuarioEcommerce.Value,
                nroPedido,
                request?.Motivo,
                ct);
            return Ok(new { ok = true });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>Historial de facturas emitidas (ERP / Pentaho).</summary>
    [HttpGet("invoices")]
    public async Task<ActionResult<IReadOnlyList<AccountInvoiceDto>>> Invoices(CancellationToken ct)
    {
        if (!_tenant.IsResolved)
            return BadRequest(new { error = "Falta tenant." });
        if (!_tenant.IDUsuarioEcommerce.HasValue)
            return Unauthorized(new { error = "Usuario no autenticado." });

        return Ok(await _invoices.ListAsync(_tenant.IDUsuarioEcommerce.Value, ct));
    }

    /// <summary>Descarga PDF factura vía Pentaho (idpedido + idempresa, paridad ERP).</summary>
    [HttpGet("orders/{nroPedido:long}/factura.pdf")]
    public async Task<IActionResult> DownloadFactura(long nroPedido, CancellationToken ct)
    {
        if (!_tenant.IsResolved)
            return BadRequest(new { error = "Falta tenant." });
        if (!_tenant.IDUsuarioEcommerce.HasValue)
            return Unauthorized(new { error = "Usuario no autenticado." });

        try
        {
            var pdf = await _invoices.DownloadPdfAsync(_tenant.IDUsuarioEcommerce.Value, nroPedido, ct);
            Response.Headers["Content-Disposition"] = $"inline; filename=\"{pdf.FileName}\"";
            return File(pdf.Content, "application/pdf");
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>PDF nota de crédito (devolución) vía Pentaho — original a media hoja.</summary>
    [HttpGet("orders/{nroPedido:long}/nota-credito.pdf")]
    public async Task<IActionResult> DownloadNotaCredito(long nroPedido, CancellationToken ct)
    {
        if (!_tenant.IsResolved)
            return BadRequest(new { error = "Falta tenant." });
        if (!_tenant.IDUsuarioEcommerce.HasValue)
            return Unauthorized(new { error = "Usuario no autenticado." });

        try
        {
            var pdf = await _invoices.DownloadNotaCreditoPdfAsync(_tenant.IDUsuarioEcommerce.Value, nroPedido, ct);
            Response.Headers["Content-Disposition"] = $"inline; filename=\"{pdf.FileName}\"";
            return File(pdf.Content, "application/pdf");
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }
}
