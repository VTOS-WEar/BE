using VTOS.Domain.Common;

namespace VTOS.Domain.Entities;

/// <summary>
/// Tracks each student-data import batch (file upload session).
/// </summary>
public class ImportBatch : BaseEntity
{
    public Guid SchoolID { get; set; }
    public string FileName { get; set; } = string.Empty;
    public int TotalRows { get; set; }
    public int SuccessCount { get; set; }
    public int SkippedCount { get; set; }
    public int ErrorCount { get; set; }
    public DateTime CreatedAt { get; set; }

    // Navigation
    public School School { get; set; } = null!;
}
