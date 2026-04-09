using Microsoft.AspNetCore.Http;

namespace VTOS.Application.Features.TryOn.Commands.GuestTryOn;

/// <summary>
/// Command for virtual try-on request (both guest and logged-in users)
/// </summary>
public record GuestTryOnCommand(
    /// <summary>
    /// Optional guest session ID for rate limiting (guest users)
    /// </summary>
    string? GuestSessionId,
    
    /// <summary>
    /// ID of the outfit to try on
    /// </summary>
    Guid OutfitId,
    
    /// <summary>
    /// User's photo to apply the outfit to
    /// </summary>
    IFormFile Photo,

    /// <summary>
    /// Optional user ID for logged-in users (links history to their account)
    /// </summary>
    Guid? UserId = null
);
