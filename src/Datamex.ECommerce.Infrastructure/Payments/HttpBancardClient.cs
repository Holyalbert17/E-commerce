using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Datamex.ECommerce.Application.Options;
using Datamex.ECommerce.Application.Payments;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Datamex.ECommerce.Infrastructure.Payments;

public sealed class HttpBancardClient : IBancardClient
{
    private readonly HttpClient _http;
    private readonly BancardOptions _options;
    private readonly ILogger<HttpBancardClient> _logger;

    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public HttpBancardClient(HttpClient http, IOptions<BancardOptions> options, ILogger<HttpBancardClient> logger)
    {
        _http = http;
        _options = options.Value;
        _logger = logger;
    }

    public bool IsEnabled => _options.IsLive;
    public string Mode => "Live";
    public string? CheckoutScriptUrl => _options.CheckoutScriptUrl;

    public async Task<BancardBuyResponse> CreateMultiBuyAsync(
        string publicKey,
        string privateKey,
        long shopProcessId,
        decimal amountGs,
        string currency,
        int numberOfItems,
        long store,
        long storeBranch,
        string itemName,
        string returnUrl,
        string cancelUrl,
        CancellationToken ct = default)
    {
        var amountUs = BancardCrypto.FormatAmount(0);
        var amountGsStr = BancardCrypto.FormatAmount(amountGs);
        var body = new
        {
            public_key = publicKey,
            operation = new
            {
                token = BancardCrypto.MultiBuyToken(privateKey, shopProcessId, 1, amountUs, amountGsStr),
                number_of_items = numberOfItems,
                shop_process_id = shopProcessId,
                currency,
                amount_in_us = amountUs,
                amount_in_gs = amountGsStr,
                items = new[]
                {
                    new
                    {
                        name = itemName,
                        store,
                        store_branch = storeBranch,
                        amount = amountGsStr,
                        currency
                    }
                },
                additional_data = "",
                description = "",
                return_url = returnUrl,
                cancel_url = cancelUrl
            }
        };

        return await PostAsync(_options.MultiBuyUrl, body, ct);
    }

    public async Task<BancardBuyResponse> RollbackAsync(
        string publicKey, string privateKey, long shopProcessId, CancellationToken ct = default)
    {
        var body = new
        {
            public_key = publicKey,
            operation = new
            {
                token = BancardCrypto.RollbackToken(privateKey, shopProcessId),
                shop_process_id = shopProcessId.ToString()
            }
        };
        return await PostAsync(_options.RollbackUrl, body, ct);
    }

    public async Task<BancardBuyResponse> GetConfirmationAsync(
        string publicKey, string privateKey, long shopProcessId, CancellationToken ct = default)
    {
        var body = new
        {
            public_key = publicKey,
            operation = new
            {
                token = BancardCrypto.ConfirmToken(privateKey, shopProcessId),
                shop_process_id = shopProcessId.ToString()
            }
        };
        return await PostAsync(_options.ConfirmationsUrl, body, ct);
    }

    private async Task<BancardBuyResponse> PostAsync(string url, object body, CancellationToken ct)
    {
        using var resp = await _http.PostAsJsonAsync(url, body, JsonOpts, ct);
        var text = await resp.Content.ReadAsStringAsync(ct);
        if (!resp.IsSuccessStatusCode)
        {
            _logger.LogWarning("Bancard HTTP {Code}: {Body}", (int)resp.StatusCode, text);
            return new BancardBuyResponse("error", null, text);
        }

        try
        {
            using var doc = JsonDocument.Parse(text);
            var root = doc.RootElement;
            var status = root.TryGetProperty("status", out var s) ? s.GetString() ?? "error" : "error";
            var processId = root.TryGetProperty("process_id", out var p) ? p.GetString() : null;
            var desc = root.TryGetProperty("description", out var d) ? d.GetString() : null;
            return new BancardBuyResponse(status, processId, desc);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "No se pudo parsear respuesta Bancard: {Body}", text);
            return new BancardBuyResponse("error", null, text);
        }
    }
}
