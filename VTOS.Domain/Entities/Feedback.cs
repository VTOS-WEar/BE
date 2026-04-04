using VTOS.Domain.Common;
using VTOS.Domain.Enums;

namespace VTOS.Domain.Entities;

public class Feedback : AuditableEntity
{
    public Guid UserID { get; set; }
    public Guid ProductVariantID { get; set; }
    public Guid CampaignID { get; set; }

    public int Rating { get; set; }
    public string? Comment { get; set; }
    public DateTime Timestamp { get; set; }
    public ModerationStatus ModerationStatus { get; set; }

    // Navigation properties
    public User User { get; set; } = null!;
    public ProductVariant ProductVariant { get; set; } = null!;
    public Campaign Campaign { get; set; } = null!;
}

