using Datamex.ECommerce.Application.Options;
using Datamex.ECommerce.Application.Payments;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Datamex.ECommerce.Infrastructure.Payments;

public sealed class StubBancardClient : IBancardClient
{
    private readonly BancardOptions _options;
    private readonly ILogger<StubBancardClient> _logger;

    public StubBancardClient(IOptions<BancardOptions> options, ILogger<StubBancardClient> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    public bool IsEnabled => _options.IsStub;
    public string Mode => _options.Mode;
    public string? CheckoutScriptUrl => null;

    public Task<BancardBuyResponse> CreateMultiBuyAsync(
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
        // Token shape validated for parity with ERP (not sent externally in stub).
        var amountUs = BancardCrypto.FormatAmount(0);
        var amountGsStr = BancardCrypto.FormatAmount(amountGs);
        _ = BancardCrypto.MultiBuyToken(privateKey, shopProcessId, 1, amountUs, amountGsStr);

        var processId = $"stub-{shopProcessId}";
        _logger.LogInformation(
            "Bancard STUB multi_buy shop_process_id={Id} amount={Amt} {Cur} → process_id={Pid}",
            shopProcessId, amountGsStr, currency, processId);

        return Task.FromResult(new BancardBuyResponse("success", processId, "Stub multi_buy OK"));
    }

    public Task<BancardBuyResponse> RollbackAsync(
        string publicKey, string privateKey, long shopProcessId, CancellationToken ct = default)
    {
        _ = BancardCrypto.RollbackToken(privateKey, shopProcessId);
        _logger.LogInformation("Bancard STUB rollback shop_process_id={Id}", shopProcessId);
        return Task.FromResult(new BancardBuyResponse("success", null, "Stub rollback OK"));
    }

    public Task<BancardBuyResponse> GetConfirmationAsync(
        string publicKey, string privateKey, long shopProcessId, CancellationToken ct = default)
    {
        _ = BancardCrypto.ConfirmToken(privateKey, shopProcessId);
        return Task.FromResult(new BancardBuyResponse("success", $"stub-{shopProcessId}", "Stub confirmation OK"));
    }
}
