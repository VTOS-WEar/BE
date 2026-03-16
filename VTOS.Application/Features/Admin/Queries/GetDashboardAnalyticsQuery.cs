using VTOS.Application.Features.Admin.DTOs;

namespace VTOS.Application.Features.Admin.Queries;

public record GetDashboardAnalyticsQuery(
    string TimeRange = "Month" // "Week", "Month", "Year"
);

public interface IGetDashboardAnalyticsQueryHandler
{
    Task<DashboardAnalyticsDto> HandleAsync(
        GetDashboardAnalyticsQuery query,
        CancellationToken cancellationToken);
}
