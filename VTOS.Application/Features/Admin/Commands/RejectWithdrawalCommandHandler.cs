using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using VTOS.Application.Abstractions;
using VTOS.Application.Common;
using VTOS.Application.Features.Notifications;
using VTOS.Application.Features.Schools.Commands;
using VTOS.Domain.Entities;
using VTOS.Domain.Enums;

namespace VTOS.Application.Features.Admin.Commands;

public class RejectWithdrawalCommandHandler : IRejectWithdrawalCommandHandler
{
    private readonly IApplicationDbContext _db;
    private readonly ILogger<RejectWithdrawalCommandHandler> _logger;
    private readonly INotificationService _notificationService;

    public RejectWithdrawalCommandHandler(
        IApplicationDbContext db,
        ILogger<RejectWithdrawalCommandHandler> logger,
        INotificationService notificationService)
    {
        _db = db;
        _logger = logger;
        _notificationService = notificationService;
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
        withdrawal.ApprovedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync(ct);

        _logger.LogInformation(
            "Withdrawal request rejected: WithdrawalId={WithdrawalId}, AdminNote={AdminNote}",
            withdrawal.Id, command.AdminNote);

        try
        {
            var wallet = withdrawal.Wallet;
            var reason = string.IsNullOrEmpty(command.AdminNote) ? "" : $" Reason: {command.AdminNote}";
            if (wallet.OwnerType == WalletOwnerType.Provider)
            {
                await _notificationService.NotifyProviderAsync(
                    wallet.OwnerID,
                    "Withdrawal rejected",
                    $"Withdrawal request for {withdrawal.Amount:N0} VND has been rejected.{reason}",
                    "Withdrawal",
                    withdrawal.Id,
                    "WithdrawalRequest",
                    "/provider/wallet",
                    ct);
            }
            else if (wallet.OwnerType == WalletOwnerType.Parent)
            {
                await _notificationService.CreateAsync(
                    wallet.OwnerID,
                    "Withdrawal rejected",
                    $"Withdrawal request for {withdrawal.Amount:N0} VND has been rejected.{reason}",
                    "Withdrawal",
                    withdrawal.Id,
                    "WithdrawalRequest",
                    "/parentprofile/wallet",
                    ct);
            }
        }
        catch
        {
            // Notification failure should not block rejection.
        }

        return Result<WithdrawalRequestResponse>.Success(new WithdrawalRequestResponse
        {
            WithdrawalRequestId = withdrawal.Id,
            WalletId = withdrawal.WalletID,
            Amount = withdrawal.Amount,
            FeeRate = withdrawal.FeeRate,
            FeeAmount = withdrawal.FeeAmount,
            NetAmount = withdrawal.NetAmount,
            Status = withdrawal.Status,
            RequestedAt = withdrawal.RequestedAt,
            ApprovedAt = withdrawal.ApprovedAt,
            AdminNote = withdrawal.AdminNote
        });
    }
}
