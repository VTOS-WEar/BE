namespace VTOS.Application.Common.Models.BodygramDTOs;

/// <summary>
/// Error response from Bodygram API when scan validation fails
/// </summary>
public class BodygramErrorResponse
{
    /// <summary>
    /// List of validation errors
    /// </summary>
    public List<BodygramError> Errors { get; set; } = new();
}

/// <summary>
/// Individual error from Bodygram validation
/// </summary>
public class BodygramError
{
    /// <summary>
    /// Error code (e.g., "noEXIF", "invalidPhotoFormat", "faceNotDetected", etc.)
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Error category: "format", "quality", "posing", "face", "person", or "other"
    /// </summary>
    public string Type { get; set; } = string.Empty;

    /// <summary>
    /// Human-readable explanation of what went wrong
    /// </summary>
    public string Explanation { get; set; } = string.Empty;
}

/// <summary>
/// Formatted error response for client
/// </summary>
public class FormattedBodygramError
{
    /// <summary>
    /// Error category for UI grouping/styling
    /// </summary>
    public string Type { get; set; } = string.Empty;

    /// <summary>
    /// Error code identifier
    /// </summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>
    /// User-friendly error message
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// Additional guidance for fixing the error
    /// </summary>
    public string? Suggestion { get; set; }
}
