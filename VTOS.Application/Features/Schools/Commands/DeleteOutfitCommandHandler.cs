using Microsoft.EntityFrameworkCore;
using VTOS.Application.Abstractions;
using VTOS.Application.Common;
using VTOS.Domain.Enums;

namespace VTOS.Application.Features.Schools.Commands;

/// <summary>
/// Soft-delete an outfit (sets IsDeleted = true).
/// Validates that the outfit belongs to the current school.
/// Blocked if the outfit is linked to an Active/Paused/Locked campaign
/// (parents have placed orders — the outfit cannot be deleted).
/// </summary>
public class DeleteOutfitCommandHandler : IDeleteOutfitCommandHandler
{
    private readonly IApplicationDbContext _db;

    public DeleteOutfitCommandHandler(IApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<Result<bool>> HandleAsync(DeleteOutfitCommand command, CancellationToken ct = default)
    {
        var user = await _db.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == command.UserId, ct);

        if (user == null)
            return Result<bool>.Failure("User not found.", "USER_NOT_FOUND");

        var schoolMgr = await _db.SchoolManagers.AsNoTracking().FirstOrDefaultAsync(m => m.UserID == user.Id, ct);

        if (schoolMgr == null)
            return Result<bool>.Failure("School profile not set up yet.", "SCHOOL_NOT_FOUND");

        var outfit = await _db.Outfits
            .FirstOrDefaultAsync(o => o.Id == command.OutfitId && !o.IsDeleted, ct);

        if (outfit == null)
            return Result<bool>.Failure("Outfit not found.", "OUTFIT_NOT_FOUND");

        // Security: ensure the outfit belongs to this school
        if (outfit.SchoolID != schoolMgr.SchoolID)
            return Result<bool>.Failure("You do not have permission to delete this outfit.", "OUTFIT_NOT_FOUND");

        // Block deletion if outfit is linked to an Active/Paused/Locked campaign
        var hasActiveCampaign = await _db.CampaignOutfits
            .AsNoTracking()
            .AnyAsync(co =>
                co.OutfitID == outfit.Id
                && (co.Campaign.Status == CampaignStatus.Active
                    || co.Campaign.Status == CampaignStatus.Paused
                    || co.Campaign.Status == CampaignStatus.Locked), ct);

        if (hasActiveCampaign)
            return Result<bool>.Failure(
                "Không thể xóa đồng phục đang thuộc chiến dịch đang hoạt động.",
                "OUTFIT_IN_ACTIVE_CAMPAIGN");

        outfit.IsDeleted = true;
        outfit.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync(ct);

        return Result<bool>.Success(true);
    }
}
