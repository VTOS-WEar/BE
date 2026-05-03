using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using VTOS.Application.Abstractions;
using VTOS.Application.Common;
using VTOS.Domain.Entities;

namespace VTOS.Application.Features.TryOn.Commands.GuestTryOn;

/// <summary>
/// Settings for try-on feature
/// </summary>
public class TryOnSettings
{
    public const string SectionName = "TryOnSettings";
    public string ApiKey { get; set; } = string.Empty;
    public string ApiUrl { get; set; } = string.Empty;
    public int MaxGuestTriesPerSession { get; set; } = 5;
}

/// <summary>
/// Handler for guest virtual try-on command
/// </summary>
public class GuestTryOnCommandHandler : IGuestTryOnCommandHandler
{
    private readonly IApplicationDbContext _context;
    private readonly IPrivateImageStorageService _privateImageStorageService;
    private readonly IImageDownloadService _imageDownloadService;
    private readonly ITryOnImageAccessService _tryOnImageAccessService;
    private readonly IVirtualTryOnService _virtualTryOnService;
    private readonly ILogger<GuestTryOnCommandHandler> _logger;
    private readonly int _maxTriesPerSession;

    public GuestTryOnCommandHandler(
        IApplicationDbContext context,
        IPrivateImageStorageService privateImageStorageService,
        IImageDownloadService imageDownloadService,
        ITryOnImageAccessService tryOnImageAccessService,
        IVirtualTryOnService virtualTryOnService,
        ILogger<GuestTryOnCommandHandler> logger,
        IOptions<TryOnSettings> settings)
    {
        _context = context;
        _privateImageStorageService = privateImageStorageService;
        _imageDownloadService = imageDownloadService;
        _tryOnImageAccessService = tryOnImageAccessService;
        _virtualTryOnService = virtualTryOnService;
        _logger = logger;
        _maxTriesPerSession = settings.Value.MaxGuestTriesPerSession;
    }

    public async Task<Result<GuestTryOnResponse>> HandleAsync(GuestTryOnCommand command, CancellationToken cancellationToken = default)
    {
        // Step 1: Generate or use existing guest session ID
        var guestSessionId = string.IsNullOrEmpty(command.GuestSessionId) 
            ? Guid.NewGuid().ToString() 
            : command.GuestSessionId;

        _logger.LogInformation("Processing guest try-on for session: {SessionId}, outfit: {OutfitId}", 
            guestSessionId, command.OutfitId);

        // Step 2: Check rate limit
        var today = DateTime.UtcNow.Date;
        int tryCount;
        if (command.UserId.HasValue)
        {
            // Logged-in user: rate limit by UserID
            tryCount = await _context.TryOnHistories
                .CountAsync(t => t.UserID == command.UserId.Value
                    && t.TryOnTimestamp.Date == today, cancellationToken);
        }
        else
        {
            // Guest: rate limit by session ID
            tryCount = await _context.TryOnHistories
                .CountAsync(t => t.GuestSessionID == guestSessionId 
                    && t.TryOnTimestamp.Date == today, cancellationToken);
        }

        if (tryCount >= _maxTriesPerSession)
        {
            _logger.LogWarning("Rate limit exceeded for session: {SessionId}. Count: {Count}", 
                guestSessionId, tryCount);
            return Result<GuestTryOnResponse>.Failure(
                $"Maximum {_maxTriesPerSession} tries per session per day. Try again tomorrow.",
                "RATE_LIMIT_EXCEEDED");
        }

        // Step 3: Validate outfit exists and is available
        var outfit = await _context.Outfits
            .AsNoTracking()
            .FirstOrDefaultAsync(o => o.Id == command.OutfitId && o.IsAvailable, cancellationToken);

        if (outfit == null)
        {
            return Result<GuestTryOnResponse>.Failure(
                "Outfit not found or unavailable.",
                "OUTFIT_NOT_FOUND");
        }

        // Step 4: Upload human photo to get public URL
        PrivateImageUploadResult humanImage;
        string humanImageUrl;
        try
        {
            await using var stream = command.Photo.OpenReadStream();
            humanImage = await _privateImageStorageService.UploadPrivateAsync(
                stream, 
                command.Photo.FileName, 
                "tryon",
                command.Photo.ContentType,
                cancellationToken);

            humanImageUrl = await _privateImageStorageService.CreateReadUrlAsync(
                humanImage.ObjectKey,
                TimeSpan.FromMinutes(5),
                cancellationToken);

            _logger.LogDebug("Human photo uploaded privately: {ObjectKey}", humanImage.ObjectKey);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to upload human photo");
            return Result<GuestTryOnResponse>.Failure(
                "Failed to upload photo. Please try again.",
                "UPLOAD_FAILED");
        }

        // Step 5: Get garment image URL from outfit
        var garmentImageUrl = outfit.MainImageURL;

        if (string.IsNullOrEmpty(garmentImageUrl))
        {
            return Result<GuestTryOnResponse>.Failure(
                "Outfit does not have a main image.",
                "OUTFIT_NO_IMAGE");
        }

        // Step 6: Call virtual try-on AI service
        var tryOnResult = await _virtualTryOnService.ProcessAsync(
            humanImageUrl, 
            garmentImageUrl, 
            cancellationToken);

        if (!tryOnResult.Success || string.IsNullOrEmpty(tryOnResult.ImageUrl))
        {
            _logger.LogError("Virtual try-on failed: {Error}", tryOnResult.Error);
            return Result<GuestTryOnResponse>.Failure(
                $"Try-on processing failed: {tryOnResult.Error}",
                "TRYON_FAILED");
        }

        // Step 7: Upload the generated result to private MinIO storage.
        var resultImageUrl = tryOnResult.ImageUrl;
        PrivateImageUploadResult resultImage;
        try
        {
            resultImage = await StoreTryOnResultAsync(resultImageUrl, cancellationToken);
            _logger.LogInformation("Try-on result uploaded privately: {ObjectKey}", resultImage.ObjectKey);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to secure try-on result image");
            return Result<GuestTryOnResponse>.Failure(
                "Failed to secure try-on result image. Please try again.",
                "TRYON_RESULT_STORAGE_FAILED");
        }

        // Step 8: Save to TryOnHistory
        var history = new TryOnHistory
        {
            GuestSessionID = guestSessionId,
            UserID = command.UserId,
            OutfitID = command.OutfitId,
            UploadedPhotoURL = string.Empty,
            ResultPhotoURL = null,
            UploadedPhotoObjectKey = humanImage.ObjectKey,
            UploadedPhotoContentType = humanImage.ContentType,
            UploadedPhotoSizeBytes = humanImage.SizeBytes,
            ResultPhotoObjectKey = resultImage.ObjectKey,
            ResultPhotoContentType = resultImage.ContentType,
            ResultPhotoSizeBytes = resultImage.SizeBytes,
            TryOnTimestamp = DateTime.UtcNow,
            SourcePlatform = "Web"
        };

        _context.TryOnHistories.Add(history);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Guest try-on completed. TryOnId: {TryOnId}, Session: {SessionId}", 
            history.Id, guestSessionId);

        // Step 9: Return result with remaining tries
        var remainingTries = _maxTriesPerSession - (tryCount + 1);
        var secureResultUrl = _tryOnImageAccessService.CreateImageUrl(history, TryOnImageAssetKind.Result);

        return Result<GuestTryOnResponse>.Success(new GuestTryOnResponse(
            history.Id,
            secureResultUrl ?? string.Empty,
            guestSessionId,
            remainingTries
        ));
    }

    private async Task<PrivateImageUploadResult> StoreTryOnResultAsync(
        string resultImageUrl,
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
            var fileName = $"tryon-result-{Guid.NewGuid():N}.{ext}";

            return await _privateImageStorageService.UploadPrivateAsync(
                ms,
                fileName,
                "tryon-results",
                mimeType,
                cancellationToken);
        }

        var downloaded = await _imageDownloadService.DownloadAsync(resultImageUrl, cancellationToken);
        using var downloadedStream = new MemoryStream(downloaded.Bytes);
        return await _privateImageStorageService.UploadPrivateAsync(
            downloadedStream,
            downloaded.FileName,
            "tryon-results",
            downloaded.ContentType,
            cancellationToken);
    }
}
