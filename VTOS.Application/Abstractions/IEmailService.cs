namespace VTOS.Application.Abstractions;

/// <summary>
/// Interface for email service to send OTP codes and other emails.
/// </summary>
public interface IEmailService
{
    /// <summary>
    /// Sends an OTP code to the specified email address.
    /// </summary>
    Task SendOTPEmailAsync(string toEmail, string otpCode, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends a password reset link to the specified email address.
    /// </summary>
    Task SendPasswordResetEmailAsync(string toEmail, string resetLink, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends an OTP code for password change confirmation.
    /// </summary>
    Task SendChangePasswordOTPEmailAsync(string toEmail, string otpCode, CancellationToken cancellationToken = default);
}
