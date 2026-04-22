namespace VTOS.Application.Features.Public.DTOs;

public class SchoolSemesterCatalogResponse
{
    public Guid SemesterPublicationId { get; set; }
    public Guid SchoolId { get; set; }
    public string Semester { get; set; } = string.Empty;
    public string AcademicYear { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public string Status { get; set; } = string.Empty;
    public List<SemesterCatalogOutfitDto> Outfits { get; set; } = new();
}

public class SemesterCatalogOutfitDto
{
    public Guid OutfitId { get; set; }
    public string OutfitName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? MainImageUrl { get; set; }
    public decimal Price { get; set; }
    public string OutfitType { get; set; } = string.Empty;
    public List<string> Sizes { get; set; } = new();
    public List<SemesterCatalogProviderDto> Providers { get; set; } = new();
}

public class SemesterCatalogProviderDto
{
    public Guid ProviderId { get; set; }
    public string ProviderName { get; set; } = string.Empty;
    public string? ContactEmail { get; set; }
    public decimal Price { get; set; }
    public decimal AverageRating { get; set; }
    public int TotalRatings { get; set; }
    public int TotalCompletedOrders { get; set; }
}

public class PublicProviderProfileDto
{
    public Guid ProviderId { get; set; }
    public string ProviderName { get; set; } = string.Empty;
    public string? ContactPersonName { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? Address { get; set; }
    public decimal AverageRating { get; set; }
    public int TotalRatings { get; set; }
    public int TotalCompletedOrders { get; set; }
}
