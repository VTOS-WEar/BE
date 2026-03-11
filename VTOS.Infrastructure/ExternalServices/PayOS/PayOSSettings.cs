namespace VTOS.Infrastructure.ExternalServices.PayOS;

/// <summary>
/// Configuration settings for PayOS API
/// </summary>
public class PayOSSettings
{
    public const string SectionName = "PayOSSettings";

    /// <summary>
    /// Client ID for PayOS service
    /// </summary>
    public string ClientId { get; set; } = string.Empty;

    /// <summary>
    /// API Key for PayOS service
    /// </summary>
    public string ApiKey { get; set; } = string.Empty;

    /// <summary>
    /// Checksum Key for PayOS service
    /// </summary>
    public string ChecksumKey { get; set; } = string.Empty;

    /// <summary>
    /// Payout Client ID for PayOS service
    /// </summary>
    public string PayoutClientId { get; set; } = string.Empty;
    /// <summary>
    /// Payment API Prefix for PayOS service (e.g. "v2/payment-requests")
    /// </summary>
    public string PaymentApiPrefix { get; set; } = string.Empty;

    /// <summary>
    /// Payout API Key for PayOS service
    /// </summary>
    public string PayoutApiKey { get; set; } = string.Empty;

    /// <summary>
    /// Payout Checksum Key for PayOS service
    /// </summary>
    public string PayoutChecksumKey { get; set; } = string.Empty;
    /// <summary>
    /// Payout API Prefix for PayOS service (e.g. "https://api-payout.payos.vn")
    /// </summary>
    public string PayoutApiPrefix { get; set; } = string.Empty;

    /// <summary>
    /// API endpoint URL
    /// </summary>
    public string ApiUrl { get; set; } = string.Empty;
}
