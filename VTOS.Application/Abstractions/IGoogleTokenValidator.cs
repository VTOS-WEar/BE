namespace VTOS.Application.Abstractions;

/// <summary>
/// Validates Google ID tokens and extracts user info.
/// </summary>
public interface IGoogleTokenValidator
{
    /// <summary>Validates a Google ID token and returns user info if valid.</summary>
    Task<GoogleUserInfo?> ValidateAsync(string idToken);
}

/// <summary>
/// User information extracted from a valid Google ID token.
/// </summary>
public record GoogleUserInfo(string Sub, string Email, string Name, string? Picture);
