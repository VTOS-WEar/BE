using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using VTOS.Application.Abstractions;
using VTOS.Application.Common;
using VTOS.Application.Features.Notifications;
using VTOS.Application.Features.Schools.Commands;
using VTOS.Domain.Entities;
using VTOS.Domain.Enums;

namespace VTOS.Application.Features.Admin.Commands;

public class ApproveWithdrawalCommandHandler : IApproveWithdrawalCommandHandler
{
    private readonly IApplicationDbContext _db;
    private readonly ILogger<ApproveWithdrawalCommandHandler> _logger;
    private readonly INotificationService _notificationService;

    public ApproveWithdrawalCommandHandler(
        IApplicationDbContext db,
        ILogger<ApproveWithdrawalCommandHandler> logger,
        INotificationService notificationService)
    {
        _db = db;
        _logger = logger;
        _notificationService = notificationService;
    }

    public async Task<Result<WithdrawalRequestResponse>> HandleAsync(ApproveWithdrawalCommand command, CancellationToken ct = default)
    {
        // Step 1: Load withdrawal request with wallet
        var withdrawal = await _db.Set<WalletWithdrawalRequest>()
            .Include(w => w.Wallet)
            .FirstOrDefaultAsync(w => w.Id == command.WithdrawalRequestId, ct);

        if (withdrawal == null)
            return Result<WithdrawalRequestResponse>.Failure("Withdrawal request not found.", "WITHDRAWAL_NOT_FOUND");

        if (withdrawal.Status != "Pending")
            return Result<WithdrawalRequestResponse>.Failure($"Withdrawal request cannot be approved. Current status: {withdrawal.Status}", "WITHDRAWAL_NOT_APPROVABLE");

        var wallet = withdrawal.Wallet;

        // Step 2: Validate wallet balance
        if (wallet.Balance < withdrawal.Amount)
            return Result<WithdrawalRequestResponse>.Failure("Insufficient wallet balance.", "INSUFFICIENT_BALANCE");

        // Step 3: Deduct wallet balance
        wallet.Balance -= withdrawal.Amount;
        wallet.UpdatedAt = DateTime.UtcNow;

        // Step 4: Update withdrawal request status (Admin will transfer manually via banking)
        withdrawal.Status = "Approved";
        withdrawal.ApprovedAt = DateTime.UtcNow;
        withdrawal.AdminNote = command.AdminNote;

        // Step 5: Save all changes
        await _db.SaveChangesAsync(ct);

        _logger.LogInformation(
            "Withdrawal approved: WithdrawalId={WithdrawalId}, Amount={Amount}, WalletId={WalletId}",
            withdrawal.Id, withdrawal.Amount, wallet.Id);

        // Notify wallet owner (School or Provider)
        try
        {
            if (wallet.OwnerType == WalletOwnerType.School)
                await _notificationService.NotifySchoolAsync(wallet.OwnerID,
                    "✅ Rút tiền đã duyệt",
                    $"Yêu cầu rút {withdrawal.Amount:N0}đ đã được duyệt.",
                    "Withdrawal", withdrawal.Id, "WithdrawalRequest",
                    "/school/wallet", ct);
            else
                await _notificationService.NotifyProviderAsync(wallet.OwnerID,
                    "✅ Rút tiền đã duyệt",
                    $"Yêu cầu rút {withdrawal.Amount:N0}đ đã được duyệt.",
                    "Withdrawal", withdrawal.Id, "WithdrawalRequest",
                    "/provider/wallet", ct);
        }
        catch { /* Don't fail */ }

        return Result<WithdrawalRequestResponse>.Success(new WithdrawalRequestResponse
        {
            WithdrawalRequestId = withdrawal.Id,
            WalletId = wallet.Id,
            Amount = withdrawal.Amount,
            Status = withdrawal.Status,
            RequestedAt = withdrawal.RequestedAt,
            ApprovedAt = withdrawal.ApprovedAt,
            AdminNote = withdrawal.AdminNote
        });
    }
}
