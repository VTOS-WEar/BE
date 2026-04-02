namespace VTOS.Infrastructure.ExternalServices.ImageStorage;

/// <summary>
/// Configuration settings for MinIO S3-compatible storage
/// </summary>
public class MinioSettings
{
    public const string SectionName = "ImageStorage:Minio";

    /// <summary>
    /// MinIO server endpoint (e.g., "media.vtos.homes")
    /// </summary>
    public string Endpoint { get; set; } = string.Empty;

    /// <summary>
    /// Access key (username)
    /// </summary>
    public string AccessKey { get; set; } = string.Empty;

    /// <summary>
    /// Secret key (password)
    /// </summary>
    public string SecretKey { get; set; } = string.Empty;

    /// <summary>
    /// Bucket name for storing media files
    /// </summary>
    public string BucketName { get; set; } = "media";

    /// <summary>
    /// Whether to use SSL/TLS connection
    /// </summary>
    public bool UseSSL { get; set; } = true;

    /// <summary>
    /// Public base URL for accessing uploaded files (e.g., "https://media.vtos.homes")
    /// </summary>
    public string PublicBaseUrl { get; set; } = string.Empty;
}
