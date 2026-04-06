using VTOS.Domain.Common;
using VTOS.Domain.Enums;

namespace VTOS.Domain.Entities;

public class Campaign : AuditableEntity
{
    public Guid SchoolID { get; set; }
    public string CampaignName { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public CampaignStatus Status { get; set; }
    public string? Description { get; set; }

    // Navigation properties
    public School School { get; set; } = null!;
    public ICollection<CampaignOutfit> CampaignOutfits { get; set; } = new List<CampaignOutfit>();
    public ICollection<Order> Orders { get; set; } = new List<Order>();
    public ICollection<ProductionBatch> ProductionBatches { get; set; } = new List<ProductionBatch>();
    public ICollection<Feedback> Feedbacks { get; set; } = new List<Feedback>();
}

