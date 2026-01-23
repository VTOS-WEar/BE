using VTOS.Domain.Entities;

namespace VTOS.Application.Abstractions;

/// <summary>
/// Interface for JWT token generation.
/// </summary>
public interface IJwtTokenGenerator
{
    /// <summary>
    /// Generates a JWT token for the given user.
    /// </summary>
    /// <param name="user">The user to generate token for.</param>
    /// <returns>The JWT token string.</returns>
    string GenerateToken(User user);
    
    /// <summary>
    /// Gets the expiry time in minutes.
    /// </summary>
    int GetExpiryMinutes();
}
