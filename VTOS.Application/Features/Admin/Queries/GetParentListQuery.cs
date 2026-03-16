using VTOS.Application.Features.Admin.DTOs;

namespace VTOS.Application.Features.Admin.Queries;

public record GetParentListQuery(
    int Page = 1,
    int PageSize = 10,
    string? Search = null,
    string? Status = null
);

public interface IGetParentListQueryHandler
{
    Task<PaginatedResult<ParentListItemDto>> HandleAsync(
        GetParentListQuery query,
        CancellationToken cancellationToken);
}

public record PaginatedResult<T>(
    List<T> Items,
    int TotalCount,
    int Page,
    int PageSize
);
