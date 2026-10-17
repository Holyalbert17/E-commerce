namespace Datamex.ECommerce.Application.Payments;

public record BancardBuyResponse(string Status, string? ProcessId, string? Description);

public record BancardConfirmItem(
    string? ResponseCode,
    string? AuthorizationId,
    string? AuthorizationCode,
    string? Amount,
    string? Currency);

public record BancardWebhookPayload(
    long ShopProcessId,
    string? AmountInGs,
    string? AmountInUs,
    string? Currency,
    IReadOnlyList<BancardConfirmItem> Items,
    string? RawBody = null);

public interface IBancardClient
{
    bool IsEnabled { get; }
    string Mode { get; }
    string? CheckoutScriptUrl { get; }

    Task<BancardBuyResponse> CreateMultiBuyAsync(
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
        CancellationToken ct = default);

    Task<BancardBuyResponse> RollbackAsync(
        string publicKey, string privateKey, long shopProcessId, CancellationToken ct = default);

    Task<BancardBuyResponse> GetConfirmationAsync(
        string publicKey, string privateKey, long shopProcessId, CancellationToken ct = default);
}
