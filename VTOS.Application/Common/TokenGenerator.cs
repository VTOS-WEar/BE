using System.Security.Cryptography;
using System.Text;

namespace VTOS.Application.Common;

/// <summary>
/// Utility class for generating and hashing secure tokens.
/// </summary>
public static class TokenGenerator
{
    /// <summary>
    /// Generates a cryptographically secure random token.
    /// </summary>
    /// <param name="length">Length of the token in characters (default: 64)</param>
    /// <returns>A URL-safe random string</returns>
    public static string GenerateSecureToken(int length = 64)
    {
        var randomBytes = new byte[length];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomBytes);
        
        // Convert to URL-safe Base64 (no + / =)
        return Convert.ToBase64String(randomBytes)
            .Replace("+", "-")
            .Replace("/", "_")
            .Replace("=", "")
            .Substring(0, length);
    }

    /// <summary>
    /// Hashes a token using SHA-256.
    /// </summary>
    /// <param name="token">The raw token to hash</param>
    /// <returns>Hexadecimal string of the hash (64 characters)</returns>
    public static string HashToken(string token)
    {
        using var sha256 = SHA256.Create();
        var bytes = Encoding.UTF8.GetBytes(token);
        var hash = sha256.ComputeHash(bytes);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }
}
