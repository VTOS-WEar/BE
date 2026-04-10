using System.Text.Json;
using VTOS.Application.Common.Models.BodygramDTOs;

namespace VTOS.Infrastructure.Bodygram.Helpers;

/// <summary>
/// Helper class to handle and format Bodygram API errors
/// </summary>
public static class BodygramErrorHandler
{
    /// <summary>
    /// Error type guidance - suggestions for each error type
    /// </summary>
    private static readonly Dictionary<string, string> ErrorTypeGuidance = new()
    {
        ["format"] = "Consult the Posing guidelines for photo format requirements.",
        ["quality"] = "Take pictures again in a well-lit area with clear visibility.",
        ["posing"] = "Follow the posing instructions in the Posing guidelines and retake the photos.",
        ["face"] = "Ensure your face is visible and centered in the photo.",
        ["person"] = "Verify only one person is in the photo and follow all positioning requirements.",
        ["other"] = "Please wait and retry the scan. If the error persists, contact support."
    };

    /// <summary>
    /// Error-specific suggestions for common issues
    /// </summary>
    private static readonly Dictionary<string, string> ErrorSpecificSuggestions = new()
    {
        ["noEXIF"] = "Ensure your camera has EXIF data enabled or use a modern smartphone camera.",
        ["invalidPhotoFormat"] = "Only JPEG format (.jpg or .jpeg) is accepted. Convert or retake photos.",
        ["imageTooDark"] = "Take pictures in a brightly lit area or increase lighting and retake photos.",
        ["imageTooBright"] = "Reduce backlighting or take photos in a dimmer area and retake photos.",
        ["imageTooBlurry"] = "Stabilize your device using a tripod or hold steady when capturing.",
        ["faceNotDetected"] = "Ensure your face is visible, centered, and clearly visible in the front photo.",
        ["personNotDetected"] = "Follow the exact posing guidelines - ensure full body is visible and correctly positioned.",
        ["multiplePeopleDetected"] = "Remove other people from the background and retake the photos.",
        ["rightPhotoFacingWrongDirection"] = "In the right photo, you should be facing 90° to the right. Retake the photo.",
        ["frontPhotoNotInFrame"] = "In the front photo, your entire body must be visible. Step back if needed.",
        ["rightPhotoNotInFrame"] = "In the right photo, your entire body must be visible. Step back if needed.",
        ["frontPhotoLeftArmAngle"] = "Position your left arm at your side (not touching body). Retake the photo.",
        ["frontPhotoRightArmAngle"] = "Position your right arm at your side (not touching body). Retake the photo.",
        ["headNotInFrame"] = "Ensure your head is fully visible in the front photo. Adjust camera angle.",
        ["leftArmNotInFrame"] = "Your left arm must be fully visible. Include it in the frame.",
        ["rightArmNotInFrame"] = "Your right arm must be fully visible. Include it in the frame.",
        ["leftLegNotInFrame"] = "Your left leg must be fully visible from hip to toe.",
        ["rightLegNotInFrame"] = "Your right leg must be fully visible from hip to toe.",
        ["feetNotInFrame"] = "Your feet must be fully visible and clearly shown. Adjust your stance."
    };

    /// <summary>
    /// Parses error response from Bodygram API
    /// </summary>
    /// <param name="responseContent">Raw response content from API</param>
    /// <param name="options">JSON serializer options</param>
    /// <returns>Parsed error response or null if parsing fails</returns>
    public static BodygramErrorResponse? ParseErrorResponse(string responseContent, JsonSerializerOptions? options = null)
    {
        try
        {
            options ??= new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
            return JsonSerializer.Deserialize<BodygramErrorResponse>(responseContent, options);
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Formats Bodygram errors into user-friendly response
    /// </summary>
    /// <param name="errors">List of Bodygram errors</param>
    /// <returns>Formatted errors ready to send to client</returns>
    public static List<FormattedBodygramError> FormatErrors(List<BodygramError> errors)
    {
        return errors
            .Select(FormatSingleError)
            .ToList();
    }

    /// <summary>
    /// Formats a single error with guidance and suggestions
    /// </summary>
    private static FormattedBodygramError FormatSingleError(BodygramError error)
    {
        return new FormattedBodygramError
        {
            Type = error.Type,
            Code = error.Name,
            Message = error.Explanation,
            Suggestion = GetSuggestion(error.Name, error.Type)
        };
    }

    /// <summary>
    /// Gets specific suggestion based on error code or type
    /// </summary>
    private static string GetSuggestion(string errorCode, string errorType)
    {
        // Try specific error code suggestion first
        if (ErrorSpecificSuggestions.TryGetValue(errorCode, out var suggestion))
            return suggestion;

        // Fall back to error type guidance
        if (ErrorTypeGuidance.TryGetValue(errorType, out var typeGuidance))
            return typeGuidance;

        return "Please review the photo requirements and retake the photos.";
    }

    /// <summary>
    /// Groups errors by type for organized display
    /// </summary>
    /// <param name="errors">List of formatted errors</param>
    /// <returns>Dictionary of errors grouped by type</returns>
    public static Dictionary<string, List<FormattedBodygramError>> GroupErrorsByType(List<FormattedBodygramError> errors)
    {
        return errors
            .GroupBy(e => e.Type)
            .ToDictionary(
                g => g.Key,
                g => g.ToList()
            );
    }

    /// <summary>
    /// Creates a user-friendly error summary
    /// </summary>
    /// <param name="errors">Formatted errors</param>
    /// <returns>Summary string describing all errors</returns>
    public static string CreateErrorSummary(List<FormattedBodygramError> errors)
    {
        if (!errors.Any())
            return "An unknown error occurred during scan processing.";

        var groupedErrors = GroupErrorsByType(errors);
        var summary = new System.Text.StringBuilder();
        summary.AppendLine("Scan validation failed. Please fix the following issues:");
        summary.AppendLine();

        foreach (var group in groupedErrors)
        {
            summary.AppendLine($"📷 {CapitalizeType(group.Key)} Issues:");
            foreach (var error in group.Value)
            {
                summary.AppendLine($"  • {error.Code}: {error.Message}");
                if (!string.IsNullOrEmpty(error.Suggestion))
                    summary.AppendLine($"    💡 {error.Suggestion}");
            }
            summary.AppendLine();
        }

        return summary.ToString().Trim();
    }

    /// <summary>
    /// Capitalizes error type for display
    /// </summary>
    private static string CapitalizeType(string type) =>
        char.ToUpper(type[0]) + type.Substring(1);
}
