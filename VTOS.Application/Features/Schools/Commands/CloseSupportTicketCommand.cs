using Microsoft.EntityFrameworkCore;
using VTOS.Application.Abstractions;
using VTOS.Application.Common;
using VTOS.Domain.Enums;

namespace VTOS.Application.Features.Schools.Commands;

/// <summary>School closes a complaint that has been resolved by the provider.</summary>
public record CloseSupportTicketCommand(Guid UserId, Guid ComplaintId);

public interface ICloseSupportTicketCommandHandler
{
    Task<Result<string>> HandleAsync(CloseSupportTicketCommand command, CancellationToken ct = default);
}

public class CloseSupportTicketCommandHandler : ICloseSupportTicketCommandHandler
{
    private readonly IApplicationDbContext _db;

    public CloseSupportTicketCommandHandler(IApplicationDbContext db) => _db = db;

    public async Task<Result<string>> HandleAsync(CloseSupportTicketCommand command, CancellationToken ct = default)
    {
        var user = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == command.UserId, ct);
        if (user == null)
            return Result<string>.Failure("School not found.", "SCHOOL_NOT_FOUND");

        var schoolMgr = await _db.SchoolManagers.AsNoTracking().FirstOrDefaultAsync(m => m.UserID == user.Id, ct);
        if (schoolMgr?.SchoolID == null)
            return Result<string>.Failure("School not found.", "SCHOOL_NOT_FOUND");

        var ticket = await _db.SupportTickets
            .FirstOrDefaultAsync(c => c.Id == command.ComplaintId && c.SchoolID == schoolMgr.SchoolID, ct);

        if (ticket == null)
            return Result<string>.Failure("SupportTicket not found.", "COMPLAINT_NOT_FOUND");

        if (ticket.Status != SupportTicketStatus.Resolved)
            return Result<string>.Failure("Only resolved complaints can be closed.", "INVALID_STATUS");

        ticket.Status = SupportTicketStatus.Closed;
        await _db.SaveChangesAsync(ct);

        return Result<string>.Success("SupportTicket closed successfully.");
    }
}
