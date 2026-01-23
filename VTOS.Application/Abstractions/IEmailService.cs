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
}
