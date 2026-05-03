namespace VTOS.Application.Abstractions;

public record PrivateImageUploadResult(
    string ObjectKey,
    string ContentType,
    long SizeBytes);

public interface IPrivateImageStorageService
{
    Task<PrivateImageUploadResult> UploadPrivateAsync(
        Stream imageStream,
        string fileName,
        string? folder = null,
        string? contentType = null,
        CancellationToken cancellationToken = default);

    Task<string> CreateReadUrlAsync(
        string objectKey,
        TimeSpan expiresIn,
        CancellationToken cancellationToken = default);

    Task<byte[]> DownloadAsync(
        string objectKey,
        CancellationToken cancellationToken = default);
}
