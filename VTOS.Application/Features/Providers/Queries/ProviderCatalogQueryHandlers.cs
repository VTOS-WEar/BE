using Microsoft.EntityFrameworkCore;
using VTOS.Application.Abstractions;
using VTOS.Application.Common;
using VTOS.Application.Features.Providers.DTOs;
using VTOS.Domain.Enums;

namespace VTOS.Application.Features.Providers.Queries;

public record GetProviderCatalogQuery(
    Guid UserId,
    int Page = 1,
    int PageSize = 5,
    string? Status = null,
    string? Search = null);

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

        var publicationProviders = await _context.SemesterPublicationProviders
            .AsNoTracking()
            .Include(x => x.SemesterPublication)
                .ThenInclude(x => x.School)
            .Include(x => x.Contract)
            .Where(x => x.ProviderID == providerId.Value)
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
                        DisplayName = catalogItem?.DisplayName ?? publicationOutfit.Outfit.OutfitName,
                        ShortDescription = catalogItem?.ShortDescription ?? publicationOutfit.Outfit.Description,
                        MaterialDetails = catalogItem?.MaterialDetails,
                        PublicationPrice = catalogItem?.PublicationPrice,
                        PostDeadlinePrice = catalogItem?.PostDeadlinePrice,
                        Status = catalogItem?.Status.ToString() ?? ProviderCatalogItemStatus.Draft.ToString()
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
            Published = rows.Sum(x => x.Items.Count(item => item.Status == ProviderCatalogItemStatus.Published.ToString())),
            NeedsSetup = rows.Sum(x => x.Items.Count(item =>
                !item.CatalogItemId.HasValue ||
                item.Status == ProviderCatalogItemStatus.Draft.ToString()))
        };

        var filteredRows = rows.AsEnumerable();
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
}
