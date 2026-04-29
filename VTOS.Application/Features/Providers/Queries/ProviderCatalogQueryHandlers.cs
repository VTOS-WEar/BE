using Microsoft.EntityFrameworkCore;
using VTOS.Application.Abstractions;
using VTOS.Application.Common;
using VTOS.Application.Features.Providers.DTOs;
using VTOS.Domain.Enums;

namespace VTOS.Application.Features.Providers.Queries;

public record GetProviderCatalogQuery(Guid UserId);

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
            .OrderByDescending(x => x.SemesterPublication.StartDate)
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

        var response = new ProviderCatalogResponse
        {
            Publications = publicationProviders.Select(publicationProvider =>
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
            }).ToList()
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
