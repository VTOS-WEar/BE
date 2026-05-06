namespace VTOS.Application.Abstractions;

public record DirectOrderDeliveryEmailItem(
    string OutfitName,
    string Size,
    int Quantity,
    decimal UnitPrice,
    string? ImageUrl);

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
    /// Sends a delivery confirmation email to parent when a direct order is ready for receipt confirmation.
    /// </summary>
    Task SendDirectOrderReceiptConfirmationEmailAsync(
        string toEmail,
        string parentName,
        string orderCode,
        DateTime orderDate,
        string providerName,
        decimal totalAmount,
        string? shippingCompany,
        string? trackingCode,
        string confirmUrl,
        IReadOnlyList<DirectOrderDeliveryEmailItem> items,
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

    /// <summary>
    /// Sends a 6-digit OTP to the signer's email for contract signing verification.
    /// </summary>
    Task SendContractSignOTPAsync(
        string toEmail, string toName,
        string otpCode, string contractName, string contractNumber,
        int expiresInMinutes,
        CancellationToken cancellationToken = default);

    // ── Phase 03: Admin Notification Digest ──

    /// <summary>
    /// Sends a batched digest email to an Admin user with all unread notifications.
    /// </summary>
    Task SendAdminDigestEmailAsync(
        string toEmail, string adminName,
        IReadOnlyList<(string Title, string Message, DateTime CreatedAt)> notifications,
        CancellationToken cancellationToken = default);

    // ── Teacher Reminder ──

    /// <summary>
    /// Sends a reminder email from teacher to parent about completing uniform orders.
    /// </summary>
    Task SendTeacherReminderEmailAsync(
        string toEmail, string parentName,
        string teacherName, string className,
        string? note,
        CancellationToken cancellationToken = default);

    // ── Chat Digest ──

    /// <summary>
    /// Sends a batched digest email with new chat messages from a channel.
    /// </summary>
    Task SendChatDigestEmailAsync(
        string toEmail, string recipientName,
        string channelLabel, string channelType,
        IReadOnlyList<(string SenderName, string Content, DateTime SentAt)> messages,
        CancellationToken cancellationToken = default);
}
