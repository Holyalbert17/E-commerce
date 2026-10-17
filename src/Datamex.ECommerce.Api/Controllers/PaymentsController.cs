using System.Text.Json;
using Datamex.ECommerce.Application.Abstractions;
using Datamex.ECommerce.Application.Checkout;
using Datamex.ECommerce.Application.Payments;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Datamex.ECommerce.Api.Controllers;

[ApiController]
[Route("api/payments/bancard")]
[EnableRateLimiting("api")]
public sealed class PaymentsController : ControllerBase
{
    private readonly IPaymentService _payments;
    private readonly ITenantContext _tenant;

    public PaymentsController(IPaymentService payments, ITenantContext tenant)
    {
        _payments = payments;
        _tenant = tenant;
    }

    [HttpGet("session/{nroPedido:long}")]
    public async Task<ActionResult<PaymentSessionDto>> Session(long nroPedido, CancellationToken ct)
    {
        if (!_tenant.IsResolved) return BadRequest(new { error = "Falta tenant." });
        var session = GetSession();
        if (session is null) return BadRequest(new { error = "Falta X-Cart-Session." });

        var dto = await _payments.GetSessionAsync(nroPedido, session, ct);
        return dto is null ? NotFound() : Ok(dto);
    }

    /// <summary>Webhook Bancard (espejo Checkout POST ERP). Body flexible JSON.</summary>
    [HttpPost("confirm")]
    public async Task<ActionResult<object>> Confirm([FromBody] JsonElement body, CancellationToken ct)
    {
        if (!_tenant.IsResolved) return BadRequest(new { error = "Falta tenant." });

        try
        {
            var payload = ParseWebhook(body);
            var result = await _payments.ConfirmWebhookAsync(payload, ct);
            return Ok(new { status = "success", description = result.Mensaje, order = result });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { status = "error", description = ex.Message });
        }
    }

    [HttpPost("simulate-success")]
    public async Task<ActionResult<CheckoutResultDto>> SimulateSuccess(
        [FromBody] SimulatePaymentRequest body, CancellationToken ct)
    {
        if (!_tenant.IsResolved) return BadRequest(new { error = "Falta tenant." });
        var session = GetSession();
        if (session is null) return BadRequest(new { error = "Falta X-Cart-Session." });

        try
        {
            return Ok(await _payments.SimulateSuccessAsync(body.ShopProcessId, session, ct));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpPost("rollback")]
    public async Task<IActionResult> Rollback([FromBody] SimulatePaymentRequest body, CancellationToken ct)
    {
        if (!_tenant.IsResolved) return BadRequest(new { error = "Falta tenant." });
        try
        {
            await _payments.RollbackAsync(body.ShopProcessId, ct);
            return Ok(new { status = "success" });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    private static BancardWebhookPayload ParseWebhook(JsonElement body)
    {
        var raw = body.GetRawText();
        long shopProcessId = 0;
        string? amountGs = null;
        string? amountUs = null;
        string? currency = null;
        var items = new List<BancardConfirmItem>();

        static long ReadShopProcessId(JsonElement el)
        {
            if (el.ValueKind == JsonValueKind.Number) return el.GetInt64();
            return long.TryParse(el.GetString(), out var v) ? v : 0;
        }

        if (body.TryGetProperty("operation", out var op))
        {
            if (op.TryGetProperty("shop_process_id", out var sp))
                shopProcessId = ReadShopProcessId(sp);
            if (op.TryGetProperty("amount_in_gs", out var ags)) amountGs = ags.GetString();
            if (op.TryGetProperty("amount_in_us", out var aus)) amountUs = aus.GetString();
            if (op.TryGetProperty("currency", out var cur)) currency = cur.GetString();
            if (op.TryGetProperty("items", out var arr) && arr.ValueKind == JsonValueKind.Array)
            {
                foreach (var it in arr.EnumerateArray())
                {
                    items.Add(new BancardConfirmItem(
                        it.TryGetProperty("response_code", out var rc) ? rc.GetString() : "00",
                        it.TryGetProperty("authorization_id", out var aid) ? aid.GetString() : null,
                        it.TryGetProperty("authorization_code", out var ac) ? ac.GetString() : null,
                        it.TryGetProperty("amount", out var amt) ? amt.GetString() : null,
                        it.TryGetProperty("currency", out var c) ? c.GetString() : null));
                }
            }
        }
        else
        {
            // Variantes: camelCase local / snake_case raíz Bancard
            if (body.TryGetProperty("shopProcessId", out var sp2))
                shopProcessId = ReadShopProcessId(sp2);
            else if (body.TryGetProperty("shop_process_id", out var sp3))
                shopProcessId = ReadShopProcessId(sp3);

            if (body.TryGetProperty("amount_in_gs", out var ags2)) amountGs = ags2.GetString();
            if (body.TryGetProperty("amountInGs", out var ags3)) amountGs ??= ags3.GetString();
            if (body.TryGetProperty("currency", out var cur2)) currency = cur2.GetString();
            if (body.TryGetProperty("items", out var arr2) && arr2.ValueKind == JsonValueKind.Array)
            {
                foreach (var it in arr2.EnumerateArray())
                {
                    items.Add(new BancardConfirmItem(
                        it.TryGetProperty("response_code", out var rc) ? rc.GetString()
                            : it.TryGetProperty("responseCode", out var rc2) ? rc2.GetString() : "00",
                        it.TryGetProperty("authorization_id", out var aid) ? aid.GetString() : null,
                        it.TryGetProperty("authorization_code", out var ac) ? ac.GetString() : null,
                        it.TryGetProperty("amount", out var amt) ? amt.GetString() : null,
                        it.TryGetProperty("currency", out var c) ? c.GetString() : null));
                }
            }
        }

        if (shopProcessId <= 0)
            throw new InvalidOperationException("shop_process_id requerido.");

        return new BancardWebhookPayload(shopProcessId, amountGs, amountUs, currency, items, raw);
    }

    private string? GetSession()
    {
        if (Request.Headers.TryGetValue("X-Cart-Session", out var values))
        {
            var v = values.FirstOrDefault();
            if (!string.IsNullOrWhiteSpace(v)) return v.Trim();
        }
        return null;
    }
}

public record SimulatePaymentRequest(long ShopProcessId);
