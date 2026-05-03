using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Minio;
using Minio.DataModel.Args;
using VTOS.Application.Abstractions;

namespace VTOS.Infrastructure.ExternalServices.ImageStorage;

/// <summary>
/// Implementation of image upload service using MinIO S3-compatible storage
/// </summary>
public class MinioImageService : IImageUploadService, IPrivateImageStorageService
{
    private readonly IMinioClient _minioClient;
    private readonly MinioSettings _settings;
    private readonly ILogger<MinioImageService> _logger;
    private static bool _bucketEnsured = false;

    public MinioImageService(
        IOptions<MinioSettings> settings,
        ILogger<MinioImageService> logger)
    {
        _settings = settings.Value;
        _logger = logger;

        _minioClient = new MinioClient()
            .WithEndpoint(_settings.Endpoint)
            .WithCredentials(_settings.AccessKey, _settings.SecretKey)
            .WithSSL(_settings.UseSSL)
            .Build();
    }

    public async Task<string> UploadAsync(
        Stream imageStream,
        string fileName,
        string? folder = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Uploading image to MinIO: {FileName}, Folder: {Folder}", fileName, folder ?? "root");

            // Ensure bucket exists (once per service lifetime)
            await EnsureBucketExistsAsync(cancellationToken);

            var uploaded = await UploadPrivateAsync(
                imageStream,
                fileName,
                folder,
                contentType: null,
                cancellationToken);

            // Build public URL
            var publicUrl = $"{_settings.PublicBaseUrl.TrimEnd('/')}/{_settings.BucketName}/{uploaded.ObjectKey}";

            _logger.LogInformation("Image uploaded successfully to MinIO. URL: {Url}", publicUrl);
            return publicUrl;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading image to MinIO. FileName: {FileName}, Folder: {Folder}, Bucket: {Bucket}",
                fileName, folder ?? "root", _settings.BucketName);
            throw new InvalidOperationException($"Image upload failed: {ex.Message}", ex);
        }
    }

    public async Task<PrivateImageUploadResult> UploadPrivateAsync(
        Stream imageStream,
        string fileName,
        string? folder = null,
        string? contentType = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Uploading private image to MinIO: {FileName}, Folder: {Folder}", fileName, folder ?? "root");

            await EnsureBucketExistsAsync(cancellationToken);

            var extension = Path.GetExtension(fileName).ToLowerInvariant();
            var datePath = DateTime.UtcNow.ToString("yyyy/MM/dd");
            var uniqueName = $"{Guid.NewGuid():N}{extension}";
            var objectName = string.IsNullOrWhiteSpace(folder)
                ? $"{datePath}/{uniqueName}"
                : $"{folder.Trim('/')}/{datePath}/{uniqueName}";

            var resolvedContentType = string.IsNullOrWhiteSpace(contentType)
                ? DetectContentType(extension)
                : contentType;

            if (imageStream.CanSeek)
                imageStream.Position = 0;

            var objectSize = imageStream.CanSeek ? imageStream.Length : 0;

            await _minioClient.PutObjectAsync(
                new PutObjectArgs()
                    .WithBucket(_settings.BucketName)
                    .WithObject(objectName)
                    .WithStreamData(imageStream)
                    .WithObjectSize(objectSize)
                    .WithContentType(resolvedContentType),
                cancellationToken);

            _logger.LogInformation("Private image uploaded to MinIO. ObjectKey: {ObjectKey}", objectName);
            return new PrivateImageUploadResult(objectName, resolvedContentType, objectSize);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading private image to MinIO. FileName: {FileName}, Folder: {Folder}, Bucket: {Bucket}",
                fileName, folder ?? "root", _settings.BucketName);
            throw new InvalidOperationException($"Private image upload failed: {ex.Message}", ex);
        }
    }

    public async Task<string> CreateReadUrlAsync(
        string objectKey,
        TimeSpan expiresIn,
        CancellationToken cancellationToken = default)
    {
        await EnsureBucketExistsAsync(cancellationToken);

        var seconds = Math.Max(1, (int)Math.Ceiling(expiresIn.TotalSeconds));
        return await _minioClient.PresignedGetObjectAsync(
            new PresignedGetObjectArgs()
                .WithBucket(_settings.BucketName)
                .WithObject(objectKey)
                .WithExpiry(seconds));
    }

    public async Task<byte[]> DownloadAsync(
        string objectKey,
        CancellationToken cancellationToken = default)
    {
        await EnsureBucketExistsAsync(cancellationToken);

        using var destination = new MemoryStream();
        await _minioClient.GetObjectAsync(
            new GetObjectArgs()
                .WithBucket(_settings.BucketName)
                .WithObject(objectKey)
                .WithCallbackStream(stream => stream.CopyTo(destination)),
            cancellationToken);

        return destination.ToArray();
    }

    private static string DetectContentType(string extension) => extension switch
    {
        ".jpg" or ".jpeg" => "image/jpeg",
        ".png" => "image/png",
        ".webp" => "image/webp",
        ".gif" => "image/gif",
        ".svg" => "image/svg+xml",
        _ => "application/octet-stream"
    };

    private async Task EnsureBucketExistsAsync(CancellationToken cancellationToken)
    {
        if (_bucketEnsured) return;

        var found = await _minioClient.BucketExistsAsync(
            new BucketExistsArgs().WithBucket(_settings.BucketName),
            cancellationToken);

        if (!found)
        {
            _logger.LogInformation("Creating MinIO bucket: {Bucket}", _settings.BucketName);
            await _minioClient.MakeBucketAsync(
                new MakeBucketArgs().WithBucket(_settings.BucketName),
                cancellationToken);
        }

        _bucketEnsured = true;
    }
}
