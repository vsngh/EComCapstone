using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using ECommerce.Api.Configuration;
using Microsoft.Extensions.Options;

namespace ECommerce.Api.Security;

public class WebhookSignatureVerifier
{
    private readonly string _secret;

    public WebhookSignatureVerifier(IOptions<PaymentsOptions> options)
    {
        _secret = options.Value.WebhookSecret;
    }

    public bool IsValid(object payload, string? receivedSignature)
    {
        if (string.IsNullOrWhiteSpace(_secret) || string.IsNullOrWhiteSpace(receivedSignature))
        {
            return false;
        }

        var body = JsonSerializer.Serialize(payload);
        var expected = ComputeHmacSha256(body, _secret);
        var expectedBytes = Encoding.UTF8.GetBytes(expected);
        var receivedBytes = Encoding.UTF8.GetBytes(receivedSignature);

        return CryptographicOperations.FixedTimeEquals(expectedBytes, receivedBytes);
    }

    public static string ComputeHmacSha256(string message, string secret)
    {
        var key = Encoding.UTF8.GetBytes(secret);
        var data = Encoding.UTF8.GetBytes(message);

        byte[] digest;
        using (var hmac = new HMACSHA256(key))
        {
            digest = hmac.ComputeHash(data);
        }

        return string.Concat(digest.Select(b => b.ToString("x2")));
    }
}