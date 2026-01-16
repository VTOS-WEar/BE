using VTOS.Domain.Common;

namespace VTOS.Domain.Entities;

public class TryOnHistory : AuditableEntity
{
    public string? GuestSessionID { get; set; }
    public Guid? UserID { get; set; }
    public Guid? ChildID { get; set; }
    public Guid OutfitID { get; set; }
    public string UploadedPhotoURL { get; set; } = string.Empty;
    public string? ResultPhotoURL { get; set; }
    public DateTime TryOnTimestamp { get; set; }
    public string? AlignmentAdjustment { get; set; }
    public string? SourcePlatform { get; set; }

    // Navigation properties
    public User? User { get; set; }
    public ChildProfile? ChildProfile { get; set; }
    public Outfit Outfit { get; set; } = null!;
    public AIFitAnalysis? AIFitAnalysis { get; set; }
}

