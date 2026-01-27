using Microsoft.EntityFrameworkCore;
using VTOS.Application.Abstractions;
using VTOS.Application.Features.Public.DTOs;

namespace VTOS.Application.Features.Public.Queries;

public class GetCategoriesQueryHandler
{
    private readonly IApplicationDbContext _context;

    public GetCategoriesQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<CategoryListResponse> HandleAsync(GetCategoriesQuery query, CancellationToken ct = default)
    {
        var categories = await _context.Categories
            .AsNoTracking()
            .OrderBy(c => c.CategoryName)
            .Select(c => new CategoryDto(
                c.Id,
                c.CategoryName,
                c.OutfitCategories.Count(oc => oc.Outfit.IsAvailable)
            ))
            .ToListAsync(ct);

        return new CategoryListResponse(categories);
    }
}
