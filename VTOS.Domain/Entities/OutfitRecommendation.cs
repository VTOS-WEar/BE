using VTOS.Domain.Common;

namespace VTOS.Domain.Entities;

public class OutfitRecommendation : AuditableEntity
{
    public Guid UserID { get; set; }
    public Guid OutfitID { get; set; }
    public decimal RecommendationScore { get; set; }
    public Guid? RuleConfigID { get; set; }

    // Navigation properties
    public User User { get; set; } = null!;
    public Outfit Outfit { get; set; } = null!;
}

