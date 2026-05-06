using VTOS.Domain.Common;
using VTOS.Domain.Enums;

namespace VTOS.Domain.Entities;

public class ProviderCatalogItem : AuditableEntity
{
    public Guid ProviderID { get; set; }
    public Guid ContractItemID { get; set; }
    public Guid OutfitID { get; set; }
    public Guid SemesterPublicationProviderID { get; set; }
    public Guid? SizeChartID { get; set; }
    public string DisplayName { get; set; } = string.Empty;
    public string? ShortDescription { get; set; }
    public string? FullDescription { get; set; }
    public string? MaterialDetails { get; set; }
    public string? CareInstructions { get; set; }
    public string? MainImageUrl { get; set; }
    public string? GalleryImageUrls { get; set; }
    public decimal PublicationPrice { get; set; }
    public decimal PostDeadlinePrice { get; set; }
    public ProviderCatalogItemStatus Status { get; set; } = ProviderCatalogItemStatus.Draft;
    public DateTime? PublishedAt { get; set; }
    public DateTime? HiddenAt { get; set; }

    public Provider Provider { get; set; } = null!;
    public ContractItem ContractItem { get; set; } = null!;
    public Outfit Outfit { get; set; } = null!;
    public SemesterPublicationProvider SemesterPublicationProvider { get; set; } = null!;
    public SizeChart? SizeChart { get; set; }
    public ICollection<ProductVariant> ProductVariants { get; set; } = new List<ProductVariant>();
}
