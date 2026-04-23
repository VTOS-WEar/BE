using VTOS.Application.Common;
using VTOS.Application.Features.Users.DTOs;

namespace VTOS.Application.Features.Users.Queries;

public record GetParentAddressesQuery(Guid ParentUserId);

public interface IGetParentAddressesQueryHandler
{
    Task<Result<IReadOnlyList<ParentAddressResponse>>> HandleAsync(
        GetParentAddressesQuery query,
        CancellationToken cancellationToken = default);
}
