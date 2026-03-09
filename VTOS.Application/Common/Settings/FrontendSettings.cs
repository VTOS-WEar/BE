namespace VTOS.Application.Common.Settings;

/// <summary>
/// Configuration settings for frontend application URLs
/// </summary>
public class FrontendSettings
{
    public const string SectionName = "FrontendSettings";

    /// <summary>
    /// Base URL of the frontend application (e.g., https://vtos.com)
    /// </summary>
    public string BaseUrl { get; set; } = string.Empty;
}
