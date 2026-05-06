using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using VTOS.Application.Abstractions;
using VTOS.Application.Features.Notifications;
using VTOS.Domain.Enums;

namespace VTOS.Infrastructure.BackgroundJobs;

public class ParentTryOnJobWorker : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<ParentTryOnJobWorker> _logger;
    private static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(5);
    private static readonly TimeSpan ProcessingTimeout = TimeSpan.FromMinutes(30);

    public ParentTryOnJobWorker(IServiceProvider serviceProvider, ILogger<ParentTryOnJobWorker> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("ParentTryOnJobWorker started.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await RunOnceAsync(stoppingToken);
                await Task.Delay(PollInterval, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in ParentTryOnJobWorker cycle");
                await Task.Delay(PollInterval, stoppingToken);
            }
        }
    }

    private async Task RunOnceAsync(CancellationToken cancellationToken)
    {
        for (var i = 0; i < 2; i++)
        {
            using var claimScope = _serviceProvider.CreateScope();
            var jobId = await ClaimNextJobAsync(claimScope.ServiceProvider, cancellationToken);
            if (jobId == null)
            {
                return;
            }

            using var jobScope = _serviceProvider.CreateScope();
            await ProcessJobAsync(jobScope.ServiceProvider, jobId.Value, cancellationToken);
        }
    }

    private async Task<Guid?> ClaimNextJobAsync(IServiceProvider services, CancellationToken cancellationToken)
    {
        var db = services.GetRequiredService<IApplicationDbContext>();
        var staleProcessingCutoff = DateTime.UtcNow.Subtract(ProcessingTimeout);

        for (var attempts = 0; attempts < 5; attempts++)
        {
            var candidateId = await db.TryOnHistories
                .AsNoTracking()
                .Where(t => t.UserID != null
                    && (t.Status == TryOnJobStatus.Queued
                        || (t.Status == TryOnJobStatus.Processing
                            && (t.UpdatedAt == null || t.UpdatedAt <= staleProcessingCutoff))
                        || (t.Status == TryOnJobStatus.Completed
                            && t.CompletedAt == null
                            && (t.ResultPhotoObjectKey == null || t.ResultPhotoObjectKey == "")
                            && (t.ResultPhotoURL == null || t.ResultPhotoURL == "")
                            && t.UploadedPhotoObjectKey != null
                            && t.UploadedPhotoObjectKey != "")))
                .OrderBy(t => t.TryOnTimestamp)
                .Select(t => (Guid?)t.Id)
                .FirstOrDefaultAsync(cancellationToken);

            if (candidateId == null)
            {
                return null;
            }

            var now = DateTime.UtcNow;
            var claimed = await db.TryOnHistories
                .Where(t => t.Id == candidateId.Value
                    && t.UserID != null
                    && (t.Status == TryOnJobStatus.Queued
                        || (t.Status == TryOnJobStatus.Processing
                            && (t.UpdatedAt == null || t.UpdatedAt <= staleProcessingCutoff))
                        || (t.Status == TryOnJobStatus.Completed
                            && t.CompletedAt == null
                            && (t.ResultPhotoObjectKey == null || t.ResultPhotoObjectKey == "")
                            && (t.ResultPhotoURL == null || t.ResultPhotoURL == "")
                            && t.UploadedPhotoObjectKey != null
                            && t.UploadedPhotoObjectKey != "")))
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(t => t.Status, TryOnJobStatus.Processing)
                    .SetProperty(t => t.ErrorMessage, (string?)null)
                    .SetProperty(t => t.CompletedAt, (DateTime?)null)
                    .SetProperty(t => t.UpdatedAt, now),
                    cancellationToken);

            if (claimed == 1)
            {
                return candidateId.Value;
            }
        }

        return null;
    }

    private async Task ProcessJobAsync(IServiceProvider services, Guid tryOnId, CancellationToken cancellationToken)
    {
        var db = services.GetRequiredService<IApplicationDbContext>();
        var privateImageStorage = services.GetRequiredService<IPrivateImageStorageService>();
        var imageDownloadService = services.GetRequiredService<IImageDownloadService>();
        var virtualTryOnService = services.GetRequiredService<IVirtualTryOnService>();
        var notificationService = services.GetRequiredService<INotificationService>();

        var job = await db.TryOnHistories
            .Include(t => t.Outfit)
            .FirstOrDefaultAsync(t => t.Id == tryOnId, cancellationToken);

        if (job == null || job.Status != TryOnJobStatus.Processing)
        {
            return;
        }

        try
        {
            if (job.UserID == null)
            {
                throw new InvalidOperationException("Try-on job does not belong to a parent user.");
            }

            if (string.IsNullOrWhiteSpace(job.UploadedPhotoObjectKey))
            {
                throw new InvalidOperationException("Try-on job source photo is missing.");
            }

            if (job.Outfit == null || string.IsNullOrWhiteSpace(job.Outfit.MainImageURL))
            {
                throw new InvalidOperationException("Outfit image is missing.");
            }

            var humanImageUrl = await privateImageStorage.CreateReadUrlAsync(
                job.UploadedPhotoObjectKey,
                TimeSpan.FromMinutes(5),
                cancellationToken);

            var tryOnResult = await virtualTryOnService.ProcessAsync(
                humanImageUrl,
                job.Outfit.MainImageURL,
                cancellationToken);

            if (!tryOnResult.Success || string.IsNullOrEmpty(tryOnResult.ImageUrl))
            {
                throw new InvalidOperationException(tryOnResult.Error ?? "Try-on processing failed.");
            }

            var resultImage = await StoreTryOnResultAsync(
                tryOnResult.ImageUrl,
                privateImageStorage,
                imageDownloadService,
                cancellationToken);

            job.ResultPhotoURL = null;
            job.ResultPhotoObjectKey = resultImage.ObjectKey;
            job.ResultPhotoContentType = resultImage.ContentType;
            job.ResultPhotoSizeBytes = resultImage.SizeBytes;
            job.Status = TryOnJobStatus.Completed;
            job.CompletedAt = DateTime.UtcNow;
            job.UpdatedAt = DateTime.UtcNow;
            await db.SaveChangesAsync(cancellationToken);

            try
            {
                await notificationService.CreateAsync(
                    job.UserID.Value,
                    "Ảnh thử đồ đã sẵn sàng",
                    $"Kết quả thử đồ cho {job.Outfit.OutfitName} đã hoàn tất.",
                    "TryOn",
                    job.Id,
                    "TryOnHistory",
                    $"/parentprofile/history?tryOnId={job.Id}",
                    cancellationToken);
            }
            catch (Exception notificationEx)
            {
                _logger.LogWarning(notificationEx, "Parent try-on job completed but notification failed. TryOnId: {TryOnId}", job.Id);
            }

            _logger.LogInformation("Parent try-on job completed. TryOnId: {TryOnId}", job.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Parent try-on job failed. TryOnId: {TryOnId}", job.Id);

            job.Status = TryOnJobStatus.Failed;
            job.ErrorMessage = NormalizeError(ex.Message);
            job.CompletedAt = DateTime.UtcNow;
            job.UpdatedAt = DateTime.UtcNow;
            await db.SaveChangesAsync(cancellationToken);

            if (job.UserID.HasValue)
            {
                try
                {
                    await notificationService.CreateAsync(
                        job.UserID.Value,
                        "Thử đồ chưa thành công",
                        $"Không thể tạo ảnh thử đồ cho {job.Outfit?.OutfitName ?? "đồng phục này"}. Vui lòng thử lại.",
                        "TryOn",
                        job.Id,
                        "TryOnHistory",
                        $"/parentprofile/history?tryOnId={job.Id}",
                        cancellationToken);
                }
                catch (Exception notificationEx)
                {
                    _logger.LogWarning(notificationEx, "Parent try-on job failed and failure notification could not be created. TryOnId: {TryOnId}", job.Id);
                }
            }
        }
    }

    private static async Task<PrivateImageUploadResult> StoreTryOnResultAsync(
        string resultImageUrl,
        IPrivateImageStorageService privateImageStorage,
        IImageDownloadService imageDownloadService,
        CancellationToken cancellationToken)
    {
        if (resultImageUrl.StartsWith("data:image/", StringComparison.OrdinalIgnoreCase))
        {
            var commaIdx = resultImageUrl.IndexOf(',');
            if (commaIdx < 0)
            {
                throw new InvalidOperationException("Try-on result data URL is malformed.");
            }

            var base64Data = resultImageUrl[(commaIdx + 1)..];
            var headerPart = resultImageUrl[..commaIdx];
            var mimeType = headerPart.Replace("data:", string.Empty).Replace(";base64", string.Empty);
            var ext = mimeType.Contains("png", StringComparison.OrdinalIgnoreCase)
                ? "png"
                : mimeType.Contains("webp", StringComparison.OrdinalIgnoreCase)
                    ? "webp"
                    : "jpg";

            var imageBytes = Convert.FromBase64String(base64Data);
            using var ms = new MemoryStream(imageBytes);
            return await privateImageStorage.UploadPrivateAsync(
                ms,
                $"tryon-result-{Guid.NewGuid():N}.{ext}",
                "tryon-results",
                mimeType,
                cancellationToken);
        }

        var downloaded = await imageDownloadService.DownloadAsync(resultImageUrl, cancellationToken);
        using var downloadedStream = new MemoryStream(downloaded.Bytes);
        return await privateImageStorage.UploadPrivateAsync(
            downloadedStream,
            downloaded.FileName,
            "tryon-results",
            downloaded.ContentType,
            cancellationToken);
    }

    private static string NormalizeError(string message)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            return "Có lỗi xảy ra khi xử lý ảnh thử đồ.";
        }

        return message.Length > 1000 ? message[..1000] : message;
    }
}
