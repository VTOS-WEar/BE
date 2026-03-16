using VTOS.Application.Features.Admin.DTOs;

namespace VTOS.Application.Features.Admin.Queries;

public record GetCategoriesQuery;

public interface IGetCategoriesQueryHandler
{
    Task<List<CategoryDto>> HandleAsync(
        GetCategoriesQuery query,
        CancellationToken cancellationToken);
}
