using Microsoft.EntityFrameworkCore;
using VTOS.Application.Abstractions;
using VTOS.Application.Common;
using VTOS.Application.Features.Schools.Queries;

namespace VTOS.Application.Features.Providers.Queries;

public record GetProviderComplaintDetailQuery(Guid UserId, Guid ComplaintId);

public interface IGetProviderComplaintDetailQueryHandler
{
    Task<Result<ComplaintDetailDto>> HandleAsync(GetProviderComplaintDetailQuery query, CancellationToken ct = default);
}

public class GetProviderComplaintDetailQueryHandler : IGetProviderComplaintDetailQueryHandler
{
    private readonly IApplicationDbContext _db;

    public GetProviderComplaintDetailQueryHandler(IApplicationDbContext db) => _db = db;

    public async Task<Result<ComplaintDetailDto>> HandleAsync(GetProviderComplaintDetailQuery query, CancellationToken ct = default)
    {
        var user = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == query.UserId, ct);
        var providerMgr = await _db.ProviderManagers.AsNoTracking().FirstOrDefaultAsync(m => m.UserID == user.Id, ct);
        if (providerMgr?.ProviderID == null)
            return Result<ComplaintDetailDto>.Failure("Provider not found.", "PROVIDER_NOT_FOUND");

        var c = await _db.Complaints.AsNoTracking()
            .Include(x => x.Campaign)
            .Include(x => x.Batch)
            .Include(x => x.Provider)
            .FirstOrDefaultAsync(x => x.Id == query.ComplaintId && x.ProviderID == providerMgr.ProviderID, ct);

        if (c == null)
            return Result<ComplaintDetailDto>.Failure("Complaint not found.", "COMPLAINT_NOT_FOUND");

        return Result<ComplaintDetailDto>.Success(new ComplaintDetailDto(
            c.Id, c.CampaignID, c.Campaign?.CampaignName,
            c.BatchID, c.Batch?.BatchName,
            c.ProviderID, c.Provider?.ProviderName,
            c.Title, c.Description, c.Response,
            c.Status.ToString(), c.CreatedAt,
            c.RespondedAt, c.ResolvedAt
        ));
    }
}
