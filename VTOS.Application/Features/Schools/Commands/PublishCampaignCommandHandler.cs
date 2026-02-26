using Microsoft.EntityFrameworkCore;
using VTOS.Application.Abstractions;
using VTOS.Application.Common;
using VTOS.Domain.Entities;
using VTOS.Domain.Enums;

namespace VTOS.Application.Features.Schools.Commands;

/// <summary>
/// UC-44: Publish a uniform pre-order campaign.
/// - Resolves SchoolID from the logged-in user.
/// - Validates all outfit IDs belong to the school and are available.
/// - Creates Campaign + CampaignOutfit records atomically.
/// </summary>
public class PublishCampaignCommandHandler : IPublishCampaignCommandHandler
{
    private readonly IApplicationDbContext _db;

    public PublishCampaignCommandHandler(IApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<Result<PublishCampaignResponseDto>> HandleAsync(PublishCampaignCommand command, CancellationToken ct = default)
    {
        // 1. Resolve school
        var user = await _db.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == command.UserId, ct);

        if (user == null)
            return Result<PublishCampaignResponseDto>.Failure("User not found.", "USER_NOT_FOUND");

        if (user.SchoolID == null)
            return Result<PublishCampaignResponseDto>.Failure(
                "School profile not set up yet. Please create your school profile first.", "SCHOOL_NOT_FOUND");

        var schoolId = user.SchoolID.Value;

        // 2. Validate all outfits exist, belong to school, and are available
        var outfitIds = command.Outfits.Select(o => o.OutfitId).Distinct().ToList();

        var schoolOutfits = await _db.Outfits
            .AsNoTracking()
            .Where(o => outfitIds.Contains(o.Id) && !o.IsDeleted)
            .ToListAsync(ct);

        // Check every requested outfit is found and belongs to this school
        foreach (var input in command.Outfits)
        {
            var outfit = schoolOutfits.FirstOrDefault(o => o.Id == input.OutfitId);
            if (outfit == null)
                return Result<PublishCampaignResponseDto>.Failure(
                    $"Outfit '{input.OutfitId}' not found.", "OUTFIT_NOT_FOUND");

            if (outfit.SchoolID != schoolId)
                return Result<PublishCampaignResponseDto>.Failure(
                    $"Outfit '{input.OutfitId}' does not belong to your school.", "OUTFIT_NOT_FOUND");

            if (!outfit.IsAvailable)
                return Result<PublishCampaignResponseDto>.Failure(
                    $"Outfit '{outfit.OutfitName}' is not available.", "OUTFIT_NOT_AVAILABLE");
        }

        // 3. Create Campaign
        var campaign = new Campaign
        {
            Id = Guid.NewGuid(),
            SchoolID = schoolId,
            CampaignName = command.CampaignName,
            Description = command.Description,
            StartDate = command.StartDate,
            EndDate = command.EndDate,
            Status = CampaignStatus.Active,
            CreatedAt = DateTime.UtcNow
        };

        _db.Campaigns.Add(campaign);

        // 4. Create CampaignOutfit entries
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

        // 5. Save atomically
        await _db.SaveChangesAsync(ct);

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
