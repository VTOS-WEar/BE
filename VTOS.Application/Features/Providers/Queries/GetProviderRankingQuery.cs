using Microsoft.EntityFrameworkCore;
using VTOS.Application.Abstractions;
using VTOS.Application.Features.Providers.DTOs;
using VTOS.Domain.Enums;

namespace VTOS.Application.Features.Providers.Queries;

public record GetProviderRankingQuery(Guid SchoolId);

public interface IGetProviderRankingQueryHandler
{
    Task<ProviderRankingResponse> HandleAsync(GetProviderRankingQuery query, CancellationToken cancellationToken = default);
}

public class GetProviderRankingQueryHandler : IGetProviderRankingQueryHandler
{
    private readonly IApplicationDbContext _context;

    public GetProviderRankingQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<ProviderRankingResponse> HandleAsync(GetProviderRankingQuery query, CancellationToken cancellationToken = default)
    {
        var providerIds = await _context.SemesterPublicationProviders
            .AsNoTracking()
            .Where(x =>
                x.SemesterPublication.SchoolID == query.SchoolId &&
                x.Status == SemPublicationProviderStatus.Active &&
                !x.Provider.IsDeleted)
            .Select(x => x.ProviderID)
            .Distinct()
            .ToListAsync(cancellationToken);

        var items = await _context.Providers
            .AsNoTracking()
            .Where(x => providerIds.Contains(x.Id))
            .OrderByDescending(x => x.AverageRating)
            .ThenByDescending(x => x.TotalRatings)
            .ThenByDescending(x => x.TotalCompletedOrders)
            .ThenBy(x => x.ProviderName)
            .Select(x => new ProviderRankingItemDto(
                x.Id,
                x.ProviderName,
                x.AverageRating,
                x.TotalRatings,
                x.TotalCompletedOrders))
            .ToListAsync(cancellationToken);

        return new ProviderRankingResponse
        {
            SchoolId = query.SchoolId,
            Items = items
        };
    }
}
