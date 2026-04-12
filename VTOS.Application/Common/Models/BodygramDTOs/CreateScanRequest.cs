namespace VTOS.Application.Common.Models.BodygramDTOs;

/// <summary>
/// Request DTO for creating a Bodygram scan
/// </summary>
public class CreateScanRequest
{
    /// <summary>
    /// Custom ID for the scan
    /// </summary>
    public string CustomScanId { get; set; } = string.Empty;
    
    /// <summary>
    /// Photo scan data with user measurements
    /// </summary>
    public PhotoScanData PhotoScan { get; set; } = new();
}

/// <summary>
/// Photo scan data with user information
/// </summary>
public class PhotoScanData
{
    /// <summary>
    /// Age of the person in the photos (in years)
    /// </summary>
    public int Age { get; set; }
    
    /// <summary>
    /// Weight in grams (e.g., 54000 for 54kg)
    /// </summary>
    public int Weight { get; set; }
    
    /// <summary>
    /// Height in millimeters (e.g., 1640 for 164cm)
    /// </summary>
    public int Height { get; set; }
    
    /// <summary>
    /// Gender: "male" or "female"
    /// </summary>
    public string Gender { get; set; } = string.Empty;
    
    /// <summary>
    /// Front photo encoded in base64
    /// </summary>
    public string FrontPhoto { get; set; } = string.Empty;
    
    /// <summary>
    /// Right side photo encoded in base64
    /// </summary>
    public string RightPhoto { get; set; } = string.Empty;
}
