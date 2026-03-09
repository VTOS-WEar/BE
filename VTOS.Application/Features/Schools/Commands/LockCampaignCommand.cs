using Microsoft.EntityFrameworkCore;
using VTOS.Application.Abstractions;
using VTOS.Application.Common;
using VTOS.Domain.Enums;

namespace VTOS.Application.Features.Schools.Commands;

public record LockCampaignCommand(Guid UserId, Guid CampaignId);

public interface ILockCampaignCommandHandler
{
    Task<Result<string>> HandleAsync(LockCampaignCommand command, CancellationToken ct = default);
}

public class LockCampaignCommandHandler : ILockCampaignCommandHandler
{
    private readonly IApplicationDbContext _db;

    public LockCampaignCommandHandler(IApplicationDbContext db) => _db = db;

    public async Task<Result<string>> HandleAsync(LockCampaignCommand command, CancellationToken ct = default)
    {
        var user = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == command.UserId, ct);
        if (user?.SchoolID == null)
            return Result<string>.Failure("School not found.", "SCHOOL_NOT_FOUND");

        var campaign = await _db.Campaigns
            .FirstOrDefaultAsync(c => c.Id == command.CampaignId && c.SchoolID == user.SchoolID.Value, ct);

        if (campaign == null)
            return Result<string>.Failure("Campaign not found.", "CAMPAIGN_NOT_FOUND");

        if (campaign.Status != CampaignStatus.Active)
            return Result<string>.Failure(
                $"Only Active campaigns can be locked. Current status: {campaign.Status}.",
                "INVALID_STATUS");

        campaign.Status = CampaignStatus.Locked;
        await _db.SaveChangesAsync(ct);

        return Result<string>.Success("Campaign locked successfully. No more orders will be accepted.");
    }
}
