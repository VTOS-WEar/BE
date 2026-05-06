using Microsoft.EntityFrameworkCore;
using VTOS.Application.Abstractions;
using VTOS.Application.Common;
using VTOS.Application.Features.Providers.DTOs;
using VTOS.Application.Features.Schools.DTOs;
using VTOS.Application.Features.Schools.Helpers;
using VTOS.Domain.Entities;
using VTOS.Domain.Enums;

namespace VTOS.Application.Features.Providers.Queries;

public record GetProviderCatalogQuery(
    Guid UserId,
    int Page = 1,
    int PageSize = 5,
    string? Status = null,
    string? Search = null,
    Guid? SchoolId = null,
    Guid? SemesterPublicationProviderId = null,
    string? AcademicYear = null);

public interface IGetProviderCatalogQueryHandler
{
    Task<Result<ProviderCatalogResponse>> HandleAsync(GetProviderCatalogQuery query, CancellationToken cancellationToken = default);
}

public class GetProviderCatalogQueryHandler : IGetProviderCatalogQueryHandler
{
    private readonly IApplicationDbContext _context;

    public GetProviderCatalogQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result<ProviderCatalogResponse>> HandleAsync(GetProviderCatalogQuery query, CancellationToken cancellationToken = default)
    {
        var providerId = await ResolveProviderIdAsync(query.UserId, cancellationToken);
        if (!providerId.HasValue)
            return Result<ProviderCatalogResponse>.Failure("Provider not found for current user.", "PROVIDER_NOT_FOUND");

        var now = DateTime.UtcNow;
        var usableContractStatuses = new[] { "Active", "InUse" };

        var publicationProviders = await _context.SemesterPublicationProviders
            .AsNoTracking()
            .Include(x => x.SemesterPublication)
                .ThenInclude(x => x.School)
            .Include(x => x.Contract)
            .Where(x => x.ProviderID == providerId.Value)
            .Where(x =>
                x.ContractID.HasValue
                && x.Contract != null
                && usableContractStatuses.Contains(x.Contract.Status)
                && x.Contract.ExpiresAt > now)
            .ToListAsync(cancellationToken);

        var publicationIds = publicationProviders.Select(x => x.SemesterPublicationID).Distinct().ToList();
        var contractIds = publicationProviders.Where(x => x.ContractID.HasValue).Select(x => x.ContractID!.Value).Distinct().ToList();
        var publicationProviderIds = publicationProviders.Select(x => x.Id).ToList();

        var publicationOutfits = await _context.SemesterPublicationOutfits
            .AsNoTracking()
            .Include(x => x.Outfit)
            .Where(x => publicationIds.Contains(x.SemesterPublicationID))
            .ToListAsync(cancellationToken);

        var contractItems = await _context.ContractItems
            .AsNoTracking()
            .Where(x => contractIds.Contains(x.ContractID))
            .ToListAsync(cancellationToken);

        var catalogItems = await _context.ProviderCatalogItems
            .AsNoTracking()
            .Where(x => x.ProviderID == providerId.Value && publicationProviderIds.Contains(x.SemesterPublicationProviderID))
            .ToListAsync(cancellationToken);

        var outfitIds = publicationOutfits.Select(x => x.OutfitID).Distinct().ToList();
        var catalogItemIds = catalogItems.Select(x => x.Id).Distinct().ToList();
        var variantRows = await _context.ProductVariants
            .AsNoTracking()
            .Where(x =>
                outfitIds.Contains(x.OutfitID) &&
                !x.IsDeleted &&
                (x.ProviderCatalogItemID == null || catalogItemIds.Contains(x.ProviderCatalogItemID.Value)))
            .ToListAsync(cancellationToken);

        var sizeChartIds = publicationOutfits
            .Where(x => x.Outfit.SizeChartID.HasValue)
            .Select(x => x.Outfit.SizeChartID!.Value)
            .Concat(catalogItems.Where(x => x.SizeChartID.HasValue).Select(x => x.SizeChartID!.Value))
            .Distinct()
            .ToList();
        var sizeDetails = await _context.SizeChartDetails
            .AsNoTracking()
            .Where(x => sizeChartIds.Contains(x.SizeChartID))
            .Include(x => x.Measurements)
            .ToListAsync(cancellationToken);

        var rows = publicationProviders.Select(publicationProvider =>
        {
            var outfits = publicationOutfits
                .Where(x => x.SemesterPublicationID == publicationProvider.SemesterPublicationID)
                .OrderBy(x => x.Outfit.OutfitName)
                .ToList();

            var items = outfits
                .Select(publicationOutfit =>
                {
                    var contractItem = publicationProvider.ContractID.HasValue
                        ? contractItems.FirstOrDefault(x =>
                            x.ContractID == publicationProvider.ContractID.Value &&
                            x.OutfitID == publicationOutfit.OutfitID)
                        : null;

                    if (contractItem == null)
                        return null;

                    var catalogItem = catalogItems.FirstOrDefault(x =>
                        x.SemesterPublicationProviderID == publicationProvider.Id &&
                        x.ContractItemID == contractItem.Id);

                    return new ProviderCatalogItemDto
                    {
                        CatalogItemId = catalogItem?.Id,
                        ContractItemId = contractItem.Id,
                        OutfitId = publicationOutfit.OutfitID,
                        OutfitName = publicationOutfit.Outfit.OutfitName,
                        OutfitImageUrl = publicationOutfit.Outfit.MainImageURL,
                        SchoolMaterialType = publicationOutfit.Outfit.MaterialType,
                        ContractPricePerUnit = contractItem.PricePerUnit,
                        DisplayName = publicationOutfit.Outfit.OutfitName,
                        ShortDescription = catalogItem?.ShortDescription ?? publicationOutfit.Outfit.Description,
                        MaterialDetails = catalogItem?.MaterialDetails,
                        PublicationPrice = catalogItem?.PublicationPrice,
                        PostDeadlinePrice = catalogItem?.PostDeadlinePrice,
                        Status = catalogItem?.Status.ToString() ?? ProviderCatalogItemStatus.Draft.ToString(),
                        Variants = ResolveVariants(publicationOutfit.Outfit, catalogItem, variantRows, sizeDetails),
                        SizeSource = HasProviderVariants(catalogItem, variantRows) ? "ProviderManaged" : "InheritedFromOutfit",
                        CanManageSizes = publicationProvider.Status != SemPublicationProviderStatus.Suspended
                    };
                })
                .Where(x => x != null)
                .Select(x => x!)
                .ToList();

            return new ProviderCatalogPublicationDto
            {
                SemesterPublicationProviderId = publicationProvider.Id,
                SemesterPublicationId = publicationProvider.SemesterPublicationID,
                SchoolId = publicationProvider.SemesterPublication.SchoolID,
                SchoolName = publicationProvider.SemesterPublication.School.SchoolName,
                Semester = publicationProvider.SemesterPublication.Semester,
                AcademicYear = publicationProvider.SemesterPublication.AcademicYear,
                StartDate = publicationProvider.SemesterPublication.StartDate,
                EndDate = publicationProvider.SemesterPublication.EndDate,
                PublicationStatus = publicationProvider.SemesterPublication.Status.ToString(),
                ProviderStatus = publicationProvider.Status.ToString(),
                ContractId = publicationProvider.ContractID,
                ContractName = publicationProvider.Contract?.ContractName,
                ContractNumber = publicationProvider.Contract?.ContractNumber,
                Items = items
            };
        }).ToList();

        var summary = new ProviderCatalogSummaryDto
        {
            Publications = rows.Count,
            Items = rows.Sum(x => x.Items.Count),
            Published = rows.Sum(x => x.Items.Count(item =>
                item.Status == ProviderCatalogItemStatus.Ready.ToString() ||
                item.Status == ProviderCatalogItemStatus.Published.ToString())),
            NeedsSetup = rows.Sum(x => x.Items.Count(item =>
                !item.CatalogItemId.HasValue ||
                item.Status == ProviderCatalogItemStatus.Draft.ToString()))
        };

        var schoolOptions = rows
            .GroupBy(x => new { x.SchoolId, x.SchoolName })
            .Select(group => new ProviderCatalogSchoolOptionDto
            {
                SchoolId = group.Key.SchoolId,
                SchoolName = group.Key.SchoolName,
                PublicationCount = group.Count(),
                ActiveCount = group.Count(x =>
                    x.ProviderStatus == SemPublicationProviderStatus.Active.ToString() &&
                    x.PublicationStatus != SemesterPublicationStatus.Draft.ToString()),
                NeedsSetupCount = group.Sum(x => x.Items.Count(item =>
                    !item.CatalogItemId.HasValue ||
                    item.Status == ProviderCatalogItemStatus.Draft.ToString()))
            })
            .OrderByDescending(x => x.ActiveCount)
            .ThenBy(x => x.SchoolName)
            .ToList();

        var filteredRows = rows.AsEnumerable();
        if (query.SchoolId.HasValue)
        {
            filteredRows = filteredRows.Where(x => x.SchoolId == query.SchoolId.Value);
        }

        if (query.SemesterPublicationProviderId.HasValue)
        {
            filteredRows = filteredRows.Where(x => x.SemesterPublicationProviderId == query.SemesterPublicationProviderId.Value);
        }

        if (!string.IsNullOrWhiteSpace(query.AcademicYear))
        {
            var academicYear = query.AcademicYear.Trim();
            filteredRows = filteredRows.Where(x => x.AcademicYear == academicYear);
        }

        var status = query.Status?.Trim().ToLowerInvariant();
        if (!string.IsNullOrWhiteSpace(status) && status != "all")
        {
            filteredRows = status switch
            {
                "active" => filteredRows.Where(x =>
                    x.ProviderStatus == SemPublicationProviderStatus.Active.ToString() &&
                    x.PublicationStatus != SemesterPublicationStatus.Draft.ToString()),
                "draft" => filteredRows.Where(x => x.PublicationStatus == SemesterPublicationStatus.Draft.ToString()),
                "suspended" => filteredRows.Where(x => x.ProviderStatus == SemPublicationProviderStatus.Suspended.ToString()),
                "needssetup" => filteredRows.Where(x => x.Items.Any(item =>
                    !item.CatalogItemId.HasValue ||
                    item.Status == ProviderCatalogItemStatus.Draft.ToString())),
                _ => filteredRows
            };
        }

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var search = query.Search.Trim().ToLowerInvariant();
            schoolOptions = schoolOptions
                .Where(x => x.SchoolName.ToLowerInvariant().Contains(search))
                .ToList();
            filteredRows = filteredRows.Where(x =>
                x.SchoolName.ToLowerInvariant().Contains(search) ||
                x.Semester.ToLowerInvariant().Contains(search) ||
                x.AcademicYear.ToLowerInvariant().Contains(search) ||
                (x.ContractName != null && x.ContractName.ToLowerInvariant().Contains(search)) ||
                (x.ContractNumber != null && x.ContractNumber.ToLowerInvariant().Contains(search)) ||
                x.Items.Any(item =>
                    item.OutfitName.ToLowerInvariant().Contains(search) ||
                    item.DisplayName.ToLowerInvariant().Contains(search) ||
                    (item.SchoolMaterialType != null && item.SchoolMaterialType.ToLowerInvariant().Contains(search)) ||
                    (item.MaterialDetails != null && item.MaterialDetails.ToLowerInvariant().Contains(search))));
        }

        var page = Math.Max(1, query.Page);
        var pageSize = Math.Clamp(query.PageSize, 1, 50);
        var orderedRows = filteredRows
            .OrderBy(x => x.ProviderStatus == SemPublicationProviderStatus.Suspended.ToString() ? 1 : 0)
            .ThenByDescending(x => x.StartDate)
            .ToList();
        var totalCount = orderedRows.Count;

        var response = new ProviderCatalogResponse
        {
            Publications = orderedRows
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList(),
            SchoolOptions = schoolOptions,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize,
            TotalPages = (int)Math.Ceiling((double)totalCount / pageSize),
            Summary = summary
        };

        return Result<ProviderCatalogResponse>.Success(response);
    }

    private async Task<Guid?> ResolveProviderIdAsync(Guid userId, CancellationToken cancellationToken)
    {
        return await _context.ProviderManagers
            .AsNoTracking()
            .Where(x => x.UserID == userId)
            .Select(x => (Guid?)x.ProviderID)
            .FirstOrDefaultAsync(cancellationToken);
    }

    private static bool HasProviderVariants(ProviderCatalogItem? catalogItem, IEnumerable<ProductVariant> variants)
    {
        return catalogItem != null && variants.Any(x => x.ProviderCatalogItemID == catalogItem.Id);
    }

    private static List<ProductVariantDto> ResolveVariants(
        Outfit outfit,
        ProviderCatalogItem? catalogItem,
        IReadOnlyCollection<ProductVariant> variants,
        IReadOnlyCollection<SizeChartDetail> sizeDetails)
    {
        var providerVariants = catalogItem == null
            ? new List<ProductVariant>()
            : variants
                .Where(x => x.ProviderCatalogItemID == catalogItem.Id)
                .OrderBy(x => x.Size)
                .ToList();

        if (providerVariants.Count > 0)
        {
            var providerDetails = catalogItem!.SizeChartID.HasValue
                ? sizeDetails.Where(x => x.SizeChartID == catalogItem.SizeChartID.Value).ToList()
                : new List<SizeChartDetail>();
            return MapVariants(providerVariants, providerDetails);
        }

        var baseVariants = variants
            .Where(x => x.OutfitID == outfit.Id && x.ProviderCatalogItemID == null)
            .OrderBy(x => x.Size)
            .ToList();
        var baseDetails = outfit.SizeChartID.HasValue
            ? sizeDetails.Where(x => x.SizeChartID == outfit.SizeChartID.Value).ToList()
            : new List<SizeChartDetail>();
        return MapVariants(baseVariants, baseDetails);
    }

    private static List<ProductVariantDto> MapVariants(IEnumerable<ProductVariant> variants, IReadOnlyCollection<SizeChartDetail> details)
    {
        return variants
            .Select(variant =>
            {
                var detail = details.FirstOrDefault(d => d.SizeLabel == variant.Size);
                return new ProductVariantDto
                {
                    ProductVariantId = variant.Id,
                    OutfitId = variant.OutfitID,
                    Size = variant.Size,
                    Price = variant.Price,
                    StockQuantity = variant.StockQuantity,
                    ColorVariant = variant.ColorVariant,
                    MaterialType = variant.MaterialType,
                    SKUCode = variant.SKUCode,
                    VariantImageURL = variant.VariantImageURL,
                    Measurements = VariantSizeChartSyncHelper.ToDtos(detail)
                };
            })
            .ToList();
    }
}
