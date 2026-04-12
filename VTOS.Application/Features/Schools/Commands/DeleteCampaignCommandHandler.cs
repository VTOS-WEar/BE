using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using VTOS.Application.Abstractions;
using VTOS.Application.Common;
using VTOS.Domain.Enums;

namespace VTOS.Application.Features.Schools.Commands;

/// <summary>
/// UC-45c: Delete a draft campaign.
/// Only Draft campaigns with no orders can be deleted.
/// Active, Completed, and Locked campaigns are protected.
/// </summary>
public class DeleteCampaignCommandHandler : IDeleteCampaignCommandHandler
{
    private readonly IApplicationDbContext _db;
    private readonly IMemoryCache _cache;

    public DeleteCampaignCommandHandler(IApplicationDbContext db, IMemoryCache cache)
    {
        _db = db;
        _cache = cache;
    }

    public async Task<Result<string>> HandleAsync(DeleteCampaignCommand command, CancellationToken ct = default)
    {
        // 1. Resolve school
        var user = await _db.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == command.UserId, ct);

        if (user == null)
            return Result<string>.Failure("User not found.", "USER_NOT_FOUND");

        var schoolMgr = await _db.SchoolManagers.AsNoTracking()
            .FirstOrDefaultAsync(m => m.UserID == user.Id, ct);

        if (schoolMgr == null)
            return Result<string>.Failure(
                "School profile not set up.", "SCHOOL_NOT_FOUND");

        var schoolId = schoolMgr.SchoolID;

        // 2. Load campaign — must exist and belong to school
        var campaign = await _db.Campaigns
            .Include(c => c.CampaignOutfits)
            .FirstOrDefaultAsync(c => c.Id == command.CampaignId && c.SchoolID == schoolId, ct);

        if (campaign == null)
            return Result<string>.Failure("Campaign not found.", "CAMPAIGN_NOT_FOUND");

        // 3. Guard: only Draft campaigns can be deleted
        if (campaign.Status != CampaignStatus.Draft)
            return Result<string>.Failure(
                $"Only Draft campaigns can be deleted. Current status: {campaign.Status}.",
                "INVALID_STATUS");

        // 4. Guard: campaign must have no orders
        var orderCount = await _db.Orders
            .AsNoTracking()
            .CountAsync(o => o.CampaignID == command.CampaignId, ct);

        if (orderCount > 0)
            return Result<string>.Failure(
                "Cannot delete a campaign that has orders. Close or cancel the campaign instead.",
                "CAMPAIGN_HAS_ORDERS");

        // 5. Delete CampaignOutfit entries first (FK constraint)
        _db.CampaignOutfits.RemoveRange(campaign.CampaignOutfits);

        // 6. Delete the campaign
        _db.Campaigns.Remove(campaign);

        // 7. Save
        await _db.SaveChangesAsync(ct);

        // 8. Invalidate caches
        _cache.Remove($"public:school:{schoolId}");

        return Result<string>.Success($"Campaign '{campaign.CampaignName}' deleted successfully.");
    }
}
