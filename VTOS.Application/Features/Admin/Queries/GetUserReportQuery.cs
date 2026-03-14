using VTOS.Application.Features.Admin.DTOs;

namespace VTOS.Application.Features.Admin.Queries;

public record GetUserReportQuery(
    DateTime? DateFrom = null,
    DateTime? DateTo = null,
    string? Role = null,
    string? Status = null
);

public interface IGetUserReportQueryHandler
{
    Task<UserReportDto> HandleAsync(
        GetUserReportQuery query,
        CancellationToken cancellationToken);
}
