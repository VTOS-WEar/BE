namespace VTOS.Application.Features.Schools.DTOs;

/// <summary>
/// DTO for school sales report (UC-49).
/// </summary>
public class SalesReportDto
{
    public decimal TotalRevenue { get; set; }
    public int TotalOrders { get; set; }
    public decimal AvgOrderValue { get; set; }
    public List<MonthlySalesDto> MonthlySales { get; set; } = new();
    public List<TopOutfitDto> TopOutfits { get; set; } = new();
}

public class MonthlySalesDto
{
    public int Year { get; set; }
    public int Month { get; set; }
    public decimal Revenue { get; set; }
    public int OrderCount { get; set; }
}

public class TopOutfitDto
{
    public Guid OutfitId { get; set; }
    public string OutfitName { get; set; } = string.Empty;
    public int TotalSold { get; set; }
    public decimal Revenue { get; set; }
}
