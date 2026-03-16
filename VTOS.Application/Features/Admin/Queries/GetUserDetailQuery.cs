using VTOS.Application.Features.Admin.DTOs;

namespace VTOS.Application.Features.Admin.Queries;

public record GetUserDetailQuery(Guid UserId);

public interface IGetUserDetailQueryHandler
{
    Task<UserDetailDto?> HandleAsync(
        GetUserDetailQuery query,
        CancellationToken cancellationToken);
}
