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
    private readonly ILogger<TryOnController> _logger;

    public TryOnController(
        IGuestTryOnCommandHandler guestTryOnHandler,
        IValidator<GuestTryOnCommand> guestTryOnValidator,
        IGetParentTryOnHistoryQueryHandler historyHandler,
        ICurrentUserService currentUser,
        ILogger<TryOnController> logger)
    {
        _guestTryOnHandler = guestTryOnHandler;
        _guestTryOnValidator = guestTryOnValidator;
        _historyHandler = historyHandler;
        _currentUser = currentUser;
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
