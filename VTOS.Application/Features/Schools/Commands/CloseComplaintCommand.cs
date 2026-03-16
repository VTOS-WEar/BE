using Microsoft.EntityFrameworkCore;
using VTOS.Application.Abstractions;
using VTOS.Application.Common;
using VTOS.Domain.Enums;

namespace VTOS.Application.Features.Schools.Commands;

/// <summary>School closes a complaint that has been resolved by the provider.</summary>
public record CloseComplaintCommand(Guid UserId, Guid ComplaintId);

public interface ICloseComplaintCommandHandler
{
    Task<Result<string>> HandleAsync(CloseComplaintCommand command, CancellationToken ct = default);
}

public class CloseComplaintCommandHandler : ICloseComplaintCommandHandler
{
    private readonly IApplicationDbContext _db;

    public CloseComplaintCommandHandler(IApplicationDbContext db) => _db = db;

    public async Task<Result<string>> HandleAsync(CloseComplaintCommand command, CancellationToken ct = default)
    {
        var user = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == command.UserId, ct);
        if (user?.SchoolID == null)
            return Result<string>.Failure("School not found.", "SCHOOL_NOT_FOUND");

        var complaint = await _db.Complaints
            .FirstOrDefaultAsync(c => c.Id == command.ComplaintId && c.SchoolID == user.SchoolID.Value, ct);

        if (complaint == null)
            return Result<string>.Failure("Complaint not found.", "COMPLAINT_NOT_FOUND");

        if (complaint.Status != ComplaintStatus.Resolved)
            return Result<string>.Failure("Only resolved complaints can be closed.", "INVALID_STATUS");

        complaint.Status = ComplaintStatus.Closed;
        await _db.SaveChangesAsync(ct);

        return Result<string>.Success("Complaint closed successfully.");
    }
}
