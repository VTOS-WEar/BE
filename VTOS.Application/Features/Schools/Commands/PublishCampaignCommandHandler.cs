using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using VTOS.Application.Abstractions;
using VTOS.Application.Common;
using VTOS.Domain.Entities;
using VTOS.Domain.Enums;

namespace VTOS.Application.Features.Schools.Commands;

/// <summary>
/// UC-44: Publish (or save as draft) a uniform pre-order campaign.
/// - Resolves SchoolID from the logged-in user.
/// - Validates all outfit IDs belong to the school and are available.
/// - Creates Campaign + CampaignOutfit records atomically.
/// - Supports SaveAsDraft (status = Draft) or Publish (status = Active).
/// </summary>
public class PublishCampaignCommandHandler : IPublishCampaignCommandHandler
{
    private readonly IApplicationDbContext _db;
    private readonly IMemoryCache _cache;

    public PublishCampaignCommandHandler(IApplicationDbContext db, IMemoryCache cache)
    {
        _db = db;
        _cache = cache;
    }

    public async Task<Result<PublishCampaignResponseDto>> HandleAsync(PublishCampaignCommand command, CancellationToken ct = default)
    {
        // 1. Resolve school
        var user = await _db.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == command.UserId, ct);

        if (user == null)
            return Result<PublishCampaignResponseDto>.Failure("User not found.", "USER_NOT_FOUND");

        var schoolMgr = await _db.SchoolManagers.AsNoTracking().FirstOrDefaultAsync(m => m.UserID == user.Id, ct);

        if (schoolMgr == null)
            return Result<PublishCampaignResponseDto>.Failure(
                "School profile not set up yet. Please create your school profile first.", "SCHOOL_NOT_FOUND");

        var schoolId = schoolMgr.SchoolID;

        // 2. Validate date range (SRS 44.E1)
        if (command.EndDate <= command.StartDate)
            return Result<PublishCampaignResponseDto>.Failure(
                "End date must be after start date.", "INVALID_DATE_RANGE");

        // 3. Validate all outfits exist, belong to school, and are available
        var outfitIds = command.Outfits.Select(o => o.OutfitId).Distinct().ToList();

        var schoolOutfits = await _db.Outfits
            .AsNoTracking()
            .Where(o => outfitIds.Contains(o.Id) && !o.IsDeleted)
            .ToListAsync(ct);

        foreach (var input in command.Outfits)
        {
            var outfit = schoolOutfits.FirstOrDefault(o => o.Id == input.OutfitId);
            if (outfit == null)
                return Result<PublishCampaignResponseDto>.Failure(
                    $"Outfit '{input.OutfitId}' not found.", "OUTFIT_NOT_FOUND");

            if (outfit.SchoolID != schoolId)
                return Result<PublishCampaignResponseDto>.Failure(
                    $"Outfit '{input.OutfitId}' does not belong to your school.", "OUTFIT_NOT_OWNED");

            if (!outfit.IsAvailable)
                return Result<PublishCampaignResponseDto>.Failure(
                    $"Outfit '{outfit.OutfitName}' is not available.", "OUTFIT_NOT_AVAILABLE");
        }

        // 3.5 Validate contract exists when ProviderID is specified (Option B — optional provider)
        var outfitsWithProvider = command.Outfits.Where(o => o.ProviderId.HasValue).ToList();
        if (outfitsWithProvider.Any())
        {
            var providerIds = outfitsWithProvider.Select(o => o.ProviderId!.Value).Distinct().ToList();

            // Load approved contracts for this school + these providers
            var approvedContracts = await _db.Contracts.AsNoTracking()
                .Where(c => c.SchoolID == schoolId
                         && c.Status == "Approved"
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
                    return Result<PublishCampaignResponseDto>.Failure(
                        $"Provider '{input.ProviderId}' does not have an approved contract for outfit '{outfit?.OutfitName ?? input.OutfitId.ToString()}'.",
                        "PROVIDER_NO_CONTRACT");
                }
            }
        }

        // 4. Create Campaign — Draft or Active based on SaveAsDraft flag
        var campaign = new Campaign
        {
            Id = Guid.NewGuid(),
            SchoolID = schoolId,
            CampaignName = command.CampaignName,
            Description = command.Description,
            StartDate = command.StartDate,
            EndDate = command.EndDate,
            Status = command.SaveAsDraft ? CampaignStatus.Draft : CampaignStatus.Active,
            CreatedAt = DateTime.UtcNow
        };

        _db.Campaigns.Add(campaign);

        // 5. Create CampaignOutfit entries (ProviderID nullable — can be assigned later)
        foreach (var input in command.Outfits)
        {
            var campaignOutfit = new CampaignOutfit
            {
                Id = Guid.NewGuid(),
                CampaignID = campaign.Id,
                OutfitID = input.OutfitId,
                ProviderID = input.ProviderId,
                CampaignPrice = input.CampaignPrice,
                MaxQuantity = input.MaxQuantity
            };
            _db.CampaignOutfits.Add(campaignOutfit);
        }

        // 6. Save atomically
        await _db.SaveChangesAsync(ct);

        // 7. Invalidate public caches so parents see the new campaign immediately
        _cache.Remove($"public:school:{schoolId}");

        return Result<PublishCampaignResponseDto>.Success(new PublishCampaignResponseDto
        {
            CampaignId = campaign.Id,
            CampaignName = campaign.CampaignName,
            Description = campaign.Description,
            Status = campaign.Status.ToString(),
            StartDate = campaign.StartDate,
            EndDate = campaign.EndDate,
            OutfitCount = command.Outfits.Count,
            CreatedAt = campaign.CreatedAt
        });
    }
}
