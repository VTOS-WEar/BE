namespace VTOS.Application.Common.Models.BodygramDTOs;

/// <summary>
/// Response DTO for Bodygram scan operations
/// </summary>
public class BodygramScanResponse
{
    /// <summary>
    /// The scan entry data
    /// </summary>
    public ScanEntry? Entry { get; set; }
}

/// <summary>
/// Individual scan entry from Bodygram
/// </summary>
public class ScanEntry
{
    /// <summary>
    /// Unique scan ID from Bodygram
    /// </summary>
    public string Id { get; set; } = string.Empty;
    
    /// <summary>
    /// Custom ID provided when creating the scan
    /// </summary>
    public string CustomScanId { get; set; } = string.Empty;
    
    /// <summary>
    /// Scan creation timestamp (Unix)
    /// </summary>
    public long CreatedAt { get; set; }
    
    /// <summary>
    /// Status of the scan: "success", "processing", "failed", etc.
    /// </summary>
    public string Status { get; set; } = string.Empty;
    
    /// <summary>
    /// Avatar data with 3D model
    /// </summary>
    public AvatarData? Avatar { get; set; }
    
    /// <summary>
    /// Input data that was sent for the scan
    /// </summary>
    public InputData? Input { get; set; }
    
    /// <summary>
    /// Body measurements extracted from the scan
    /// </summary>
    public List<Measurement> Measurements { get; set; } = new();
}

/// <summary>
/// Avatar data containing the 3D model
/// </summary>
public class AvatarData
{
    /// <summary>
    /// Base64 encoded 3D model (OBJ format)
    /// </summary>
    public string Data { get; set; } = string.Empty;
    
    /// <summary>
    /// Format of the avatar data (e.g., "obj")
    /// </summary>
    public string Format { get; set; } = string.Empty;
    
    /// <summary>
    /// Type of avatar (e.g., "highResolution")
    /// </summary>
    public string Type { get; set; } = string.Empty;
}

/// <summary>
/// Input data that was used for the scan
/// </summary>
public class InputData
{
    /// <summary>
    /// Photo scan input data
    /// </summary>
    public PhotoScanInput? PhotoScan { get; set; }
}

/// <summary>
/// Photo scan input data (returned from API)
/// </summary>
public class PhotoScanInput
{
    public int Age { get; set; }
    public int Weight { get; set; }
    public int Height { get; set; }
    public string Gender { get; set; } = string.Empty;
    
    /// <summary>
    /// Photos are not returned in the response for privacy
    /// </summary>
    public string? FrontPhoto { get; set; }
    
    /// <summary>
    /// Photos are not returned in the response for privacy
    /// </summary>
    public string? RightPhoto { get; set; }
}

/// <summary>
/// Individual measurement extracted from the scan
/// </summary>
public class Measurement
{
    /// <summary>
    /// Name of the measurement (e.g., "acrossBackShoulderWidth")
    /// </summary>
    public string Name { get; set; } = string.Empty;
    
    /// <summary>
    /// Unit of measurement (e.g., "mm")
    /// </summary>
    public string Unit { get; set; } = string.Empty;
    
    /// <summary>
    /// Numeric value of the measurement
    /// </summary>
    public double Value { get; set; }
}

/// <summary>
/// List response for scans
/// </summary>
public class ScanListResponse
{
    /// <summary>
    /// List of scans returned from the API
    /// </summary>
    public List<ScanEntry> Results { get; set; } = new();
}
