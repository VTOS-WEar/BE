using Microsoft.EntityFrameworkCore;
using VTOS.Application.Abstractions;
using VTOS.Application.Common;

namespace VTOS.Application.Features.Schools.Queries;

// ── DTOs ──

/// <summary>
/// A provider available for a specific outfit via an Approved Contract.
/// </summary>
public record ContractedProviderDto(
    Guid ProviderId,
    string ProviderName,
    Guid ContractId,
    string ContractName,
    decimal PricePerUnit
);

/// <summary>
/// Response: for each outfit, the list of contracted providers.
/// Key = outfitId (string), Value = list of providers.
/// </summary>
public record GetContractedProvidersForOutfitsResponse(
    Dictionary<string, List<ContractedProviderDto>> OutfitProviders
);

// ── Query ──

public record GetContractedProvidersForOutfitsQuery(Guid UserId);

public interface IGetContractedProvidersForOutfitsQueryHandler
{
    Task<Result<GetContractedProvidersForOutfitsResponse>> HandleAsync(
        GetContractedProvidersForOutfitsQuery query, CancellationToken ct = default);
}

// ── Handler ──

/// <summary>
/// Returns ALL approved-contracted providers grouped by outfit for the school.
/// The FE will filter by selected outfits client-side.
/// This avoids a complex query-string parameter and is simpler for a small dataset.
/// </summary>
public class GetContractedProvidersForOutfitsQueryHandler : IGetContractedProvidersForOutfitsQueryHandler
{
    private readonly IApplicationDbContext _db;

    public GetContractedProvidersForOutfitsQueryHandler(IApplicationDbContext db) => _db = db;

    public async Task<Result<GetContractedProvidersForOutfitsResponse>> HandleAsync(
        GetContractedProvidersForOutfitsQuery query, CancellationToken ct = default)
    {
        // 1. Resolve school
        var schoolMgr = await _db.SchoolManagers.AsNoTracking()
            .FirstOrDefaultAsync(m => m.UserID == query.UserId, ct);

        if (schoolMgr == null)
            return Result<GetContractedProvidersForOutfitsResponse>.Failure(
                "School not found.", "SCHOOL_NOT_FOUND");

        var schoolId = schoolMgr.SchoolID;

        // 2. Lazy expiration — auto-expire Approved contracts past ExpiresAt
        var expiredContracts = await _db.Contracts
            .Where(c => c.SchoolID == schoolId
                     && c.Status == "Approved"
                     && c.ExpiresAt <= DateTime.UtcNow)
            .ToListAsync(ct);

        if (expiredContracts.Any())
        {
            foreach (var c in expiredContracts)
                c.Status = "Expired";
            await _db.SaveChangesAsync(ct);
        }

        // 3. Query Approved + not expired contracts with their items
        var contractData = await _db.Contracts.AsNoTracking()
            .Where(c => c.SchoolID == schoolId && c.Status == "Approved")
            .Include(c => c.Provider)
            .Include(c => c.ContractItems)
            .SelectMany(c => c.ContractItems.Select(ci => new
            {
                OutfitId = ci.OutfitID.ToString(),
                ProviderId = c.ProviderID,
                ProviderName = c.Provider.ProviderName,
                ContractId = c.Id,
                ContractName = c.ContractName,
                PricePerUnit = ci.PricePerUnit,
            }))
            .ToListAsync(ct);

        // 4. Group by OutfitId
        var grouped = contractData
            .GroupBy(x => x.OutfitId)
            .ToDictionary(
                g => g.Key,
                g => g.Select(x => new ContractedProviderDto(
                    x.ProviderId, x.ProviderName,
                    x.ContractId, x.ContractName,
                    x.PricePerUnit
                )).ToList()
            );

        return Result<GetContractedProvidersForOutfitsResponse>.Success(
            new GetContractedProvidersForOutfitsResponse(grouped));
    }
}
