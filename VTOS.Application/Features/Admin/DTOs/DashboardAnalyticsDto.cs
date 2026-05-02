namespace VTOS.Application.Features.Admin.DTOs;

public record DashboardAnalyticsDto(
    int TotalUsers,
    int TotalSchools,
    int TotalProviders,
    int TotalParents,
    int TotalOrders,
    decimal TotalRevenue,
    int PendingApprovals,
    int PendingWithdrawals,
    List<MonthlyOrderDto> OrdersPerMonth,
    List<MonthlyRevenueDto> RevenuePerMonth,
    List<MonthlyUserDto> UsersPerMonth,
    List<StatusBreakdownDto> OrderStatusBreakdown,
    List<StatusBreakdownDto> PaymentStatusBreakdown,
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

public record MonthlyUserDto(
    string Month,
    string Role,
    int UserCount
);

public record StatusBreakdownDto(
    string Status,
    int Count,
    decimal TotalAmount
);

public record TopSellingUniformDto(
    Guid OutfitId,
    string OutfitName,
    int QuantitySold,
    decimal Revenue
);
