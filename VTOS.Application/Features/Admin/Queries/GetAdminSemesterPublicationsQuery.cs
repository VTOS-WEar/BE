using VTOS.Application.Common;
using VTOS.Application.Features.Admin.DTOs;

namespace VTOS.Application.Features.Admin.Queries;

public record GetAdminSemesterPublicationsQuery(
    int Page = 1,
    int PageSize = 100,
    string? Status = null
);

public interface IGetAdminSemesterPublicationsQueryHandler
{
    Task<Result<AdminSemesterPublicationListDto>> HandleAsync(
        GetAdminSemesterPublicationsQuery query,
        CancellationToken ct = default);
}
