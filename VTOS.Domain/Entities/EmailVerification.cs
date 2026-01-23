using VTOS.Domain.Common;

namespace VTOS.Domain.Entities;

/// <summary>
/// Represents an email verification record with OTP code.
/// Used for email verification during registration.
/// </summary>
public class EmailVerification : BaseEntity
{
    public string Email { get; set; } = string.Empty;
    public string OTPCode { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
    public bool IsVerified { get; set; }
    public DateTime CreatedAt { get; set; }
}
