using Microsoft.EntityFrameworkCore;
using VTOS.Application.Abstractions;
using VTOS.Application.Features.Public.DTOs;
using VTOS.Domain.Entities;
using VTOS.Domain.Enums;

namespace VTOS.Application.Features.Public.Queries;

public record GetSchoolSemesterCatalogQuery(Guid SchoolId);
public record GetAllSchoolSemesterCatalogsQuery(Guid SchoolId);
public record GetProvidersForPublicationOutfitQuery(Guid SemesterPublicationId, Guid OutfitId);
public record GetProviderPublicProfileQuery(Guid ProviderId);
public record GetProviderRatingsQuery(Guid ProviderId);
public record GetProviderRankingQuery(Guid SchoolId);

public class GetSchoolSemesterCatalogQueryHandler
{
    private readonly IApplicationDbContext _context;

    public GetSchoolSemesterCatalogQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<SchoolSemesterCatalogResponse?> HandleAsync(GetSchoolSemesterCatalogQuery query, CancellationToken ct = default)
    {
        var publication = await _context.SemesterPublications
            .AsNoTracking()
            .Where(sp => sp.SchoolID == query.SchoolId && sp.Status == SemesterPublicationStatus.Active)
            .OrderByDescending(sp => sp.StartDate)
            .FirstOrDefaultAsync(ct);

        if (publication == null)
        {
            return null;
        }

        var now = DateTime.UtcNow;
        var usableContractStatuses = new[] { "Active", "InUse" };

        var publicationOutfits = await _context.SemesterPublicationOutfits
            .AsNoTracking()
            .Where(spo => spo.SemesterPublicationID == publication.Id)
            .Select(spo => new
            {
                spo.OutfitID,
                Outfit = spo.Outfit
            })
            .ToListAsync(ct);

        var approvedProviders = await _context.SemesterPublicationProviders
            .AsNoTracking()
            .Where(spp =>
                spp.SemesterPublicationID == publication.Id
                && spp.Status == SemPublicationProviderStatus.Active
                && spp.ContractID.HasValue
                && spp.Contract != null
                && usableContractStatuses.Contains(spp.Contract.Status)
                && spp.Contract.ExpiresAt > now)
            .Select(spp => new
            {
                spp.Id,
                spp.ProviderID,
                spp.ContractID,
                Provider = spp.Provider
            })
            .ToListAsync(ct);

        var outfitIds = publicationOutfits.Select(x => x.OutfitID).Distinct().ToList();
        var publishedCatalogItems = await SemesterCatalogQueryHelpers.GetVisibleCatalogItemsAsync(_context, publication.Id, outfitIds, ct);
        var providerStats = await SemesterCatalogQueryHelpers.GetProviderStatsAsync(
            _context,
            approvedProviders.Select(p => p.ProviderID).Distinct().ToList(),
            publication.SchoolID,
            ct);

        var variants = await _context.ProductVariants
            .AsNoTracking()
            .Where(v => outfitIds.Contains(v.OutfitID) && v.ProviderCatalogItemID == null && !v.IsDeleted)
            .ToListAsync(ct);

        var isAfterDeadline = SemesterCatalogQueryHelpers.IsAfterDeadline(publication.EndDate);

        var outfits = publicationOutfits
            .Select(x =>
            {
                var sizes = variants
                    .Where(v => v.OutfitID == x.OutfitID)
                    .Select(v => v.Size)
                    .Distinct()
                    .OrderBy(s => s)
                    .ToList();

                var providers = approvedProviders
                    .Select(p =>
                    {
                        var catalogItem = publishedCatalogItems
                            .FirstOrDefault(ci => ci.SemesterPublicationProviderID == p.Id && ci.OutfitID == x.OutfitID);

                        if (catalogItem == null)
                        {
                            return null;
                        }

                        var stats = providerStats.GetValueOrDefault(p.ProviderID) ?? ProviderMarketplaceStats.Empty(p.ProviderID);
                        var publicationPrice = catalogItem.PublicationPrice;
                        var postDeadlinePrice = catalogItem.PostDeadlinePrice;

                        return new SemesterCatalogProviderDto
                        {
                            ProviderId = p.ProviderID,
                            ProviderName = p.Provider.ProviderName,
                            ContactEmail = p.Provider.Email,
                            DisplayName = x.Outfit.OutfitName,
                            ShortDescription = catalogItem?.ShortDescription ?? x.Outfit.Description,
                            MaterialDetails = catalogItem?.MaterialDetails,
                            MainImageUrl = catalogItem?.MainImageUrl ?? x.Outfit.MainImageURL,
                            Price = isAfterDeadline ? postDeadlinePrice : publicationPrice,
                            PublicationPrice = publicationPrice,
                            PostDeadlinePrice = postDeadlinePrice,
                            PricingMode = SemesterCatalogQueryHelpers.ResolvePricingModeName(isAfterDeadline),
                            AverageRating = stats.AverageRating,
                            TotalRatings = stats.TotalRatings,
                            TotalCompletedOrders = stats.TotalCompletedOrders
                        };
                    })
                    .Where(p => p != null)
                    .Select(p => p!)
                    .OrderBy(p => p.Price)
                    .ThenBy(p => p.ProviderName)
                    .ToList();

                return new SemesterCatalogOutfitDto
                {
                    OutfitId = x.OutfitID,
                    OutfitName = x.Outfit.OutfitName,
                    Description = providers.FirstOrDefault()?.ShortDescription ?? x.Outfit.Description,
                    MainImageUrl = providers.FirstOrDefault()?.MainImageUrl ?? x.Outfit.MainImageURL,
                    Price = providers.FirstOrDefault()?.Price ?? x.Outfit.Price,
                    LowestPublicationPrice = providers.Count > 0 ? providers.Min(p => p.PublicationPrice) : null,
                    LowestPostDeadlinePrice = providers.Count > 0 ? providers.Min(p => p.PostDeadlinePrice) : null,
                    OutfitType = x.Outfit.OutfitType.ToString(),
                    Sizes = sizes,
                    Providers = providers
                };
            })
            .OrderBy(x => x.OutfitName)
            .ToList();

        return new SchoolSemesterCatalogResponse
        {
            SemesterPublicationId = publication.Id,
            SchoolId = publication.SchoolID,
            Semester = publication.Semester,
            AcademicYear = publication.AcademicYear,
            StartDate = publication.StartDate,
            EndDate = publication.EndDate,
            IsAfterDeadline = isAfterDeadline,
            Status = publication.Status.ToString(),
            Outfits = outfits
        };
    }
}

public class GetAllSchoolSemesterCatalogsQueryHandler
{
    private readonly IApplicationDbContext _context;

    public GetAllSchoolSemesterCatalogsQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<SchoolSemesterCatalogResponse>> HandleAsync(GetAllSchoolSemesterCatalogsQuery query, CancellationToken ct = default)
    {
        var now = DateTime.UtcNow;
        var usableContractStatuses = new[] { "Active", "InUse" };

        var publications = await _context.SemesterPublications
            .AsNoTracking()
            .Where(sp => sp.SchoolID == query.SchoolId && sp.Status == SemesterPublicationStatus.Active)
            .OrderByDescending(sp => sp.StartDate)
            .ToListAsync(ct);

        if (!publications.Any())
        {
            return new List<SchoolSemesterCatalogResponse>();
        }

        var results = new List<SchoolSemesterCatalogResponse>();

        foreach (var publication in publications)
        {
            var publicationOutfits = await _context.SemesterPublicationOutfits
                .AsNoTracking()
                .Where(spo => spo.SemesterPublicationID == publication.Id)
                .Select(spo => new
                {
                    spo.OutfitID,
                    Outfit = spo.Outfit
                })
                .ToListAsync(ct);

            var approvedProviders = await _context.SemesterPublicationProviders
                .AsNoTracking()
                .Where(spp =>
                    spp.SemesterPublicationID == publication.Id
                    && spp.Status == SemPublicationProviderStatus.Active
                    && spp.ContractID.HasValue
                    && spp.Contract != null
                    && usableContractStatuses.Contains(spp.Contract.Status)
                    && spp.Contract.ExpiresAt > now)
                .Select(spp => new
                {
                    spp.Id,
                    spp.ProviderID,
                    spp.ContractID,
                    Provider = spp.Provider
                })
                .ToListAsync(ct);

            var outfitIds = publicationOutfits.Select(x => x.OutfitID).Distinct().ToList();
            var publishedCatalogItems = await SemesterCatalogQueryHelpers.GetVisibleCatalogItemsAsync(_context, publication.Id, outfitIds, ct);
            var providerStats = await SemesterCatalogQueryHelpers.GetProviderStatsAsync(
                _context,
                approvedProviders.Select(p => p.ProviderID).Distinct().ToList(),
                publication.SchoolID,
                ct);

            var variants = await _context.ProductVariants
                .AsNoTracking()
                .Where(v => outfitIds.Contains(v.OutfitID) && v.ProviderCatalogItemID == null && !v.IsDeleted)
                .ToListAsync(ct);

            var isAfterDeadline = SemesterCatalogQueryHelpers.IsAfterDeadline(publication.EndDate);

            var outfits = publicationOutfits
                .Select(x =>
                {
                    var sizes = variants
                        .Where(v => v.OutfitID == x.OutfitID)
                        .Select(v => v.Size)
                        .Distinct()
                        .OrderBy(s => s)
                        .ToList();

                    var providers = approvedProviders
                        .Select(p =>
                        {
                            var catalogItem = publishedCatalogItems
                                .FirstOrDefault(ci => ci.SemesterPublicationProviderID == p.Id && ci.OutfitID == x.OutfitID);

                            if (catalogItem == null)
                            {
                                return null;
                            }

                            var stats = providerStats.GetValueOrDefault(p.ProviderID) ?? ProviderMarketplaceStats.Empty(p.ProviderID);
                            var publicationPrice = catalogItem.PublicationPrice;
                            var postDeadlinePrice = catalogItem.PostDeadlinePrice;

                            return new SemesterCatalogProviderDto
                            {
                                ProviderId = p.ProviderID,
                                ProviderName = p.Provider.ProviderName,
                                ContactEmail = p.Provider.Email,
                                DisplayName = x.Outfit.OutfitName,
                                ShortDescription = catalogItem?.ShortDescription ?? x.Outfit.Description,
                                MaterialDetails = catalogItem?.MaterialDetails,
                                MainImageUrl = catalogItem?.MainImageUrl ?? x.Outfit.MainImageURL,
                                Price = isAfterDeadline ? postDeadlinePrice : publicationPrice,
                                PublicationPrice = publicationPrice,
                                PostDeadlinePrice = postDeadlinePrice,
                                PricingMode = SemesterCatalogQueryHelpers.ResolvePricingModeName(isAfterDeadline),
                                AverageRating = stats.AverageRating,
                                TotalRatings = stats.TotalRatings,
                                TotalCompletedOrders = stats.TotalCompletedOrders
                            };
                        })
                        .Where(p => p != null)
                        .Select(p => p!)
                        .OrderBy(p => p.Price)
                        .ThenBy(p => p.ProviderName)
                        .ToList();

                    return new SemesterCatalogOutfitDto
                    {
                        OutfitId = x.OutfitID,
                        OutfitName = x.Outfit.OutfitName,
                        Description = providers.FirstOrDefault()?.ShortDescription ?? x.Outfit.Description,
                        MainImageUrl = providers.FirstOrDefault()?.MainImageUrl ?? x.Outfit.MainImageURL,
                        Price = providers.FirstOrDefault()?.Price ?? x.Outfit.Price,
                        LowestPublicationPrice = providers.Count > 0 ? providers.Min(p => p.PublicationPrice) : null,
                        LowestPostDeadlinePrice = providers.Count > 0 ? providers.Min(p => p.PostDeadlinePrice) : null,
                        OutfitType = x.Outfit.OutfitType.ToString(),
                        Sizes = sizes,
                        Providers = providers
                    };
                })
                .OrderBy(x => x.OutfitName)
                .ToList();

            results.Add(new SchoolSemesterCatalogResponse
            {
                SemesterPublicationId = publication.Id,
                SchoolId = publication.SchoolID,
                Semester = publication.Semester,
                AcademicYear = publication.AcademicYear,
                StartDate = publication.StartDate,
                EndDate = publication.EndDate,
                IsAfterDeadline = isAfterDeadline,
                Status = publication.Status.ToString(),
                Outfits = outfits
            });
        }

        return results;
    }
}

public class GetProvidersForPublicationOutfitQueryHandler
{
    private readonly IApplicationDbContext _context;

    public GetProvidersForPublicationOutfitQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<SemesterCatalogProviderDto>?> HandleAsync(GetProvidersForPublicationOutfitQuery query, CancellationToken ct = default)
    {
        var now = DateTime.UtcNow;
        var usableContractStatuses = new[] { "Active", "InUse" };

        var publication = await _context.SemesterPublications
            .AsNoTracking()
            .FirstOrDefaultAsync(sp => sp.Id == query.SemesterPublicationId && sp.Status == SemesterPublicationStatus.Active, ct);

        if (publication == null)
        {
            return null;
        }

        var outfit = await _context.Outfits
            .AsNoTracking()
            .FirstOrDefaultAsync(o => o.Id == query.OutfitId && !o.IsDeleted && o.IsAvailable, ct);

        if (outfit == null)
        {
            return null;
        }

        var providers = await _context.SemesterPublicationProviders
            .AsNoTracking()
            .Where(spp =>
                spp.SemesterPublicationID == query.SemesterPublicationId
                && spp.Status == SemPublicationProviderStatus.Active
                && spp.ContractID.HasValue
                && spp.Contract != null
                && usableContractStatuses.Contains(spp.Contract.Status)
                && spp.Contract.ExpiresAt > now)
            .Select(spp => new
            {
                spp.Id,
                spp.ProviderID,
                spp.ContractID,
                Provider = spp.Provider
            })
            .ToListAsync(ct);

        var publishedCatalogItems = await SemesterCatalogQueryHelpers.GetVisibleCatalogItemsAsync(_context, query.SemesterPublicationId, new List<Guid> { query.OutfitId }, ct);
        var providerStats = await SemesterCatalogQueryHelpers.GetProviderStatsAsync(
            _context,
            providers.Select(p => p.ProviderID).Distinct().ToList(),
            publication.SchoolID,
            ct);
        var catalogItemIds = publishedCatalogItems.Select(item => item.Id).Distinct().ToList();
        var providerVariants = await _context.ProductVariants
            .AsNoTracking()
            .Where(variant =>
                variant.OutfitID == query.OutfitId &&
                variant.ProviderCatalogItemID.HasValue &&
                catalogItemIds.Contains(variant.ProviderCatalogItemID.Value) &&
                !variant.IsDeleted)
            .ToListAsync(ct);
        var baseVariants = await _context.ProductVariants
            .AsNoTracking()
            .Where(variant => variant.OutfitID == query.OutfitId && variant.ProviderCatalogItemID == null && !variant.IsDeleted)
            .OrderBy(variant => variant.Size)
            .ToListAsync(ct);
        var isAfterDeadline = SemesterCatalogQueryHelpers.IsAfterDeadline(publication.EndDate);

        return providers
            .Select(p =>
            {
                var catalogItem = publishedCatalogItems
                    .FirstOrDefault(ci => ci.SemesterPublicationProviderID == p.Id && ci.OutfitID == query.OutfitId);

                if (catalogItem == null)
                {
                    return null;
                }

                var stats = providerStats.GetValueOrDefault(p.ProviderID) ?? ProviderMarketplaceStats.Empty(p.ProviderID);
                var publicationPrice = catalogItem.PublicationPrice;
                var postDeadlinePrice = catalogItem.PostDeadlinePrice;
                var visibleVariants = providerVariants.Any(variant => variant.ProviderCatalogItemID == catalogItem.Id)
                    ? providerVariants
                        .Where(variant => variant.ProviderCatalogItemID == catalogItem.Id)
                        .OrderBy(variant => variant.Size)
                        .ToList()
                    : baseVariants;

                return new SemesterCatalogProviderDto
                {
                    ProviderId = p.ProviderID,
                    ProviderName = p.Provider.ProviderName,
                    ContactEmail = p.Provider.Email,
                    DisplayName = outfit.OutfitName,
                    ShortDescription = catalogItem?.ShortDescription ?? outfit.Description,
                    MaterialDetails = catalogItem?.MaterialDetails,
                    MainImageUrl = catalogItem?.MainImageUrl ?? outfit.MainImageURL,
                    Price = isAfterDeadline ? postDeadlinePrice : publicationPrice,
                    PublicationPrice = publicationPrice,
                    PostDeadlinePrice = postDeadlinePrice,
                    PricingMode = SemesterCatalogQueryHelpers.ResolvePricingModeName(isAfterDeadline),
                    AverageRating = stats.AverageRating,
                    TotalRatings = stats.TotalRatings,
                    TotalCompletedOrders = stats.TotalCompletedOrders,
                    Variants = visibleVariants.Select(SemesterCatalogQueryHelpers.ToPublicVariantDto).ToList()
                };
            })
            .Where(x => x != null)
            .Select(x => x!)
            .OrderBy(x => x.Price)
            .ThenBy(x => x.ProviderName)
            .ToList();
    }
}

public class GetProviderPublicProfileQueryHandler
{
    private readonly IApplicationDbContext _context;

    public GetProviderPublicProfileQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<PublicProviderProfileDto?> HandleAsync(GetProviderPublicProfileQuery query, CancellationToken ct = default)
    {
        var provider = await _context.Providers
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == query.ProviderId && !p.IsDeleted, ct);

        if (provider == null)
            return null;

        var stats = (await SemesterCatalogQueryHelpers.GetProviderStatsAsync(_context, new[] { provider.Id }, null, ct))
            .GetValueOrDefault(provider.Id) ?? ProviderMarketplaceStats.Empty(provider.Id);

        return new PublicProviderProfileDto
        {
            ProviderId = provider.Id,
            ProviderName = provider.ProviderName,
            ContactPersonName = provider.ContactPersonName,
            Phone = provider.Phone,
            Email = provider.Email,
            Address = provider.Address,
            AverageRating = stats.AverageRating,
            TotalRatings = stats.TotalRatings,
            TotalCompletedOrders = stats.TotalCompletedOrders
        };
    }
}

public class GetProviderRatingsQueryHandler
{
    private readonly IApplicationDbContext _context;

    public GetProviderRatingsQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<ProviderRatingsResponse?> HandleAsync(GetProviderRatingsQuery query, CancellationToken ct = default)
    {
        var provider = await _context.Providers
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == query.ProviderId && !p.IsDeleted, ct);

        if (provider == null)
            return null;

        var snapshots = await SemesterCatalogQueryHelpers.GetLatestProviderRatingSnapshotsAsync(_context, new[] { provider.Id }, null, ct);
        var providerSnapshots = snapshots
            .Where(x => x.ProviderId == provider.Id)
            .OrderByDescending(x => x.CreatedAt)
            .ThenByDescending(x => x.ProviderRatingId)
            .ToList();
        var stats = (await SemesterCatalogQueryHelpers.GetProviderStatsAsync(_context, new[] { provider.Id }, null, ct))
            .GetValueOrDefault(provider.Id) ?? ProviderMarketplaceStats.Empty(provider.Id);

        return new ProviderRatingsResponse
        {
            ProviderId = provider.Id,
            ProviderName = provider.ProviderName,
            AverageRating = stats.AverageRating,
            TotalRatings = stats.TotalRatings,
            TotalCompletedOrders = stats.TotalCompletedOrders,
            Items = providerSnapshots
                .Select(x => new ProviderRatingItemDto
                {
                    ProviderRatingId = x.ProviderRatingId,
                    OrderId = x.OrderId,
                    Rating = x.Rating,
                    Comment = x.Comment,
                    CreatedAt = x.CreatedAt,
                    ParentName = x.ParentName
                })
                .ToList()
        };
    }
}

public class GetProviderRankingQueryHandler
{
    private readonly IApplicationDbContext _context;

    public GetProviderRankingQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<ProviderRankingResponse?> HandleAsync(GetProviderRankingQuery query, CancellationToken ct = default)
    {
        var schoolExists = await _context.Schools
            .AsNoTracking()
            .AnyAsync(s => s.Id == query.SchoolId, ct);

        if (!schoolExists)
            return null;

        var providers = await _context.SemesterPublicationProviders
            .AsNoTracking()
            .Where(spp =>
                spp.SemesterPublication.SchoolID == query.SchoolId
                && spp.SemesterPublication.Status != SemesterPublicationStatus.Draft
                && spp.Status == SemPublicationProviderStatus.Active
                && !spp.Provider.IsDeleted)
            .Select(spp => new
            {
                spp.ProviderID,
                spp.Provider.ProviderName
            })
            .Distinct()
            .ToListAsync(ct);

        var providerIds = providers.Select(x => x.ProviderID).Distinct().ToList();
        var stats = await SemesterCatalogQueryHelpers.GetProviderStatsAsync(_context, providerIds, query.SchoolId, ct);

        return new ProviderRankingResponse
        {
            SchoolId = query.SchoolId,
            Items = providers
                .Select(provider =>
                {
                    var providerStats = stats.GetValueOrDefault(provider.ProviderID) ?? ProviderMarketplaceStats.Empty(provider.ProviderID);
                    return new ProviderRankingItemDto
                    {
                        ProviderId = provider.ProviderID,
                        ProviderName = provider.ProviderName,
                        AverageRating = providerStats.AverageRating,
                        TotalRatings = providerStats.TotalRatings,
                        TotalCompletedOrders = providerStats.TotalCompletedOrders
                    };
                })
                .OrderByDescending(x => x.AverageRating)
                .ThenByDescending(x => x.TotalRatings)
                .ThenByDescending(x => x.TotalCompletedOrders)
                .ThenBy(x => x.ProviderName)
                .ToList()
        };
    }
}

internal static class SemesterCatalogQueryHelpers
{
    internal static Task<List<ProviderCatalogItem>> GetVisibleCatalogItemsAsync(
        IApplicationDbContext context,
        Guid semesterPublicationId,
        List<Guid> outfitIds,
        CancellationToken ct)
    {
        return context.ProviderCatalogItems
            .AsNoTracking()
            .Where(ci =>
                outfitIds.Contains(ci.OutfitID)
                && (ci.Status == ProviderCatalogItemStatus.Published || ci.Status == ProviderCatalogItemStatus.Ready)
                && ci.SemesterPublicationProvider.SemesterPublicationID == semesterPublicationId)
            .ToListAsync(ct);
    }

    internal static async Task<Dictionary<Guid, ProviderMarketplaceStats>> GetProviderStatsAsync(
        IApplicationDbContext context,
        IReadOnlyCollection<Guid> providerIds,
        Guid? schoolId,
        CancellationToken ct)
    {
        if (providerIds.Count == 0)
            return new Dictionary<Guid, ProviderMarketplaceStats>();

        var snapshots = await GetLatestProviderRatingSnapshotsAsync(context, providerIds, schoolId, ct);
        var ratings = snapshots
            .GroupBy(x => x.ProviderId)
            .ToDictionary(
                g => g.Key,
                g => new
                {
                    AverageRating = (decimal)Math.Round(g.Average(x => x.Rating), 1),
                    TotalRatings = g.Count()
                });

        var completedOrders = await context.Orders
            .AsNoTracking()
            .Where(o =>
                o.ProviderID != null
                && providerIds.Contains(o.ProviderID.Value)
                && o.OrderStatus == OrderStatus.Delivered
                && (!schoolId.HasValue || (o.SemesterPublicationID != null && o.SemesterPublication!.SchoolID == schoolId.Value)))
            .GroupBy(o => o.ProviderID!.Value)
            .Select(g => new
            {
                ProviderId = g.Key,
                Count = g.Count()
            })
            .ToListAsync(ct);

        return providerIds
            .Distinct()
            .ToDictionary(
                providerId => providerId,
                providerId =>
                {
                    var rating = ratings.GetValueOrDefault(providerId);
                    var completed = completedOrders.FirstOrDefault(x => x.ProviderId == providerId)?.Count ?? 0;
                    return new ProviderMarketplaceStats(
                        providerId,
                        rating?.AverageRating ?? 0m,
                        rating?.TotalRatings ?? 0,
                        completed);
                });
    }

    internal static async Task<List<ProviderRatingSnapshot>> GetLatestProviderRatingSnapshotsAsync(
        IApplicationDbContext context,
        IReadOnlyCollection<Guid> providerIds,
        Guid? schoolId,
        CancellationToken ct)
    {
        if (providerIds.Count == 0)
            return new List<ProviderRatingSnapshot>();

        var rawSnapshots = await context.Feedbacks
            .AsNoTracking()
            .Where(f =>
                f.OrderItem.Order.ProviderID != null
                && providerIds.Contains(f.OrderItem.Order.ProviderID.Value)
                && f.OrderItem.Order.SemesterPublicationID != null
                && (!schoolId.HasValue || f.OrderItem.Order.SemesterPublication!.SchoolID == schoolId.Value))
            .Select(f => new ProviderRatingSnapshot(
                f.Id,
                f.OrderItem.Order.ProviderID!.Value,
                f.OrderItem.OrderID,
                f.Rating,
                f.Comment,
                f.Timestamp,
                f.User.FullName))
            .ToListAsync(ct);

        return rawSnapshots
            .GroupBy(x => new { x.ProviderId, x.OrderId })
            .Select(g => g
                .OrderByDescending(x => x.CreatedAt)
                .ThenByDescending(x => x.ProviderRatingId)
                .First())
            .ToList();
    }

    internal static bool IsAfterDeadline(DateTime endDate)
    {
        return DateTime.UtcNow > endDate;
    }

    internal static decimal ResolveCurrentPrice(ProviderCatalogItem catalogItem, bool isAfterDeadline)
    {
        return isAfterDeadline ? catalogItem.PostDeadlinePrice : catalogItem.PublicationPrice;
    }

    internal static string ResolvePricingModeName(bool isAfterDeadline)
    {
        return isAfterDeadline
            ? OrderPricingMode.PostDeadlineDirect.ToString()
            : OrderPricingMode.PublicationWindow.ToString();
    }

    internal static ProductVariantDto ToPublicVariantDto(ProductVariant variant)
    {
        return new ProductVariantDto(
            variant.Id,
            variant.Size,
            variant.ColorVariant,
            variant.MaterialType,
            variant.StockQuantity,
            variant.Price,
            variant.SKUCode,
            variant.VariantImageURL
        );
    }
}

internal sealed record ProviderMarketplaceStats(Guid ProviderId, decimal AverageRating, int TotalRatings, int TotalCompletedOrders)
{
    internal static ProviderMarketplaceStats Empty(Guid providerId) => new(providerId, 0m, 0, 0);
}

internal sealed record ProviderRatingSnapshot(
    Guid ProviderRatingId,
    Guid ProviderId,
    Guid OrderId,
    int Rating,
    string? Comment,
    DateTime CreatedAt,
    string ParentName);
