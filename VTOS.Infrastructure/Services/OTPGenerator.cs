namespace VTOS.Infrastructure.Services;

/// <summary>
/// Utility class for generating OTP codes.
/// </summary>
public static class OTPGenerator
{
    /// <summary>
    /// Generates a 6-digit OTP code.
    /// </summary>
    public static string Generate()
    {
        return Random.Shared.Next(100000, 999999).ToString();
    }
}
