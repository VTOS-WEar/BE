using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using OtpNet;
using VTOS.Application.Abstractions;

namespace VTOS.Infrastructure.ExternalServices.TwoFactor;

/// <summary>
/// TOTP service implementation using Otp.NET (RFC 6238).
/// </summary>
public class TotpService : ITotpService
{
    public string GenerateSecret()
    {
        var key = KeyGeneration.GenerateRandomKey(20); // 160-bit
        return Base32Encoding.ToString(key);
    }

    public string GetQrCodeUri(string secret, string email, string issuer = "VTOS")
    {
        var encoded = Uri.EscapeDataString(issuer);
        var encodedEmail = Uri.EscapeDataString(email);
        return $"otpauth://totp/{encoded}:{encodedEmail}?secret={secret}&issuer={encoded}&digits=6&period=30";
    }

    public bool VerifyCode(string secret, string code)
    {
        if (string.IsNullOrWhiteSpace(code) || code.Length != 6) return false;

        try
        {
            var keyBytes = Base32Encoding.ToBytes(secret);
            var totp = new Totp(keyBytes, step: 30, totpSize: 6);
            return totp.VerifyTotp(code, out _, new VerificationWindow(previous: 1, future: 1));
        }
        catch
        {
            return false;
        }
    }

    public (List<string> PlainCodes, string HashedCodesJson) GenerateRecoveryCodes(int count = 8)
    {
        var plainCodes = new List<string>();
        var hashedCodes = new List<string>();

        for (var i = 0; i < count; i++)
        {
            // Generate 8-char alphanumeric code (e.g., "A1B2-C3D4")
            var part1 = GenerateRandomAlphanumeric(4);
            var part2 = GenerateRandomAlphanumeric(4);
            var code = $"{part1}-{part2}";
            plainCodes.Add(code);
            hashedCodes.Add(HashCode(code));
        }

        return (plainCodes, JsonSerializer.Serialize(hashedCodes));
    }

    public (bool IsValid, string? UpdatedHashedCodesJson) ValidateRecoveryCode(string code, string hashedCodesJson)
    {
        if (string.IsNullOrWhiteSpace(hashedCodesJson)) return (false, null);

        try
        {
            var hashedCodes = JsonSerializer.Deserialize<List<string>>(hashedCodesJson);
            if (hashedCodes == null || hashedCodes.Count == 0) return (false, null);

            var normalizedCode = code.Trim().ToUpperInvariant();
            var hash = HashCode(normalizedCode);
            var idx = hashedCodes.IndexOf(hash);

            if (idx < 0) return (false, null);

            // Remove used code
            hashedCodes.RemoveAt(idx);
            return (true, JsonSerializer.Serialize(hashedCodes));
        }
        catch
        {
            return (false, null);
        }
    }

    private static string HashCode(string code)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(code.Trim().ToUpperInvariant()));
        return Convert.ToBase64String(bytes);
    }

    private static string GenerateRandomAlphanumeric(int length)
    {
        const string chars = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789"; // No O/0/I/1 for clarity
        var result = new char[length];
        for (var i = 0; i < length; i++)
        {
            result[i] = chars[RandomNumberGenerator.GetInt32(chars.Length)];
        }
        return new string(result);
    }
}
