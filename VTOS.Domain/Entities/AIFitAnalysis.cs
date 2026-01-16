using VTOS.Domain.Common;

namespace VTOS.Domain.Entities;

public class AIFitAnalysis : AuditableEntity
{
    public Guid TryOnID { get; set; }
    public string? DetectedBodyProportions { get; set; }
    public string? SuggestedSize { get; set; }
    public int? FitScore { get; set; }
    public string? AlgorithmVersion { get; set; }

    // Navigation properties
    public TryOnHistory TryOnHistory { get; set; } = null!;
}

