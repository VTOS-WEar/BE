using Microsoft.EntityFrameworkCore;
using VTOS.Application.Abstractions;
using VTOS.Application.Features.Admin.DTOs;

namespace VTOS.Application.Features.Admin.Queries;

public class GetTotalQuantityPerItemQueryHandler : IGetTotalQuantityPerItemQueryHandler
{
    private readonly IApplicationDbContext _context;

    public GetTotalQuantityPerItemQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<TotalQuantityPerItemDto> HandleAsync(
        GetTotalQuantityPerItemQuery query,
        CancellationToken cancellationToken)
    {
        var itemsQuery = _context.OrderItems
            .Include(oi => oi.ProductVariant)
                .ThenInclude(pv => pv.Outfit)
                    .ThenInclude(o => o.School)
            .AsQueryable();

        // Apply date filters
        if (query.DateFrom.HasValue)
            itemsQuery = itemsQuery.Where(oi => oi.Order.CreatedAt >= query.DateFrom.Value);

        if (query.DateTo.HasValue)
            itemsQuery = itemsQuery.Where(oi => oi.Order.CreatedAt <= query.DateTo.Value);

        var items = await itemsQuery
            .GroupBy(oi => new 
            { 
                OutfitId = oi.ProductVariant.Outfit.Id,
                OutfitName = oi.ProductVariant.Outfit.OutfitName,
                Size = oi.SizeOrdered,
                SchoolId = oi.ProductVariant.Outfit.School.Id,
                SchoolName = oi.ProductVariant.Outfit.School.SchoolName
            })
            .Select(g => new { g.Key.OutfitId, g.Key.OutfitName, g.Key.Size, QuantitySold = g.Sum(oi => oi.Quantity), g.Key.SchoolId, g.Key.SchoolName })
            .OrderByDescending(x => x.QuantitySold)
            .ToListAsync(cancellationToken);

        var itemsDto = items
            .Select(x => new ItemQuantitySoldDto(x.OutfitId, x.OutfitName, x.Size, x.QuantitySold, x.SchoolId, x.SchoolName))
            .ToList();

        return new TotalQuantityPerItemDto(itemsDto);
    }
}
