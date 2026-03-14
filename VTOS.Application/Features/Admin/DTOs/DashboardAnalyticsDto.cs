namespace VTOS.Application.Features.Admin.DTOs;

public record DashboardAnalyticsDto(
    int TotalUsers,
    int TotalSchools,
    int TotalProviders,
    int TotalOrders,
    decimal TotalRevenue,
    List<MonthlyOrderDto> OrdersPerMonth,
    List<MonthlyRevenueDto> RevenuePerMonth,
    List<TopSellingUniformDto> TopSellingUniforms
);

public record MonthlyOrderDto(
    string Month,
    int OrderCount
);

public record MonthlyRevenueDto(
    string Month,
    decimal Revenue
);

public record TopSellingUniformDto(
    Guid OutfitId,
    string OutfitName,
    int QuantitySold,
    decimal Revenue
);
