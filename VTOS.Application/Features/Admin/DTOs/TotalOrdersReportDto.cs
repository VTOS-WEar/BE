namespace VTOS.Application.Features.Admin.DTOs;

public record TotalOrdersReportDto(
    int TotalOrders,
    int CompletedOrders,
    int PendingOrders,
    int CancelledOrders,
    List<OrderByStatusDto> OrdersByStatus,
    List<OrderBySchoolDto> OrdersBySchool,
    List<OrderByMonthDto> OrdersByMonth
);

public record OrderByStatusDto(
    string Status,
    int Count,
    decimal Percentage
);

public record OrderBySchoolDto(
    Guid SchoolId,
    string SchoolName,
    int OrderCount,
    decimal TotalAmount
);

public record OrderByMonthDto(
    string Month,
    int OrderCount,
    decimal TotalAmount
);
