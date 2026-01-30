namespace VTOS.Infrastructure.ExternalServices.TryOn;

/// <summary>
/// Configuration settings for 302.ai Virtual Try-On API
/// </summary>
public class VirtualTryOnSettings
{
    public const string SectionName = "TryOnSettings";

    /// <summary>
    /// API Key for 302.ai service
    /// </summary>
    public string ApiKey { get; set; } = string.Empty;

    /// <summary>
    /// API endpoint URL
    /// </summary>
    public string ApiUrl { get; set; } = "https://api.302.ai/302/submit/virtual-tryon-v2";

    /// <summary>
    /// Maximum number of try-on attempts per guest session per day
    /// </summary>
    public int MaxGuestTriesPerSession { get; set; } = 5;
}
