using Microsoft.EntityFrameworkCore;
using VTOS.Application.Abstractions;
using VTOS.Application.Features.Public.DTOs;
using VTOS.Domain.Enums;

namespace VTOS.Application.Features.Public.Queries;

public record GetSchoolSemesterCatalogQuery(Guid SchoolId);
public record GetAllSchoolSemesterCatalogsQuery(Guid SchoolId);
public record GetProvidersForPublicationOutfitQuery(Guid SemesterPublicationId, Guid OutfitId);
public record GetProviderPublicProfileQuery(Guid ProviderId);

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
            .Where(spp => spp.SemesterPublicationID == publication.Id && spp.Status == SemPublicationProviderStatus.Active)
            .Select(spp => new
            {
                spp.ProviderID,
                Provider = spp.Provider,
                spp.ContractID
            })
            .ToListAsync(ct);

        var outfitIds = publicationOutfits.Select(x => x.OutfitID).Distinct().ToList();
        var contractIds = approvedProviders.Where(x => x.ContractID.HasValue).Select(x => x.ContractID!.Value).Distinct().ToList();

        var variants = await _context.ProductVariants
            .AsNoTracking()
            .Where(v => outfitIds.Contains(v.OutfitID) && !v.IsDeleted)
            .ToListAsync(ct);

        var contractItemPrices = await _context.ContractItems
            .AsNoTracking()
            .Where(ci => contractIds.Contains(ci.ContractID) && outfitIds.Contains(ci.OutfitID))
            .ToListAsync(ct);

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
                        var contractPrice = p.ContractID.HasValue
                            ? contractItemPrices
                                .Where(ci => ci.ContractID == p.ContractID.Value && ci.OutfitID == x.OutfitID)
                                .Select(ci => (decimal?)ci.PricePerUnit)
                                .FirstOrDefault()
                            : null;

                        return new SemesterCatalogProviderDto
                        {
                            ProviderId = p.ProviderID,
                            ProviderName = p.Provider.ProviderName,
                            ContactEmail = p.Provider.Email,
                            Price = contractPrice ?? x.Outfit.Price,
                            AverageRating = p.Provider.AverageRating,
                            TotalRatings = p.Provider.TotalRatings,
                            TotalCompletedOrders = p.Provider.TotalCompletedOrders
                        };
                    })
                    .OrderBy(p => p.Price)
                    .ThenBy(p => p.ProviderName)
                    .ToList();

                return new SemesterCatalogOutfitDto
                {
                    OutfitId = x.OutfitID,
                    OutfitName = x.Outfit.OutfitName,
                    Description = x.Outfit.Description,
                    MainImageUrl = x.Outfit.MainImageURL,
                    Price = x.Outfit.Price,
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
        var publications = await _context.SemesterPublications
            .AsNoTracking()
            .Where(sp => sp.SchoolID == query.SchoolId)
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
                .Where(spp => spp.SemesterPublicationID == publication.Id && spp.Status == SemPublicationProviderStatus.Active)
                .Select(spp => new
                {
                    spp.ProviderID,
                    Provider = spp.Provider,
                    spp.ContractID
                })
                .ToListAsync(ct);

            var outfitIds = publicationOutfits.Select(x => x.OutfitID).Distinct().ToList();
            var contractIds = approvedProviders.Where(x => x.ContractID.HasValue).Select(x => x.ContractID!.Value).Distinct().ToList();

            var variants = await _context.ProductVariants
                .AsNoTracking()
                .Where(v => outfitIds.Contains(v.OutfitID) && !v.IsDeleted)
                .ToListAsync(ct);

            var contractItemPrices = await _context.ContractItems
                .AsNoTracking()
                .Where(ci => contractIds.Contains(ci.ContractID) && outfitIds.Contains(ci.OutfitID))
                .ToListAsync(ct);

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
                            var contractPrice = p.ContractID.HasValue
                                ? contractItemPrices
                                    .Where(ci => ci.ContractID == p.ContractID.Value && ci.OutfitID == x.OutfitID)
                                    .Select(ci => (decimal?)ci.PricePerUnit)
                                    .FirstOrDefault()
                                : null;

                            return new SemesterCatalogProviderDto
                            {
                                ProviderId = p.ProviderID,
                                ProviderName = p.Provider.ProviderName,
                                ContactEmail = p.Provider.Email,
                                Price = contractPrice ?? x.Outfit.Price,
                                AverageRating = p.Provider.AverageRating,
                                TotalRatings = p.Provider.TotalRatings,
                                TotalCompletedOrders = p.Provider.TotalCompletedOrders
                            };
                        })
                        .OrderBy(p => p.Price)
                        .ThenBy(p => p.ProviderName)
                        .ToList();

                    return new SemesterCatalogOutfitDto
                    {
                        OutfitId = x.OutfitID,
                        OutfitName = x.Outfit.OutfitName,
                        Description = x.Outfit.Description,
                        MainImageUrl = x.Outfit.MainImageURL,
                        Price = x.Outfit.Price,
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
        var publicationExists = await _context.SemesterPublications
            .AsNoTracking()
            .AnyAsync(sp => sp.Id == query.SemesterPublicationId && sp.Status == SemesterPublicationStatus.Active, ct);

        if (!publicationExists)
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
            .Where(spp => spp.SemesterPublicationID == query.SemesterPublicationId && spp.Status == SemPublicationProviderStatus.Active)
            .Select(spp => new
            {
                spp.ProviderID,
                Provider = spp.Provider,
                spp.ContractID
            })
            .ToListAsync(ct);

        var contractIds = providers.Where(x => x.ContractID.HasValue).Select(x => x.ContractID!.Value).Distinct().ToList();
        var contractItems = await _context.ContractItems
            .AsNoTracking()
            .Where(ci => contractIds.Contains(ci.ContractID) && ci.OutfitID == query.OutfitId)
            .ToListAsync(ct);

        return providers
            .Select(p =>
            {
                var contractPrice = p.ContractID.HasValue
                    ? contractItems.Where(ci => ci.ContractID == p.ContractID.Value).Select(ci => (decimal?)ci.PricePerUnit).FirstOrDefault()
                    : null;

                return new SemesterCatalogProviderDto
                {
                    ProviderId = p.ProviderID,
                    ProviderName = p.Provider.ProviderName,
                    ContactEmail = p.Provider.Email,
                    Price = contractPrice ?? outfit.Price,
                    AverageRating = p.Provider.AverageRating,
                    TotalRatings = p.Provider.TotalRatings,
                    TotalCompletedOrders = p.Provider.TotalCompletedOrders
                };
            })
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
        return await _context.Providers
            .AsNoTracking()
            .Where(p => p.Id == query.ProviderId && !p.IsDeleted)
            .Select(p => new PublicProviderProfileDto
            {
                ProviderId = p.Id,
                ProviderName = p.ProviderName,
                ContactPersonName = p.ContactPersonName,
                Phone = p.Phone,
                Email = p.Email,
                Address = p.Address,
                AverageRating = p.AverageRating,
                TotalRatings = p.TotalRatings,
                TotalCompletedOrders = p.TotalCompletedOrders
            })
            .FirstOrDefaultAsync(ct);
    }
}
