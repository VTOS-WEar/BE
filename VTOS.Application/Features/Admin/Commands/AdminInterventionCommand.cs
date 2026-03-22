using Microsoft.EntityFrameworkCore;
using VTOS.Application.Abstractions;
using VTOS.Application.Common;
using VTOS.Domain.Enums;

namespace VTOS.Application.Features.Admin.Commands;

// ── Command ──

public record AdminInterventionCommand(Guid ComplaintId, string Note, string? Action);

// ── Interface ──

public interface IAdminInterventionCommandHandler
{
    Task<Result<string>> HandleAsync(AdminInterventionCommand command, CancellationToken ct = default);
}

// ── Handler ──

public class AdminInterventionCommandHandler : IAdminInterventionCommandHandler
{
    private readonly IApplicationDbContext _context;

    public AdminInterventionCommandHandler(IApplicationDbContext context) => _context = context;

    public async Task<Result<string>> HandleAsync(AdminInterventionCommand command, CancellationToken ct = default)
    {
        var complaint = await _context.Complaints
            .FirstOrDefaultAsync(c => c.Id == command.ComplaintId, ct);

        if (complaint == null)
            return Result<string>.Failure("Complaint not found.", "NOT_FOUND");

        // Admin can add note (append to response), change status
        var adminNote = $"[Admin - {DateTime.UtcNow:dd/MM/yyyy HH:mm}] {command.Note}";
        complaint.Response = string.IsNullOrEmpty(complaint.Response)
            ? adminNote
            : complaint.Response + "\n\n" + adminNote;

        if (!string.IsNullOrEmpty(command.Action))
        {
            switch (command.Action.ToLower())
            {
                case "escalate":
                    complaint.Status = ComplaintStatus.InProgress;
                    break;
                case "resolve":
                    complaint.Status = ComplaintStatus.Resolved;
                    complaint.ResolvedAt = DateTime.UtcNow;
                    break;
                case "close":
                    complaint.Status = ComplaintStatus.Closed;
                    complaint.ResolvedAt ??= DateTime.UtcNow;
                    break;
            }
        }

        complaint.RespondedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync(ct);

        return Result<string>.Success($"Intervention added. Status: {complaint.Status}");
    }
}
