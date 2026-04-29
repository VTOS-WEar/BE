namespace VTOS.Application.Features.Providers.DTOs;

public class ProviderCatalogResponse
{
    public List<ProviderCatalogPublicationDto> Publications { get; set; } = new();
}

public class ProviderCatalogPublicationDto
{
    public Guid SemesterPublicationProviderId { get; set; }
    public Guid SemesterPublicationId { get; set; }
    public Guid SchoolId { get; set; }
    public string SchoolName { get; set; } = string.Empty;
    public string Semester { get; set; } = string.Empty;
    public string AcademicYear { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public string PublicationStatus { get; set; } = string.Empty;
    public string ProviderStatus { get; set; } = string.Empty;
    public Guid? ContractId { get; set; }
    public string? ContractName { get; set; }
    public string? ContractNumber { get; set; }
    public List<ProviderCatalogItemDto> Items { get; set; } = new();
}

public class ProviderCatalogItemDto
{
    public Guid? CatalogItemId { get; set; }
    public Guid ContractItemId { get; set; }
    public Guid OutfitId { get; set; }
    public string OutfitName { get; set; } = string.Empty;
    public string? OutfitImageUrl { get; set; }
    public string? SchoolMaterialType { get; set; }
    public decimal ContractPricePerUnit { get; set; }
    public string DisplayName { get; set; } = string.Empty;
    public string? ShortDescription { get; set; }
    public string? MaterialDetails { get; set; }
    public decimal? PublicationPrice { get; set; }
    public decimal? PostDeadlinePrice { get; set; }
    public string Status { get; set; } = "Draft";
}

public class UpsertProviderCatalogItemRequest
{
    public string? DisplayName { get; set; }
    public string? ShortDescription { get; set; }
    public string? MaterialDetails { get; set; }
    public decimal PublicationPrice { get; set; }
    public decimal PostDeadlinePrice { get; set; }
    public string Status { get; set; } = "Draft";
}
