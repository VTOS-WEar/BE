using VTOS.Domain.Common;

namespace VTOS.Domain.Entities;

/// <summary>
/// Represents a planned distribution session for a production batch.
/// School can schedule when to distribute uniforms (AtSchool or AtHome).
/// </summary>
public class DistributionSchedule : BaseEntity
{
    public Guid BatchID { get; set; }
    public DateTime ScheduledDate { get; set; }
    public string Method { get; set; } = "AtSchool"; // "AtSchool" or "AtHome"
    public string TimeSlot { get; set; } = "AllDay"; // "Morning", "Afternoon", "AllDay"
    public string? Note { get; set; }
    public string Status { get; set; } = "Planned"; // "Planned", "InProgress", "Completed"
    public DateTime CreatedAt { get; set; }
    public DateTime? CompletedAt { get; set; }

    // Navigation properties
    public ProductionBatch Batch { get; set; } = null!;
}
