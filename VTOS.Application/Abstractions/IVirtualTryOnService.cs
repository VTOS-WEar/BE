namespace VTOS.Application.Abstractions;

/// <summary>
/// Service for virtual try-on AI processing
/// </summary>
public interface IVirtualTryOnService
{
    /// <summary>
    /// Process a virtual try-on request
    /// </summary>
    /// <param name="humanImageUrl">Public URL of the human/model photo</param>
    /// <param name="garmentImageUrl">Public URL of the garment/outfit image</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Result containing the try-on image URL or error</returns>
    Task<TryOnResult> ProcessAsync(string humanImageUrl, string garmentImageUrl, CancellationToken cancellationToken = default);
}

/// <summary>
/// Result of a virtual try-on operation
/// </summary>
public record TryOnResult(bool Success, string? ImageUrl, string? Error);
