using Microsoft.AspNetCore.Http;

namespace VTOS.Application.Features.TryOn.Commands.GuestTryOn;

/// <summary>
/// Command for guest virtual try-on request
/// </summary>
public record GuestTryOnCommand(
    /// <summary>
    /// Optional guest session ID for rate limiting
    /// </summary>
    string? GuestSessionId,
    
    /// <summary>
    /// ID of the outfit to try on
    /// </summary>
    Guid OutfitId,
    
    /// <summary>
    /// User's photo to apply the outfit to
    /// </summary>
    IFormFile Photo
);
