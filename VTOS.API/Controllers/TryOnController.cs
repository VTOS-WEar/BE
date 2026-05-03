using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VTOS.Application.Abstractions;
using VTOS.Application.Features.TryOn.Commands.GuestTryOn;
using VTOS.Application.Features.TryOn.Queries;

namespace VTOS.API.Controllers;

/// <summary>
/// Request DTO for try-on
/// </summary>
public class TryOnRequest
{
    /// <summary>
    /// ID of the outfit to try on
    /// </summary>
    public Guid OutfitId { get; set; }

    /// <summary>
    /// User's photo (max 10MB, jpg/png/webp)
    /// </summary>
    public IFormFile Photo { get; set; } = null!;

    /// <summary>
    /// Optional session ID for rate limiting (guest users only)
    /// </summary>
    public string? GuestSessionId { get; set; }
}

public class TryOnResultLinkRequest
{
    public string? GuestSessionId { get; set; }
}

/// <summary>
/// Controller for virtual try-on operations
/// </summary>
[ApiController]
[Route("api/tryon")]
public class TryOnController : ControllerBase
{
    private readonly IGuestTryOnCommandHandler _guestTryOnHandler;
    private readonly IValidator<GuestTryOnCommand> _guestTryOnValidator;
    private readonly IGetParentTryOnHistoryQueryHandler _historyHandler;
    private readonly ICurrentUserService _currentUser;
    private readonly ITryOnImageAccessService _tryOnImageAccessService;
    private readonly IPrivateImageStorageService _privateImageStorageService;
    private readonly IImageWatermarkService _imageWatermarkService;
    private readonly ILogger<TryOnController> _logger;

    public TryOnController(
        IGuestTryOnCommandHandler guestTryOnHandler,
        IValidator<GuestTryOnCommand> guestTryOnValidator,
        IGetParentTryOnHistoryQueryHandler historyHandler,
        ICurrentUserService currentUser,
        ITryOnImageAccessService tryOnImageAccessService,
        IPrivateImageStorageService privateImageStorageService,
        IImageWatermarkService imageWatermarkService,
        ILogger<TryOnController> logger)
    {
        _guestTryOnHandler = guestTryOnHandler;
        _guestTryOnValidator = guestTryOnValidator;
        _historyHandler = historyHandler;
        _currentUser = currentUser;
        _tryOnImageAccessService = tryOnImageAccessService;
        _privateImageStorageService = privateImageStorageService;
        _imageWatermarkService = imageWatermarkService;
        _logger = logger;
    }

    /// <summary>
    /// Perform virtual try-on (for both guest and logged-in users)
    /// </summary>
    [HttpPost("request")]
    [AllowAnonymous]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(GuestTryOnResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    public async Task<IActionResult> TryOnRequest(
        [FromForm] TryOnRequest request,
        CancellationToken cancellationToken = default)
    {
        // Extract UserId if authenticated
        Guid? userId = User.Identity?.IsAuthenticated == true ? _currentUser.UserId : null;

        _logger.LogInformation("Try-on request. OutfitId: {OutfitId}, UserId: {UserId}, Session: {SessionId}", 
            request.OutfitId, userId?.ToString() ?? "guest", request.GuestSessionId ?? "new");

        var command = new GuestTryOnCommand(request.GuestSessionId, request.OutfitId, request.Photo, userId);

        // Validate
        var validationResult = await _guestTryOnValidator.ValidateAsync(command, cancellationToken);
        if (!validationResult.IsValid)
        {
            return BadRequest(new { errors = validationResult.Errors.Select(e => e.ErrorMessage) });
        }

        // Execute
        var result = await _guestTryOnHandler.HandleAsync(command, cancellationToken);

        if (!result.IsSuccess)
        {
            return result.ErrorCode switch
            {
                "RATE_LIMIT_EXCEEDED" => StatusCode(StatusCodes.Status429TooManyRequests, 
                    new { error = result.Error, code = result.ErrorCode }),
                "OUTFIT_NOT_FOUND" => NotFound(new { error = result.Error, code = result.ErrorCode }),
                _ => BadRequest(new { error = result.Error, code = result.ErrorCode })
            };
        }

        return Ok(result.Value);
    }

    /// <summary>
    /// Resolve a short-lived try-on image ticket to private image bytes.
    /// </summary>
    [HttpGet("images/{ticket}")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status410Gone)]
    public async Task<IActionResult> GetTryOnImage(
        [FromRoute] string ticket,
        CancellationToken cancellationToken = default)
    {
        var result = await _tryOnImageAccessService.ValidateTicketAsync(ticket, cancellationToken);
        if (!result.IsSuccess)
        {
            return result.ErrorCode switch
            {
                "IMAGE_TICKET_EXPIRED" => StatusCode(StatusCodes.Status410Gone, new { error = result.Error, code = result.ErrorCode }),
                "TRYON_IMAGE_NOT_FOUND" => NotFound(new { error = result.Error, code = result.ErrorCode }),
                _ => StatusCode(StatusCodes.Status403Forbidden, new { error = result.Error, code = result.ErrorCode })
            };
        }

        var bytes = await _privateImageStorageService.DownloadAsync(
            result.Value!.ObjectKey,
            cancellationToken);

        Response.Headers.CacheControl = "private, no-store, max-age=0";
        if (result.Value.AssetKind == TryOnImageAssetKind.Result && result.Value.IsGuest)
        {
            var watermarked = _imageWatermarkService.ApplyTryOnGuestWatermark(bytes);
            return File(watermarked.Bytes, watermarked.ContentType);
        }

        return File(bytes, result.Value.ContentType);
    }

    /// <summary>
    /// Issue a fresh 5-minute link for a try-on result.
    /// </summary>
    [HttpPost("{tryOnId:guid}/result-link")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CreateResultLink(
        [FromRoute] Guid tryOnId,
        [FromBody] TryOnResultLinkRequest? request,
        CancellationToken cancellationToken = default)
    {
        Guid? userId = User.Identity?.IsAuthenticated == true ? _currentUser.UserId : null;
        var result = await _tryOnImageAccessService.CreateResultImageUrlAsync(
            tryOnId,
            userId,
            request?.GuestSessionId,
            cancellationToken);

        if (!result.IsSuccess)
        {
            return result.ErrorCode switch
            {
                "TRYON_NOT_FOUND" or "TRYON_IMAGE_NOT_FOUND" => NotFound(new { error = result.Error, code = result.ErrorCode }),
                "FORBIDDEN" => StatusCode(StatusCodes.Status403Forbidden, new { error = result.Error, code = result.ErrorCode }),
                _ => BadRequest(new { error = result.Error, code = result.ErrorCode })
            };
        }

        return Ok(new { resultPhotoUrl = result.Value });
    }

    /// <summary>
    /// Get try-on history for the current logged-in parent
    /// </summary>
    [HttpGet("history")]
    [Authorize(Roles = "Parent")]
    [ProducesResponseType(typeof(GetParentTryOnHistoryResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetTryOnHistory(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var query = new GetParentTryOnHistoryQuery(_currentUser.UserId, page, pageSize);
        var result = await _historyHandler.HandleAsync(query, cancellationToken);
        if (!result.IsSuccess)
            return BadRequest(new { error = result.Error, code = result.ErrorCode });
        return Ok(result.Value);
    }
}
