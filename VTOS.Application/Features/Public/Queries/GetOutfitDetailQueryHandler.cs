using Microsoft.EntityFrameworkCore;
using VTOS.Application.Abstractions;
using VTOS.Application.Features.Public.DTOs;
using VTOS.Domain.Entities;
using VTOS.Domain.Enums;

namespace VTOS.Application.Features.Public.Queries;

public class GetOutfitDetailQueryHandler
{
    private readonly IApplicationDbContext _context;

    public GetOutfitDetailQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<OutfitDetailResponse?> HandleAsync(GetOutfitDetailQuery query, CancellationToken ct = default)
    {
        var outfit = await _context.Outfits
            .AsNoTracking()
            .Include(o => o.School)
            .Include(o => o.ProductVariants.Where(pv => !pv.IsDeleted && pv.ProviderCatalogItemID == null))
            .Include(o => o.SizeChart)
                .ThenInclude(sc => sc!.SizeChartDetails)
                    .ThenInclude(detail => detail.Measurements)
            .Include(o => o.OutfitCategories)
                .ThenInclude(oc => oc.Category)
            .FirstOrDefaultAsync(o => o.Id == query.OutfitId, ct);

        if (outfit == null)
            return null;

        var feedbacks = await _context.Feedbacks
            .AsNoTracking()
            .Include(f => f.OrderItem)
                .ThenInclude(oi => oi.ProductVariant)
            .Include(f => f.User)
            .Where(f => f.OrderItem.ProductVariant.OutfitID == query.OutfitId)
            .OrderByDescending(f => f.Timestamp)
            .ToListAsync(ct);

        var averageRating = feedbacks.Any() ? (decimal)feedbacks.Average(f => f.Rating) : 0m;

        SizeChartDto? sizeChartDto = null;
        if (outfit.SizeChart != null)
        {
            var details = outfit.SizeChart.SizeChartDetails
                .Select(d => new SizeChartDetailDto(
                    d.SizeLabel,
                    d.Measurements
                        .OrderBy(m => m.DisplayName)
                        .ThenBy(m => m.FieldKey)
                        .Select(m => new SizeChartMeasurementDto(
                            m.FieldKey,
                            m.DisplayName,
                            m.Unit,
                            m.MinCm,
                            m.MaxCm
                        ))
                        .ToList()
                ))
                .ToList();

            sizeChartDto = new SizeChartDto(
                outfit.SizeChart.Id,
                outfit.SizeChart.ChartName,
                outfit.SizeChart.Unit,
                details
            );
        }

        var variants = outfit.ProductVariants
            .Select(pv => new ProductVariantDto(
                pv.Id,
                pv.Size,
                pv.ColorVariant,
                pv.MaterialType,
                pv.StockQuantity,
                pv.Price,
                pv.SKUCode,
                pv.VariantImageURL
            ))
            .ToList();

        var categories = outfit.OutfitCategories
            .Select(oc => oc.Category.CategoryName)
            .ToList();

        var now = DateTime.UtcNow;
        var usableContractStatuses = new[] { "Active", "InUse" };
        var matchingPublicationProviders = await _context.SemesterPublicationProviders
            .AsNoTracking()
            .Where(spp =>
                spp.Status == SemPublicationProviderStatus.Active
                && spp.SemesterPublication.Status == SemesterPublicationStatus.Active
                && spp.SemesterPublication.StartDate <= now
                && spp.ContractID.HasValue
                && spp.Contract != null
                && usableContractStatuses.Contains(spp.Contract.Status)
                && spp.Contract.ExpiresAt > now
                && spp.SemesterPublication.Outfits.Any(spo => spo.OutfitID == query.OutfitId))
            .Select(spp => new
            {
                spp.Id,
                spp.ContractID,
                PublicationId = spp.SemesterPublicationID,
                PublicationStatus = spp.SemesterPublication.Status.ToString(),
                spp.SemesterPublication.StartDate,
                spp.SemesterPublication.EndDate,
                spp.ProviderID,
                ProviderName = spp.Provider.ProviderName
            })
            .ToListAsync(ct);

        var catalogItems = await _context.ProviderCatalogItems
            .AsNoTracking()
            .Where(item =>
                item.OutfitID == query.OutfitId &&
                (item.Status == ProviderCatalogItemStatus.Published || item.Status == ProviderCatalogItemStatus.Ready) &&
                matchingPublicationProviders.Select(spp => spp.Id).Contains(item.SemesterPublicationProviderID))
            .ToListAsync(ct);

        var catalogItemIds = catalogItems.Select(item => item.Id).Distinct().ToList();
        var providerVariants = await _context.ProductVariants
            .AsNoTracking()
            .Where(variant =>
                variant.OutfitID == query.OutfitId &&
                variant.ProviderCatalogItemID.HasValue &&
                catalogItemIds.Contains(variant.ProviderCatalogItemID.Value) &&
                !variant.IsDeleted)
            .ToListAsync(ct);

        var campaignOptions = matchingPublicationProviders
            .Select(spp =>
            {
                var catalogItem = catalogItems.FirstOrDefault(item => item.SemesterPublicationProviderID == spp.Id);

                if (catalogItem == null)
                    return null;

                var publicationPrice = catalogItem.PublicationPrice;
                var postDeadlinePrice = catalogItem.PostDeadlinePrice;
                var visibleVariants = providerVariants.Any(variant => variant.ProviderCatalogItemID == catalogItem.Id)
                    ? providerVariants
                        .Where(variant => variant.ProviderCatalogItemID == catalogItem.Id)
                        .OrderBy(variant => variant.Size)
                        .Select(ToPublicVariantDto)
                        .ToList()
                    : variants;

                return new OutfitCampaignOptionDto(
                    spp.PublicationId,
                    outfit.OutfitName,
                    spp.PublicationStatus,
                    spp.StartDate,
                    spp.EndDate,
                    catalogItem.Id,
                    spp.EndDate >= now ? publicationPrice : postDeadlinePrice,
                    null,
                    visibleVariants
                );
            })
            .Where(x => x != null)
            .Select(x => x!)
            .OrderBy(x => x.EndDate)
            .ThenBy(x => x.CampaignName)
            .ToList();

        var reviews = feedbacks
            .Select(f => new ReviewDto(
                f.Id,
                f.Rating,
                f.Comment,
                f.Timestamp,
                f.User.FullName,
                f.User.Avatar
            ))
            .ToList();

        return new OutfitDetailResponse(
            outfit.Id,
            outfit.OutfitName,
            outfit.Description,
            outfit.Price,
            outfit.OutfitType.ToString(),
            outfit.MainImageURL,
            outfit.IsAvailable,
            outfit.IsCustomizable,
            new OutfitSchoolDto(outfit.School.Id, outfit.School.SchoolName, outfit.School.LogoURL),
            variants,
            sizeChartDto,
            campaignOptions,
            categories,
            Math.Round(averageRating, 1),
            feedbacks.Count,
            reviews
        );
    }

    private static ProductVariantDto ToPublicVariantDto(ProductVariant variant)
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
