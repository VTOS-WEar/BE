using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using VTOS.Application.Abstractions;
using VTOS.Application.Common;
using VTOS.Application.Features.Schools.Commands;
using VTOS.Domain.Entities;

namespace VTOS.Application.Features.Admin.Commands;

public class RejectWithdrawalCommandHandler : IRejectWithdrawalCommandHandler
{
    private readonly IApplicationDbContext _db;
    private readonly ILogger<RejectWithdrawalCommandHandler> _logger;

    public RejectWithdrawalCommandHandler(
        IApplicationDbContext db,
        ILogger<RejectWithdrawalCommandHandler> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<Result<WithdrawalRequestResponse>> HandleAsync(RejectWithdrawalCommand command, CancellationToken ct = default)
    {
        var withdrawal = await _db.Set<WalletWithdrawalRequest>()
            .Include(w => w.Wallet)
            .FirstOrDefaultAsync(w => w.Id == command.WithdrawalRequestId, ct);

        if (withdrawal == null)
            return Result<WithdrawalRequestResponse>.Failure("Withdrawal request not found.", "WITHDRAWAL_NOT_FOUND");

        if (withdrawal.Status != "Pending")
            return Result<WithdrawalRequestResponse>.Failure($"Withdrawal request cannot be rejected. Current status: {withdrawal.Status}", "WITHDRAWAL_NOT_REJECTABLE");

        withdrawal.Status = "Rejected";
        withdrawal.AdminNote = command.AdminNote;
        withdrawal.ApprovedAt = DateTime.UtcNow; // reuse as "processed at"

        await _db.SaveChangesAsync(ct);

        _logger.LogInformation(
            "Withdrawal request rejected: WithdrawalId={WithdrawalId}, AdminNote={AdminNote}",
            withdrawal.Id, command.AdminNote);

        return Result<WithdrawalRequestResponse>.Success(new WithdrawalRequestResponse
        {
            WithdrawalRequestId = withdrawal.Id,
            WalletId = withdrawal.WalletID,
            Amount = withdrawal.Amount,
            Status = withdrawal.Status,
            RequestedAt = withdrawal.RequestedAt,
            ApprovedAt = withdrawal.ApprovedAt,
            AdminNote = withdrawal.AdminNote
        });
    }
}
