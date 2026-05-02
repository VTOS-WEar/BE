using VTOS.Domain.Enums;

namespace VTOS.Application.Features.Schools.DTOs;

/// <summary>
/// DTO representing an outfit owned by a school.
/// </summary>
public class OutfitDto
{
    public Guid OutfitId { get; set; }
    public string OutfitName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? MaterialType { get; set; }
    public decimal Price { get; set; }
    public OutfitType OutfitType { get; set; }
    public Guid? CategoryId { get; set; }
    public string? CategoryName { get; set; }
    public string? MainImageURL { get; set; }
    public Guid? SizeChartID { get; set; }
    public bool IsAvailable { get; set; }
    public bool IsCustomizable { get; set; }
    public bool CanDelete { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

public class OutfitListResponse
{
    public List<OutfitDto> Items { get; set; } = new();
    public int Total { get; set; }
    public int TotalCount { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; }
    public int TotalPages { get; set; }
    public OutfitListSummaryDto Summary { get; set; } = new();
}

public class OutfitListSummaryDto
{
    public int Total { get; set; }
    public int Available { get; set; }
    public int Unavailable { get; set; }
}
