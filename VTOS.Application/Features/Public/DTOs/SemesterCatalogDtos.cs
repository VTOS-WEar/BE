namespace VTOS.Application.Features.Public.DTOs;

public class SchoolSemesterCatalogResponse
{
    public Guid SemesterPublicationId { get; set; }
    public Guid SchoolId { get; set; }
    public string Semester { get; set; } = string.Empty;
    public string AcademicYear { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public bool IsAfterDeadline { get; set; }
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
    public decimal? LowestPublicationPrice { get; set; }
    public decimal? LowestPostDeadlinePrice { get; set; }
    public string OutfitType { get; set; } = string.Empty;
    public List<string> Sizes { get; set; } = new();
    public List<SemesterCatalogProviderDto> Providers { get; set; } = new();
}

public class SemesterCatalogProviderDto
{
    public Guid ProviderId { get; set; }
    public string ProviderName { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string? ShortDescription { get; set; }
    public string? MaterialDetails { get; set; }
    public string? MainImageUrl { get; set; }
    public string? ContactEmail { get; set; }
    public decimal Price { get; set; }
    public decimal PublicationPrice { get; set; }
    public decimal PostDeadlinePrice { get; set; }
    public string PricingMode { get; set; } = string.Empty;
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

public class ProviderRatingItemDto
{
    public Guid ProviderRatingId { get; set; }
    public Guid OrderId { get; set; }
    public int Rating { get; set; }
    public string? Comment { get; set; }
    public DateTime CreatedAt { get; set; }
    public string ParentName { get; set; } = string.Empty;
}

public class ProviderRatingsResponse
{
    public Guid ProviderId { get; set; }
    public string ProviderName { get; set; } = string.Empty;
    public decimal AverageRating { get; set; }
    public int TotalRatings { get; set; }
    public int TotalCompletedOrders { get; set; }
    public List<ProviderRatingItemDto> Items { get; set; } = new();
}

public class ProviderRankingItemDto
{
    public Guid ProviderId { get; set; }
    public string ProviderName { get; set; } = string.Empty;
    public decimal AverageRating { get; set; }
    public int TotalRatings { get; set; }
    public int TotalCompletedOrders { get; set; }
}

public class ProviderRankingResponse
{
    public Guid SchoolId { get; set; }
    public List<ProviderRankingItemDto> Items { get; set; } = new();
}
