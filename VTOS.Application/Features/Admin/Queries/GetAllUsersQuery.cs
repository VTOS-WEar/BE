namespace VTOS.Application.Features.Admin.Queries;
using VTOS.Application.Features.Admin.DTOs;

public record GetAllUsersQuery(
    int Page = 1,
    int PageSize = 20,
    string? Search = null,
    string? Role = null,
    string? Status = null);

public record UserListPagedResult(
    List<UserListItemDto> Items,
    int TotalCount,
    int Page,
    int PageSize);

public interface IGetAllUsersQueryHandler
{
    Task<UserListPagedResult> HandleAsync(
        GetAllUsersQuery query,
        CancellationToken cancellationToken);
}
