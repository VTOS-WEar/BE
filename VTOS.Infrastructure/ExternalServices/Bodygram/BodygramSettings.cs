namespace VTOS.Infrastructure.Bodygram;

/// <summary>
/// Bodygram API credential pair (ApiKey + OrganizationId)
/// </summary>
public class BodygramCredential
{
    public string ApiKey { get; set; } = string.Empty;
    public string OrganizationId { get; set; } = string.Empty;
}

/// <summary>
/// Bodygram API configuration settings with fallback credentials.
/// </summary>
public class BodygramSettings
{
    public const string SectionName = "BodygramSettings";
    
    /// <summary>
    /// Primary API Key for Bodygram authentication (deprecated, use Credentials[0])
    /// </summary>
    public string ApiKey { get; set; } = string.Empty;
    
    /// <summary>
    /// Primary Organization ID for Bodygram (deprecated, use Credentials[0])
    /// </summary>
    public string OrganizationId { get; set; } = string.Empty;
    
    /// <summary>
    /// Base URL for Bodygram API
    /// </summary>
    public string BaseUrl { get; set; } = "https://platform.bodygram.com/api";
    
    /// <summary>
    /// List of credential pairs (primary + fallback) for retry logic.
    /// Index 0 = primary, 1-2 = backup credentials
    /// </summary>
    public List<BodygramCredential> Credentials { get; set; } = new();
}
