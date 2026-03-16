using VTOS.Application.Features.Admin.DTOs;

namespace VTOS.Application.Features.Admin.Queries;

public record GetTotalRevenueQuery(
    DateTime? DateFrom = null,
    DateTime? DateTo = null
);

public interface IGetTotalRevenueQueryHandler
{
    Task<TotalRevenueReportDto> HandleAsync(
        GetTotalRevenueQuery query,
        CancellationToken cancellationToken);
}
