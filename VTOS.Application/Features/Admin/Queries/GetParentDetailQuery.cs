using VTOS.Application.Features.Admin.DTOs;

namespace VTOS.Application.Features.Admin.Queries;

public record GetParentDetailQuery(Guid ParentId);

public interface IGetParentDetailQueryHandler
{
    Task<ParentDetailDto?> HandleAsync(
        GetParentDetailQuery query,
        CancellationToken cancellationToken);
}
