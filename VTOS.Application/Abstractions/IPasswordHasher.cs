namespace VTOS.Application.Abstractions;

/// <summary>
/// Interface for password hashing service.
/// Implementation uses BCrypt for secure password storage.
/// </summary>
public interface IPasswordHasher
{
    /// <summary>
    /// Hashes a plain text password.
    /// </summary>
    string HashPassword(string password);
    
    /// <summary>
    /// Verifies a password against a hash.
    /// </summary>
    bool VerifyPassword(string hash, string password);
}
