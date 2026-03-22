using VTOS.Domain.Entities;

namespace VTOS.Application.Abstractions;

/// <summary>
/// Interface for JWT token generation.
/// </summary>
public interface IJwtTokenGenerator
{
    /// <summary>Generates a JWT token for the given user.</summary>
    string GenerateToken(User user, Guid? providerId = null, Guid? schoolId = null);
    
    /// <summary>Gets the expiry time in minutes.</summary>
    int GetExpiryMinutes();

    /// <summary>Generates a short-lived token for 2FA verification (5 min TTL).</summary>
    string GenerateTwoFactorToken(Guid userId);

    /// <summary>Validates a 2FA temp token and returns the userId if valid.</summary>
    Guid? ValidateTwoFactorToken(string token);
}
