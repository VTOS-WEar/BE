using Microsoft.Extensions.Logging;
using VTOS.Application.Abstractions;

namespace VTOS.Infrastructure.ExternalServices.ImageStorage;

public class ImageDownloadService : IImageDownloadService
{
    private const int MaxImageBytes = 20 * 1024 * 1024;

    private readonly HttpClient _httpClient;
    private readonly ILogger<ImageDownloadService> _logger;

    public ImageDownloadService(HttpClient httpClient, ILogger<ImageDownloadService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<DownloadedImage> DownloadAsync(string imageUrl, CancellationToken cancellationToken = default)
    {
        if (!Uri.TryCreate(imageUrl, UriKind.Absolute, out var uri) ||
            (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
        {
            throw new InvalidOperationException("Image URL is not a valid HTTP(S) URL.");
        }

        using var response = await _httpClient.GetAsync(uri, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        response.EnsureSuccessStatusCode();

        var contentLength = response.Content.Headers.ContentLength;
        if (contentLength > MaxImageBytes)
        {
            throw new InvalidOperationException("Downloaded image is too large.");
        }

        var bytes = await response.Content.ReadAsByteArrayAsync(cancellationToken);
        if (bytes.Length > MaxImageBytes)
        {
            throw new InvalidOperationException("Downloaded image is too large.");
        }

        var contentType = response.Content.Headers.ContentType?.MediaType ?? DetectContentType(bytes);
        var fileName = Path.GetFileName(uri.LocalPath);
        if (string.IsNullOrWhiteSpace(fileName) || !Path.HasExtension(fileName))
        {
            fileName = $"tryon-result-{Guid.NewGuid():N}{ExtensionFor(contentType)}";
        }

        _logger.LogInformation("Downloaded try-on result image. Url: {Url}, Bytes: {Bytes}, ContentType: {ContentType}",
            uri.GetLeftPart(UriPartial.Path), bytes.Length, contentType);

        return new DownloadedImage(bytes, fileName, contentType);
    }

    private static string DetectContentType(byte[] bytes)
    {
        if (bytes.Length >= 12 &&
            bytes[0] == 0x52 && bytes[1] == 0x49 && bytes[2] == 0x46 && bytes[3] == 0x46 &&
            bytes[8] == 0x57 && bytes[9] == 0x45 && bytes[10] == 0x42 && bytes[11] == 0x50)
            return "image/webp";

        if (bytes.Length >= 8 &&
            bytes[0] == 0x89 && bytes[1] == 0x50 && bytes[2] == 0x4E && bytes[3] == 0x47)
            return "image/png";

        if (bytes.Length >= 3 && bytes[0] == 0xFF && bytes[1] == 0xD8 && bytes[2] == 0xFF)
            return "image/jpeg";

        if (bytes.Length >= 4 && bytes[0] == 0x47 && bytes[1] == 0x49 && bytes[2] == 0x46 && bytes[3] == 0x38)
            return "image/gif";

        return "application/octet-stream";
    }

    private static string ExtensionFor(string contentType) => contentType.ToLowerInvariant() switch
    {
        "image/png" => ".png",
        "image/webp" => ".webp",
        "image/gif" => ".gif",
        _ => ".jpg"
    };
}
