namespace VTOS.Application.Features.TryOn.Commands.GuestTryOn;

/// <summary>
/// Response for guest virtual try-on request
/// </summary>
public record GuestTryOnResponse(
    /// <summary>
    /// ID of the try-on history record
    /// </summary>
    Guid TryOnId,
    
    /// <summary>
    /// URL of the generated try-on result image
    /// </summary>
    string ResultPhotoUrl,
    
    /// <summary>
    /// Guest session ID for tracking
    /// </summary>
    string GuestSessionId,
    
    /// <summary>
    /// Number of remaining tries for this session today
    /// </summary>
    int RemainingTries
);
