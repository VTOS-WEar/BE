using Microsoft.EntityFrameworkCore;
using VTOS.Application.Abstractions;
using VTOS.Application.Features.Providers.DTOs;

namespace VTOS.Application.Features.Providers.Queries;

public record GetProviderRatingsQuery(Guid ProviderId);

public interface IGetProviderRatingsQueryHandler
{
    Task<ProviderRatingsResponse?> HandleAsync(GetProviderRatingsQuery query, CancellationToken cancellationToken = default);
}

public class GetProviderRatingsQueryHandler : IGetProviderRatingsQueryHandler
{
    private readonly IApplicationDbContext _context;

    public GetProviderRatingsQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<ProviderRatingsResponse?> HandleAsync(GetProviderRatingsQuery query, CancellationToken cancellationToken = default)
    {
        var provider = await _context.Providers
            .AsNoTracking()
            .Where(x => x.Id == query.ProviderId && !x.IsDeleted)
            .Select(x => new ProviderRatingsResponse
            {
                ProviderId = x.Id,
                ProviderName = x.ProviderName,
                AverageRating = x.AverageRating,
                TotalRatings = x.TotalRatings,
                TotalCompletedOrders = x.TotalCompletedOrders
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (provider == null)
        {
            return null;
        }

        provider.Items = await _context.ProviderRatings
            .AsNoTracking()
            .Include(x => x.ParentUser)
            .Where(x => x.ProviderID == query.ProviderId)
            .OrderByDescending(x => x.CreatedAt)
            .Select(x => new ProviderRatingItemDto(
                x.Id,
                x.OrderID,
                x.Rating,
                x.Comment,
                x.CreatedAt,
                x.ParentUser.FullName))
            .ToListAsync(cancellationToken);

        return provider;
    }
}
