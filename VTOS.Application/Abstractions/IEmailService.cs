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

    /// <summary>
    /// Sends account credentials to a newly created School/Provider user.
    /// </summary>
    Task SendAccountCredentialsEmailAsync(string toEmail, string tempPassword, string roleName, CancellationToken cancellationToken = default);

    // ── Phase 02: Email Notifications ──

    /// <summary>
    /// Sends order confirmation email to parent after successful payment.
    /// </summary>
    Task SendOrderConfirmationEmailAsync(
        string toEmail, string parentName, string orderCode,
        decimal totalAmount, string campaignName,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends payment deadline reminder to parent (6h before 24h deadline).
    /// </summary>
    Task SendPaymentDeadlineReminderAsync(
        string toEmail, string parentName, string orderCode,
        decimal amount, DateTime deadline,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends campaign deadline reminder to parents whose children are in the school.
    /// </summary>
    Task SendCampaignDeadlineReminderAsync(
        string toEmail, string parentName,
        string campaignName, string schoolName, DateTime endDate,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends notification when a contract is approved or rejected.
    /// </summary>
    Task SendContractReplyNotificationAsync(
        string toEmail, string recipientName,
        string contractName, string action, string respondentName,
        CancellationToken cancellationToken = default);
}
