using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Minio;
using Minio.DataModel.Args;
using VTOS.Application.Abstractions;

namespace VTOS.Infrastructure.ExternalServices.ImageStorage;

/// <summary>
/// Implementation of image upload service using MinIO S3-compatible storage
/// </summary>
public class MinioImageService : IImageUploadService
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

            // Generate unique object name: {folder}/{yyyy/MM/dd}/{guid}.ext
            var extension = Path.GetExtension(fileName).ToLowerInvariant();
            var datePath = DateTime.UtcNow.ToString("yyyy/MM/dd");
            var uniqueName = $"{Guid.NewGuid():N}{extension}";
            var objectName = string.IsNullOrWhiteSpace(folder)
                ? $"{datePath}/{uniqueName}"
                : $"{folder.Trim('/')}/{datePath}/{uniqueName}";

            // Detect content type
            var contentType = extension switch
            {
                ".jpg" or ".jpeg" => "image/jpeg",
                ".png" => "image/png",
                ".webp" => "image/webp",
                ".gif" => "image/gif",
                ".svg" => "image/svg+xml",
                _ => "application/octet-stream"
            };

            // Reset stream position if possible
            if (imageStream.CanSeek)
                imageStream.Position = 0;

            // Upload to MinIO
            await _minioClient.PutObjectAsync(
                new PutObjectArgs()
                    .WithBucket(_settings.BucketName)
                    .WithObject(objectName)
                    .WithStreamData(imageStream)
                    .WithObjectSize(imageStream.Length)
                    .WithContentType(contentType),
                cancellationToken);

            // Build public URL
            var publicUrl = $"{_settings.PublicBaseUrl.TrimEnd('/')}/{_settings.BucketName}/{objectName}";

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
