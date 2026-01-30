using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VTOS.Application.Features.TryOn.Commands.GuestTryOn;

namespace VTOS.API.Controllers;

/// <summary>
/// Request DTO for guest try-on
/// </summary>
public class GuestTryOnRequest
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
    /// Optional session ID for rate limiting
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
    private readonly ILogger<TryOnController> _logger;

    public TryOnController(
        IGuestTryOnCommandHandler guestTryOnHandler,
        IValidator<GuestTryOnCommand> guestTryOnValidator,
        ILogger<TryOnController> logger)
    {
        _guestTryOnHandler = guestTryOnHandler;
        _guestTryOnValidator = guestTryOnValidator;
        _logger = logger;
    }

    /// <summary>
    /// Perform virtual try-on for guest users
    /// </summary>
    /// <param name="request">Try-on request with outfit ID and photo</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Try-on result with generated image URL</returns>
    /// <response code="200">Try-on successful</response>
    /// <response code="400">Invalid input (validation error)</response>
    /// <response code="404">Outfit not found or unavailable</response>
    /// <response code="429">Rate limit exceeded (max 5 per session per day)</response>
    [HttpPost("guest")]
    [AllowAnonymous]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(GuestTryOnResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    public async Task<IActionResult> GuestTryOn(
        [FromForm] GuestTryOnRequest request,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Guest try-on request. OutfitId: {OutfitId}, Session: {SessionId}", 
            request.OutfitId, request.GuestSessionId ?? "new");

        var command = new GuestTryOnCommand(request.GuestSessionId, request.OutfitId, request.Photo);

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
            // Return appropriate status code based on error
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
}
