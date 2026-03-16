using VTOS.Application.Features.Admin.DTOs;

namespace VTOS.Application.Features.Admin.Queries;

public record GetTotalQuantityPerItemQuery(
    DateTime? DateFrom = null,
    DateTime? DateTo = null
);

public interface IGetTotalQuantityPerItemQueryHandler
{
    Task<TotalQuantityPerItemDto> HandleAsync(
        GetTotalQuantityPerItemQuery query,
        CancellationToken cancellationToken);
}
