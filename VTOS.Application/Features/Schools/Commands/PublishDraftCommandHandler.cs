using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using VTOS.Application.Abstractions;
using VTOS.Application.Common;
using VTOS.Domain.Enums;

namespace VTOS.Application.Features.Schools.Commands;

/// <summary>
/// UC-45b: Publish a draft campaign, making it Active and open for parent orders.
/// Validates date range, outfit availability, and provider contracts before activation.
/// Re-validates all outfits (same rules as PublishCampaign).
/// </summary>
public class PublishDraftCommandHandler : IPublishDraftCommandHandler
{
    private readonly IApplicationDbContext _db;
    private readonly IMemoryCache _cache;

    public PublishDraftCommandHandler(IApplicationDbContext db, IMemoryCache cache)
    {
        _db = db;
        _cache = cache;
    }

    public async Task<Result<PublishCampaignResponseDto>> HandleAsync(PublishDraftCommand command, CancellationToken ct = default)
    {
        // 1. Resolve school
        var user = await _db.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == command.UserId, ct);

        if (user == null)
            return Result<PublishCampaignResponseDto>.Failure("User not found.", "USER_NOT_FOUND");

        var schoolMgr = await _db.SchoolManagers.AsNoTracking()
            .FirstOrDefaultAsync(m => m.UserID == user.Id, ct);

        if (schoolMgr == null)
            return Result<PublishCampaignResponseDto>.Failure(
                "School profile not set up. Please create your school profile first.", "SCHOOL_NOT_FOUND");

        var schoolId = schoolMgr.SchoolID;

        // 2. Load campaign — must exist and belong to school
        var campaign = await _db.Campaigns
            .AsNoTracking()
            .Include(c => c.CampaignOutfits)
            .FirstOrDefaultAsync(c => c.Id == command.CampaignId && c.SchoolID == schoolId, ct);

        if (campaign == null)
            return Result<PublishCampaignResponseDto>.Failure("Campaign not found.", "CAMPAIGN_NOT_FOUND");

        // 3. Guard: only Draft campaigns can be published this way
        if (campaign.Status != CampaignStatus.Draft)
            return Result<PublishCampaignResponseDto>.Failure(
                $"Only Draft campaigns can be published. Current status: {campaign.Status}.",
                "INVALID_STATUS");

        // 4. Re-validate date range
        if (campaign.EndDate <= campaign.StartDate)
            return Result<PublishCampaignResponseDto>.Failure(
                "End date must be after start date.", "INVALID_DATE_RANGE");

        // 5. Re-validate all outfits exist, belong to school, and are available
        var outfitIds = campaign.CampaignOutfits.Select(co => co.OutfitID).Distinct().ToList();

        var schoolOutfits = await _db.Outfits
            .AsNoTracking()
            .Where(o => outfitIds.Contains(o.Id) && !o.IsDeleted && o.IsAvailable)
            .ToListAsync(ct);

        if (outfitIds.Count != schoolOutfits.Count)
        {
            var missingIds = outfitIds.Except(schoolOutfits.Select(o => o.Id)).ToList();
            return Result<PublishCampaignResponseDto>.Failure(
                $"Some outfits are no longer available: {string.Join(", ", missingIds)}.",
                "OUTFIT_NOT_AVAILABLE");
        }

        foreach (var outfitId in outfitIds)
        {
            var outfit = schoolOutfits.First(o => o.Id == outfitId);
            if (outfit.SchoolID != schoolId)
                return Result<PublishCampaignResponseDto>.Failure(
                    $"Outfit '{outfitId}' does not belong to your school.", "OUTFIT_NOT_OWNED");
        }

        // 6. Re-validate provider contracts + transition contracts to InUse
        var outfitsWithProvider = campaign.CampaignOutfits.Where(co => co.ProviderID.HasValue).ToList();
        if (outfitsWithProvider.Any())
        {
            var providerIds = outfitsWithProvider.Select(co => co.ProviderID!.Value).Distinct().ToList();

            var approvedContracts = await _db.Contracts
                .Where(c => c.SchoolID == schoolId
                         && c.Status == "Approved"
                         && providerIds.Contains(c.ProviderID))
                .Include(c => c.ContractItems)
                .ToListAsync(ct);

            foreach (var co in outfitsWithProvider)
            {
                var contract = approvedContracts
                    .FirstOrDefault(c => c.ProviderID == co.ProviderID!.Value
                                      && c.ContractItems.Any(ci => ci.OutfitID == co.OutfitID));

                if (contract == null)
                    return Result<PublishCampaignResponseDto>.Failure(
                        $"Provider '{co.ProviderID}' does not have an approved contract for outfit '{co.OutfitID}'.",
                        "PROVIDER_NO_CONTRACT");
            }

            // Transition contracts to InUse (in-memory update, saved in step 9)
            var contractIds = approvedContracts
                .Where(c => outfitsWithProvider.Select(co => co.ProviderID).Contains(c.ProviderID))
                .Select(c => c.Id)
                .Distinct()
                .ToList();

            var trackedContracts = await _db.Contracts
                .Where(c => contractIds.Contains(c.Id))
                .ToListAsync(ct);

            foreach (var contract in trackedContracts)
                contract.Status = "InUse";
        }

        // 7. Transition campaign to Active
        // Load as tracked to save
        var trackedCampaign = await _db.Campaigns
            .FirstAsync(c => c.Id == command.CampaignId, ct);

        trackedCampaign.Status = CampaignStatus.Active;
        trackedCampaign.UpdatedAt = DateTime.UtcNow;

        // 9. Save
        await _db.SaveChangesAsync(ct);

        // 10. Invalidate caches
        _cache.Remove($"public:school:{schoolId}");

        return Result<PublishCampaignResponseDto>.Success(new PublishCampaignResponseDto
        {
            CampaignId = trackedCampaign.Id,
            CampaignName = trackedCampaign.CampaignName,
            Description = trackedCampaign.Description,
            Status = trackedCampaign.Status.ToString(),
            StartDate = trackedCampaign.StartDate,
            EndDate = trackedCampaign.EndDate,
            OutfitCount = campaign.CampaignOutfits.Count,
            CreatedAt = trackedCampaign.CreatedAt
        });
    }
}
