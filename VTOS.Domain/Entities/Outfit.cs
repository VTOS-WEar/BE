using VTOS.Domain.Common;
using VTOS.Domain.Enums;

namespace VTOS.Domain.Entities;

public class Outfit : AuditableEntity
{
    public Guid SchoolID { get; set; }
    public string OutfitName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? MaterialType { get; set; }
    public decimal Price { get; set; }
    public OutfitType OutfitType { get; set; }
    public string? MainImageURL { get; set; }
    public Guid? SizeChartID { get; set; }
    public bool IsAvailable { get; set; }
    public bool IsCustomizable { get; set; }
    public bool IsDeleted { get; set; }

    // Navigation properties
    public School School { get; set; } = null!;
    public SizeChart? SizeChart { get; set; }
    public ICollection<ProductVariant> ProductVariants { get; set; } = new List<ProductVariant>();
    public ICollection<OutfitCategory> OutfitCategories { get; set; } = new List<OutfitCategory>();
    public ICollection<TryOnHistory> TryOnHistories { get; set; } = new List<TryOnHistory>();
    public ICollection<OutfitRecommendation> OutfitRecommendations { get; set; } = new List<OutfitRecommendation>();
    public ICollection<CampaignOutfit> CampaignOutfits { get; set; } = new List<CampaignOutfit>();
    public ICollection<SemesterPublicationOutfit> SemesterPublicationOutfits { get; set; } = new List<SemesterPublicationOutfit>();
}

