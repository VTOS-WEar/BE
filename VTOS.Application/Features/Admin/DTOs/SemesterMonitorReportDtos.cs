namespace VTOS.Application.Features.Admin.DTOs;

public record AdminSemesterPublicationOptionDto(
    Guid Id,
    string Semester,
    string AcademicYear,
    Guid SchoolId,
    string SchoolName,
    string Status,
    DateTime StartDate,
    DateTime EndDate,
    int OrderCount
);

public record AdminSemesterPublicationListDto(
    IReadOnlyList<AdminSemesterPublicationOptionDto> Items,
    int TotalCount
);

public record SemesterMonitorPublicationDto(
    Guid Id,
    string Semester,
    string AcademicYear,
    Guid SchoolId,
    string SchoolName,
    string Status,
    DateTime StartDate,
    DateTime EndDate
);

public record SemesterMonitorSummaryDto(
    int TotalOrders,
    int CompletedOrders,
    int RefundedOrders,
    int CancelledOrders,
    int OpenOrders,
    decimal CompletedOrderRate,
    decimal RefundedOrderRate,
    decimal CancelledOrderRate,
    decimal TotalRevenue,
    decimal RefundedAmount,
    int PaymentAttempts,
    int CompletedPayments,
    decimal PaymentCompletionRate
);

public record SemesterMonitorStatusMetricDto(
    string Status,
    int Count,
    decimal Rate,
    decimal TotalAmount
);

public record SemesterMonitorOrderDetailDto(
    Guid OrderId,
    string OrderNumber,
    string StudentName,
    string SchoolName,
    string? ProviderName,
    string OrderStatus,
    string PaymentStatus,
    decimal TotalAmount,
    DateTime CreatedAt,
    DateTime? PaidAt
);

public record SemesterMonitorReportDto(
    SemesterMonitorPublicationDto Publication,
    SemesterMonitorSummaryDto Summary,
    IReadOnlyList<SemesterMonitorStatusMetricDto> OrderStatusBreakdown,
    IReadOnlyList<SemesterMonitorStatusMetricDto> PaymentStatusBreakdown,
    IReadOnlyList<SemesterMonitorOrderDetailDto> Orders
);
