using VTOS.Application.Features.Admin.DTOs;

namespace VTOS.Application.Features.Admin.Queries;

public record GetTotalOrdersQuery(
    DateTime? DateFrom = null,
    DateTime? DateTo = null
);

public interface IGetTotalOrdersQueryHandler
{
    Task<TotalOrdersReportDto> HandleAsync(
        GetTotalOrdersQuery query,
        CancellationToken cancellationToken);
}
