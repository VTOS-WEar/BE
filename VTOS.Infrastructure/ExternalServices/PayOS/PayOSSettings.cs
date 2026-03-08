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
    /// Payout API Key for PayOS service
    /// </summary>
    public string PayoutApiKey { get; set; } = string.Empty;

    /// <summary>
    /// Payout Checksum Key for PayOS service
    /// </summary>
    public string PayoutChecksumKey { get; set; } = string.Empty;

    /// <summary>
    /// API endpoint URL
    /// </summary>
    public string ApiUrl { get; set; } = "https://api-merchant.payos.vn";
}
