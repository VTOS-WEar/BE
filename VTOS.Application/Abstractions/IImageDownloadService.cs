namespace VTOS.Application.Abstractions;

public record DownloadedImage(
    byte[] Bytes,
    string FileName,
    string ContentType);

public interface IImageDownloadService
{
    Task<DownloadedImage> DownloadAsync(string imageUrl, CancellationToken cancellationToken = default);
}
