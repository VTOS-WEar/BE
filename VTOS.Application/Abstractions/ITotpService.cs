namespace VTOS.Application.Abstractions;

/// <summary>
/// Interface for TOTP (Time-based One-Time Password) operations.
/// Used for Google Authenticator / Authy 2FA.
/// </summary>
public interface ITotpService
{
    /// <summary>Generates a new random Base32 secret key.</summary>
    string GenerateSecret();

    /// <summary>
    /// Generates otpauth:// URI for QR code scanning.
    /// </summary>
    string GetQrCodeUri(string secret, string email, string issuer = "VTOS");

    /// <summary>
    /// Verifies a 6-digit TOTP code against the secret.
    /// Allows ±1 time step tolerance.
    /// </summary>
    bool VerifyCode(string secret, string code);

    /// <summary>
    /// Generates a set of one-time recovery codes.
    /// Returns plaintext codes (to show user) and their SHA-256 hashes (to store).
    /// </summary>
    (List<string> PlainCodes, string HashedCodesJson) GenerateRecoveryCodes(int count = 8);

    /// <summary>
    /// Validates a recovery code against stored hashed codes.
    /// Returns updated hashed codes JSON with the used code removed, or null if invalid.
    /// </summary>
    (bool IsValid, string? UpdatedHashedCodesJson) ValidateRecoveryCode(string code, string hashedCodesJson);
}
