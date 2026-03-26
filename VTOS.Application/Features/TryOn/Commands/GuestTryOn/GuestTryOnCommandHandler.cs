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
    private readonly IImageUploadService _imageUploadService;
    private readonly IVirtualTryOnService _virtualTryOnService;
    private readonly ILogger<GuestTryOnCommandHandler> _logger;
    private readonly int _maxTriesPerSession;

    public GuestTryOnCommandHandler(
        IApplicationDbContext context,
        IImageUploadService imageUploadService,
        IVirtualTryOnService virtualTryOnService,
        ILogger<GuestTryOnCommandHandler> logger,
        IOptions<TryOnSettings> settings)
    {
        _context = context;
        _imageUploadService = imageUploadService;
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

        // Step 2: Check rate limit (5 tries per session per day)
        var today = DateTime.UtcNow.Date;
        var tryCount = await _context.TryOnHistories
            .CountAsync(t => t.GuestSessionID == guestSessionId 
                && t.TryOnTimestamp.Date == today, cancellationToken);

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
        string humanImageUrl;
        try
        {
            await using var stream = command.Photo.OpenReadStream();
            humanImageUrl = await _imageUploadService.UploadAsync(
                stream, 
                command.Photo.FileName, 
                "tryon",
                cancellationToken);

            _logger.LogDebug("Human photo uploaded: {Url}", humanImageUrl);
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

        // Step 7: Save to TryOnHistory
        var history = new TryOnHistory
        {
            GuestSessionID = guestSessionId,
            OutfitID = command.OutfitId,
            UploadedPhotoURL = humanImageUrl,
            ResultPhotoURL = tryOnResult.ImageUrl,
            TryOnTimestamp = DateTime.UtcNow,
            SourcePlatform = "Web"
        };

        _context.TryOnHistories.Add(history);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Guest try-on completed. TryOnId: {TryOnId}, Session: {SessionId}", 
            history.Id, guestSessionId);

        // Step 8: Return result with remaining tries
        var remainingTries = _maxTriesPerSession - (tryCount + 1);

        return Result<GuestTryOnResponse>.Success(new GuestTryOnResponse(
            history.Id,
            tryOnResult.ImageUrl,
            guestSessionId,
            remainingTries
        ));
    }
}
