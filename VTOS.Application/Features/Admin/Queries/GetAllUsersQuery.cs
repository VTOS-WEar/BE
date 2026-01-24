namespace VTOS.Application.Features.Admin.Queries;
using VTOS.Application.Features.Admin.DTOs;

public record GetAllUsersQuery();

public interface IGetAllUsersQueryHandler
{
    Task<List<UserListItemDto>> HandleAsync(
        GetAllUsersQuery query,
        CancellationToken cancellationToken);
}
