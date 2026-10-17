using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace Datamex.ECommerce.Infrastructure.Payments;

internal static class BancardCrypto
{
    public static string Md5(string input)
    {
        var bytes = MD5.HashData(Encoding.UTF8.GetBytes(input));
        var sb = new StringBuilder(bytes.Length * 2);
        foreach (var b in bytes)
            sb.Append(b.ToString("x2"));
        return sb.ToString();
    }

    public static string FormatAmount(decimal amount) =>
        amount.ToString("0.00", CultureInfo.InvariantCulture);

    public static string MultiBuyToken(
        string privateKey, long shopProcessId, int itemsLength, string amountUs, string amountGs) =>
        Md5($"{privateKey.Trim()}{shopProcessId}{itemsLength}{amountUs}{amountGs}");

    public static string RollbackToken(string privateKey, long shopProcessId) =>
        Md5($"{privateKey.Trim()}{shopProcessId}rollback00,000,00");

    public static string ConfirmToken(string privateKey, long shopProcessId) =>
        Md5($"{privateKey.Trim()}{shopProcessId}get_confirmation");
}
