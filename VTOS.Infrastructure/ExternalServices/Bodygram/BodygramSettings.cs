namespace VTOS.Infrastructure.Bodygram;

/// <summary>
/// Bodygram API configuration settings.
/// </summary>
public class BodygramSettings
{
    public const string SectionName = "BodygramSettings";
    
    /// <summary>
    /// API Key for Bodygram authentication
    /// </summary>
    public string ApiKey { get; set; } = string.Empty;
    
    /// <summary>
    /// Organization ID for Bodygram
    /// </summary>
    public string OrganizationId { get; set; } = string.Empty;
    
    /// <summary>
    /// Base URL for Bodygram API
    /// </summary>
    public string BaseUrl { get; set; } = "https://platform.bodygram.com/api";
}
