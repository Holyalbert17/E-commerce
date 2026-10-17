using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Configuration;

namespace Datamex.ECommerce.Infrastructure.Sync;

/// <summary>Cifrado reversible liviano para passwords ERP (clave en Erp:CredentialKey).</summary>
internal sealed class ErpSecretBox
{
    private readonly byte[] _key;

    public ErpSecretBox(IConfiguration config)
    {
        var material = config["Erp:CredentialKey"];
        if (string.IsNullOrWhiteSpace(material))
            material = "Datamex.Ecom.ErpCred.v1";
        _key = SHA256.HashData(Encoding.UTF8.GetBytes(material.Trim()));
    }

    public string Protect(string plain)
    {
        var nonce = RandomNumberGenerator.GetBytes(12);
        var plainBytes = Encoding.UTF8.GetBytes(plain);
        var cipher = new byte[plainBytes.Length];
        var tag = new byte[16];
        using var aes = new AesGcm(_key, 16);
        aes.Encrypt(nonce, plainBytes, cipher, tag);
        var payload = new byte[1 + nonce.Length + tag.Length + cipher.Length];
        payload[0] = 1; // version
        Buffer.BlockCopy(nonce, 0, payload, 1, nonce.Length);
        Buffer.BlockCopy(tag, 0, payload, 1 + nonce.Length, tag.Length);
        Buffer.BlockCopy(cipher, 0, payload, 1 + nonce.Length + tag.Length, cipher.Length);
        return "v1:" + Convert.ToBase64String(payload);
    }

    public string? Unprotect(string? stored)
    {
        if (string.IsNullOrWhiteSpace(stored)) return null;
        if (!stored.StartsWith("v1:", StringComparison.Ordinal))
            return stored; // legacy/plain (dev)
        try
        {
            var payload = Convert.FromBase64String(stored[3..]);
            if (payload.Length < 1 + 12 + 16 || payload[0] != 1) return null;
            var nonce = payload.AsSpan(1, 12);
            var tag = payload.AsSpan(13, 16);
            var cipher = payload.AsSpan(29);
            var plain = new byte[cipher.Length];
            using var aes = new AesGcm(_key, 16);
            aes.Decrypt(nonce, cipher, tag, plain);
            return Encoding.UTF8.GetString(plain);
        }
        catch
        {
            return null;
        }
    }
}
