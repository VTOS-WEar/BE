using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using VTOS.Application.Abstractions;
using VTOS.Application.Common;
using VTOS.Application.Common.Models;
using VTOS.Application.Common.Models.PayOSDTOs;
using VTOS.Application.Features.Schools.Commands;
using VTOS.Domain.Entities;

namespace VTOS.Application.Features.Admin.Commands;

public class ApproveWithdrawalCommandHandler : IApproveWithdrawalCommandHandler
{
    private readonly IApplicationDbContext _db;
    private readonly IPayOSService _payOSService;
    private readonly ILogger<ApproveWithdrawalCommandHandler> _logger;

    public ApproveWithdrawalCommandHandler(
        IApplicationDbContext db,
        IPayOSService payOSService,
        ILogger<ApproveWithdrawalCommandHandler> logger)
    {
        _db = db;
        _payOSService = payOSService;
        _logger = logger;
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
            return Result<WithdrawalRequestResponse>.Failure("Insufficient school wallet balance.", "INSUFFICIENT_BALANCE");

        // Step 3: Validate school bank account is configured
        if (string.IsNullOrWhiteSpace(wallet.BankCode) || string.IsNullOrWhiteSpace(wallet.BankAccountNumber))
            return Result<WithdrawalRequestResponse>.Failure("School bank account is not configured.", "BANK_NOT_CONFIGURED");

        // Step 4: Call PayOS to payout to school's bank account
        var payoutRequest = new CreatePayoutRequest
        {
            ReferenceId = $"WITHDRAW-{withdrawal.Id}",
            Amount = (long)withdrawal.Amount,
            Description = $"Withdrawal for {wallet.Id.ToString()[..5]}",
            ToBin = wallet.BankCode!,
            ToAccountNumber = wallet.BankAccountNumber!,
            Category = new List<string> { "WITHDRAWAL" }
        };

        var payoutResult = await _payOSService.CreatePayoutAsync(payoutRequest, ct);

        if (payoutResult == null || string.IsNullOrEmpty(payoutResult.Id))
            return Result<WithdrawalRequestResponse>.Failure("Payout to school bank account failed.", "PAYOUT_FAILED");

        _logger.LogInformation(
            "PayOS payout created for withdrawal: PayoutId={PayoutId}, WithdrawalId={WithdrawalId}, Amount={Amount}",
            payoutResult.Id, withdrawal.Id, withdrawal.Amount);

        // Step 5: Deduct wallet balance
        wallet.Balance -= withdrawal.Amount;
        wallet.UpdatedAt = DateTime.UtcNow;

        // Step 6: Update withdrawal request status
        withdrawal.Status = "Paid";
        withdrawal.ApprovedAt = DateTime.UtcNow;
        withdrawal.PaidAt = DateTime.UtcNow;
        withdrawal.AdminNote = command.AdminNote;

        // Step 7: Save all changes
        await _db.SaveChangesAsync(ct);

        return Result<WithdrawalRequestResponse>.Success(new WithdrawalRequestResponse
        {
            WithdrawalRequestId = withdrawal.Id,
            WalletId = wallet.Id,
            Amount = withdrawal.Amount,
            Status = withdrawal.Status,
            RequestedAt = withdrawal.RequestedAt,
            ApprovedAt = withdrawal.ApprovedAt,
            PaidAt = withdrawal.PaidAt,
            AdminNote = withdrawal.AdminNote
        });
    }
}
