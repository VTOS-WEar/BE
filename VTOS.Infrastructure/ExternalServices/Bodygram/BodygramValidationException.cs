using VTOS.Application.Common.Models.BodygramDTOs;

namespace VTOS.Infrastructure.Bodygram;

/// <summary>
/// Exception thrown when Bodygram API validation fails
/// Contains detailed error information for client response
/// </summary>
public class BodygramValidationException : Exception
{
    /// <summary>
    /// Formatted validation errors from Bodygram
    /// </summary>
    public List<FormattedBodygramError> Errors { get; }

    public BodygramValidationException(List<FormattedBodygramError> errors)
        : base(CreateMessage(errors))
    {
        Errors = errors;
    }

    /// <summary>
    /// Creates a summary message from all errors
    /// </summary>
    private static string CreateMessage(List<FormattedBodygramError> errors)
    {
        return $"Bodygram validation failed with {errors.Count} error(s)";
    }
}
