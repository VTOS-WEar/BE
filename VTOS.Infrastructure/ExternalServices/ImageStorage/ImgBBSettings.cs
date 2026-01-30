namespace VTOS.Infrastructure.ExternalServices.ImageStorage;

/// <summary>
/// Configuration settings for ImgBB image hosting service
/// </summary>
public class ImgBBSettings
{
    public const string SectionName = "ImageStorage:ImgBB";

    /// <summary>
    /// API Key for ImgBB service
    /// </summary>
    public string ApiKey { get; set; } = string.Empty;

    /// <summary>
    /// API endpoint URL
    /// </summary>
    public string ApiUrl { get; set; } = "https://api.imgbb.com/1/upload";

    /// <summary>
    /// Image expiration in seconds (0 = never expire)
    /// </summary>
    public int Expiration { get; set; } = 0;
}
