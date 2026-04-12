using Microsoft.AspNetCore.Http;

namespace VTOS.API.Controllers.Dtos;

/// <summary>
/// Request DTO for creating a Bodygram scan via HTTP form submission
/// </summary>
public class CreateBodygramScanRequest
{
    /// <summary>
    /// Front view photo of the person
    /// </summary>
    public IFormFile FrontPhoto { get; set; } = null!;

    /// <summary>
    /// Right side view photo of the person
    /// </summary>
    public IFormFile RightPhoto { get; set; } = null!;

    /// <summary>
    /// Age in years (1-150)
    /// </summary>
    public int Age { get; set; }

    /// <summary>
    /// Weight in kilograms (kg) - User input format
    /// Example: 54.5 for 54.5kg
    /// </summary>
    public decimal Weight { get; set; }

    /// <summary>
    /// Height in centimeters (cm) - User input format
    /// Example: 164 for 164cm
    /// </summary>
    public int Height { get; set; }

    /// <summary>
    /// Gender: "male" or "female"
    /// </summary>
    public string Gender { get; set; } = string.Empty;

    /// <summary>
    /// Optional custom scan ID (if not provided, will be auto-generated)
    /// </summary>
    public string? CustomScanId { get; set; }
}
