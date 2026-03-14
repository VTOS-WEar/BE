namespace VTOS.Application.Features.Admin.DTOs;

public record TotalRevenueReportDto(
    decimal TotalRevenue,
    decimal CompletedPayments,
    decimal FailedPayments,
    List<RevenueBySchoolDto> RevenueBySchool,
    List<RevenueByMonthDto> RevenueByMonth,
    List<RevenueByCampaignDto> RevenueByCampaign
);

public record RevenueBySchoolDto(
    Guid SchoolId,
    string SchoolName,
    decimal Revenue,
    int OrderCount
);

public record RevenueByMonthDto(
    string Month,
    decimal Revenue,
    int OrderCount
);

public record RevenueByCampaignDto(
    Guid CampaignId,
    string CampaignName,
    decimal Revenue
);
