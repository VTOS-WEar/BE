using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using VTOS.Application.Abstractions;
using VTOS.Application.Common;
using VTOS.Application.Features.Schools.Queries;
using VTOS.Domain.Enums;

namespace VTOS.Application.Features.Schools.Commands;

/// <summary>
/// UC-45a: Edit a draft campaign.
/// Only Draft campaigns can be edited. Updates name, dates, description, and outfit lineup.
/// Re-validates all outfits and provider contracts before saving.
/// </summary>
public class UpdateCampaignCommandHandler : IUpdateCampaignCommandHandler
{
    private readonly IApplicationDbContext _db;
    private readonly IMemoryCache _cache;

    public UpdateCampaignCommandHandler(IApplicationDbContext db, IMemoryCache cache)
    {
        _db = db;
        _cache = cache;
    }

    public async Task<Result<CampaignDetailDto>> HandleAsync(UpdateCampaignCommand command, CancellationToken ct = default)
    {
        // 1. Resolve school
        var user = await _db.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == command.UserId, ct);

        if (user == null)
            return Result<CampaignDetailDto>.Failure("User not found.", "USER_NOT_FOUND");

        var schoolMgr = await _db.SchoolManagers.AsNoTracking()
            .FirstOrDefaultAsync(m => m.UserID == user.Id, ct);

        if (schoolMgr == null)
            return Result<CampaignDetailDto>.Failure(
                "School profile not set up. Please create your school profile first.", "SCHOOL_NOT_FOUND");

        var schoolId = schoolMgr.SchoolID;

        // 2. Load campaign — must exist and belong to school
        var campaign = await _db.Campaigns
            .Include(c => c.CampaignOutfits)
            .FirstOrDefaultAsync(c => c.Id == command.CampaignId && c.SchoolID == schoolId, ct);

        if (campaign == null)
            return Result<CampaignDetailDto>.Failure("Campaign not found.", "CAMPAIGN_NOT_FOUND");

        // 3. Guard: only Draft campaigns can be edited
        if (campaign.Status != CampaignStatus.Draft)
            return Result<CampaignDetailDto>.Failure(
                $"Only Draft campaigns can be edited. Current status: {campaign.Status}.",
                "INVALID_STATUS");

        // 4. Validate date range
        if (command.EndDate <= command.StartDate)
            return Result<CampaignDetailDto>.Failure(
                "End date must be after start date.", "INVALID_DATE_RANGE");

        // 5. Validate outfits exist, belong to school, and are available
        var outfitIds = command.Outfits.Select(o => o.OutfitId).Distinct().ToList();

        var schoolOutfits = await _db.Outfits
            .AsNoTracking()
            .Where(o => outfitIds.Contains(o.Id) && !o.IsDeleted)
            .ToListAsync(ct);

        foreach (var input in command.Outfits)
        {
            var outfit = schoolOutfits.FirstOrDefault(o => o.Id == input.OutfitId);
            if (outfit == null)
                return Result<CampaignDetailDto>.Failure(
                    $"Outfit '{input.OutfitId}' not found.", "OUTFIT_NOT_FOUND");

            if (outfit.SchoolID != schoolId)
                return Result<CampaignDetailDto>.Failure(
                    $"Outfit '{input.OutfitId}' does not belong to your school.", "OUTFIT_NOT_OWNED");

            if (!outfit.IsAvailable)
                return Result<CampaignDetailDto>.Failure(
                    $"Outfit '{outfit.OutfitName}' is not available.", "OUTFIT_NOT_AVAILABLE");
        }

        // 6. Validate provider contracts (same as PublishCampaign)
        var outfitsWithProvider = command.Outfits.Where(o => o.ProviderId.HasValue).ToList();
        if (outfitsWithProvider.Any())
        {
            var providerIds = outfitsWithProvider.Select(o => o.ProviderId!.Value).Distinct().ToList();
            var usableContractStatuses = new[] { "Active", "InUse" };

            var approvedContracts = await _db.Contracts.AsNoTracking()
                .Where(c => c.SchoolID == schoolId
                         && usableContractStatuses.Contains(c.Status)
                         && providerIds.Contains(c.ProviderID))
                .Include(c => c.ContractItems)
                .ToListAsync(ct);

            foreach (var input in outfitsWithProvider)
            {
                var contract = approvedContracts
                    .FirstOrDefault(c => c.ProviderID == input.ProviderId!.Value
                                      && c.ContractItems.Any(ci => ci.OutfitID == input.OutfitId));

                if (contract == null)
                {
                    var outfit = schoolOutfits.FirstOrDefault(o => o.Id == input.OutfitId);
                    return Result<CampaignDetailDto>.Failure(
                        $"Provider '{input.ProviderId}' does not have an active supplier agreement for outfit '{outfit?.OutfitName ?? input.OutfitId.ToString()}'.",
                        "PROVIDER_NO_CONTRACT");
                }
            }
        }

        // 7. Update campaign fields
        campaign.CampaignName = command.CampaignName;
        campaign.Description = command.Description;
        campaign.StartDate = command.StartDate;
        campaign.EndDate = command.EndDate;
        campaign.UpdatedAt = DateTime.UtcNow;

        // 8. Replace CampaignOutfit entries: remove old, add new
        _db.CampaignOutfits.RemoveRange(campaign.CampaignOutfits);

        foreach (var input in command.Outfits)
        {
            Guid? contractId = null;

            if (input.ProviderId.HasValue)
            {
                var usableContractStatuses = new[] { "Active", "InUse" };
                var matchingContract = await _db.Contracts
                    .Where(c => c.SchoolID == schoolId
                             && c.ProviderID == input.ProviderId.Value
                             && usableContractStatuses.Contains(c.Status))
                    .Include(c => c.ContractItems)
                    .FirstOrDefaultAsync(c => c.ContractItems.Any(ci => ci.OutfitID == input.OutfitId), ct);

                contractId = matchingContract?.Id;
            }

            _db.CampaignOutfits.Add(new Domain.Entities.CampaignOutfit
            {
                Id = Guid.NewGuid(),
                CampaignID = campaign.Id,
                OutfitID = input.OutfitId,
                ProviderID = input.ProviderId,
                ContractID = contractId,
                CampaignPrice = input.CampaignPrice,
                MaxQuantity = input.MaxQuantity
            });
        }

        // 9. Save atomically
        await _db.SaveChangesAsync(ct);

        // 10. Invalidate caches
        _cache.Remove($"public:school:{schoolId}");

        // 11. Return updated campaign detail
        var updatedOutfits = await _db.CampaignOutfits
            .Where(co => co.CampaignID == campaign.Id)
            .Include(co => co.Outfit)
            .ToListAsync(ct);

        var outfitDtos = updatedOutfits
            .Select(co => new CampaignOutfitDetailDto(
                co.Id, co.OutfitID, co.Outfit.OutfitName, co.Outfit.MainImageURL,
                co.CampaignPrice, co.MaxQuantity, co.ProviderID
            )).ToList();

        var orderCount = await _db.Orders
            .AsNoTracking()
            .CountAsync(o => o.CampaignID == campaign.Id, ct);

        return Result<CampaignDetailDto>.Success(new CampaignDetailDto(
            campaign.Id, campaign.CampaignName, campaign.Status.ToString(),
            campaign.StartDate, campaign.EndDate, campaign.Description,
            campaign.CreatedAt, orderCount, outfitDtos
        ));
    }
}
