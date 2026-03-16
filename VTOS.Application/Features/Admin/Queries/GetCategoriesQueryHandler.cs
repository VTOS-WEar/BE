using Microsoft.EntityFrameworkCore;
using VTOS.Application.Abstractions;
using VTOS.Application.Features.Admin.DTOs;

namespace VTOS.Application.Features.Admin.Queries;

public class GetCategoriesQueryHandler : IGetCategoriesQueryHandler
{
    private readonly IApplicationDbContext _context;

    public GetCategoriesQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<CategoryDto>> HandleAsync(
        GetCategoriesQuery query,
        CancellationToken cancellationToken)
    {
        var categories = await _context.Categories
            .OrderBy(c => c.CategoryName)
            .Select(c => new CategoryDto(
                c.Id,
                c.CategoryName,
                c.CreatedAt
            ))
            .ToListAsync(cancellationToken);

        return categories;
    }
}
