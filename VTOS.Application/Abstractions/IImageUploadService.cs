namespace VTOS.Application.Abstractions;

/// <summary>
/// Service for uploading images to cloud storage
/// </summary>
public interface IImageUploadService
{
    /// <summary>
    /// Uploads an image and returns the public URL
    /// </summary>
    /// <param name="imageStream">The image stream to upload</param>
    /// <param name="fileName">Original file name</param>
    /// <param name="folder">Optional subfolder (e.g., "schools", "avatars", "tryon")</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Public URL of the uploaded image</returns>
    Task<string> UploadAsync(Stream imageStream, string fileName, string? folder = null, CancellationToken cancellationToken = default);
}
