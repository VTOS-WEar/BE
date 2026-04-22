using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using VTOS.Application.Abstractions;
using VTOS.Application.Common;
using VTOS.Application.Common.Models;
using VTOS.Domain.Entities;
using VTOS.Domain.Enums;

namespace VTOS.Application.Features.Schools.Commands;

public class ApproveRefundCommandHandler : IApproveRefundCommandHandler
{
    private readonly IApplicationDbContext _db;
    private readonly IPayOSService _payOSService;
    private readonly ILogger<ApproveRefundCommandHandler> _logger;

    public ApproveRefundCommandHandler(
        IApplicationDbContext db,
        IPayOSService payOSService,
        ILogger<ApproveRefundCommandHandler> logger)
    {
        _db = db;
        _payOSService = payOSService;
        _logger = logger;
    }

    public async Task<Result<RefundResponse>> HandleAsync(ApproveRefundCommand command, CancellationToken ct = default)
    {
        try
        {
            // Step 1: Get current user with Role and validate School role
            var schoolUser = await _db.Users
                .AsNoTracking()
                .Include(u => u.Role)
                .FirstOrDefaultAsync(u => u.Id == command.SchoolUserId, ct);

            if (schoolUser == null)
                return Result<RefundResponse>.Failure("User not found.", "USER_NOT_FOUND");

            if (schoolUser.Role?.RoleName != "School")
                return Result<RefundResponse>.Failure("Only school managers can approve refunds.", "FORBIDDEN");

            var schoolMgr = await _db.SchoolManagers.AsNoTracking().FirstOrDefaultAsync(m => m.UserID == schoolUser.Id, ct);
            if (schoolMgr == null)
                return Result<RefundResponse>.Failure("User is not assigned to any school.", "SCHOOL_NOT_FOUND");

            var schoolId = schoolMgr.SchoolID;

            // Step 2: Load refund with related payment → order → childProfile
            var refund = await _db.Refunds
                .Include(r => r.PaymentTransaction)
                    .ThenInclude(pt => pt.Order)
                        .ThenInclude(o => o.ChildProfile)
                .FirstOrDefaultAsync(r => r.Id == command.RefundId, ct);

            if (refund == null)
                return Result<RefundResponse>.Failure("Refund not found.", "REFUND_NOT_FOUND");

            if (refund.RefundStatus != RefundStatus.Pending)
                return Result<RefundResponse>.Failure($"Refund cannot be approved. Current status: {refund.RefundStatus}", "REFUND_NOT_APPROVABLE");

            // Step 3: Validate that this refund belongs to the current user's school
            var order = refund.PaymentTransaction.Order;
            if (order.ChildProfile.SchoolID != schoolId)
                return Result<RefundResponse>.Failure("This refund does not belong to your school.", "UNAUTHORIZED_REFUND_ACCESS");

            // Step 4: Load school wallet and check balance
            var wallet = await _db.Set<Wallet>()
                .FirstOrDefaultAsync(w => w.OwnerID == schoolId && w.OwnerType == WalletOwnerType.School && w.IsActive, ct);

            if (wallet == null)
                return Result<RefundResponse>.Failure("School wallet not found or inactive.", "WALLET_NOT_FOUND");

            if (wallet.Balance < refund.RefundAmount)
                return Result<RefundResponse>.Failure("Insufficient school wallet balance for refund.", "INSUFFICIENT_BALANCE");

            // Step 5: Get parent's default bank account for payout
            var parentId = order.ChildProfile.ParentUserID;
            var parentBank = await _db.ParentBankAccounts
                .FirstOrDefaultAsync(b => b.ParentUserID == parentId && b.IsDefault, ct);

            if (parentBank == null)
                return Result<RefundResponse>.Failure("Parent does not have a default bank account configured.", "PARENT_BANK_NOT_FOUND");

            if (string.IsNullOrWhiteSpace(parentBank.BankCode))
                return Result<RefundResponse>.Failure("Parent bank account is missing bank code.", "PARENT_BANK_CODE_MISSING");

            // Step 6: Call PayOS to perform actual payout to parent's bank account
            var payoutRequest = new CreatePayoutRequest
            {
                ReferenceId = $"REFUND-{refund.Id}",
                Amount = (long)refund.RefundAmount,
                Description = $"Refund for order {order.Id.ToString().Substring(0,5)}",
                ToBin = parentBank.BankCode,
                ToAccountNumber = parentBank.AccountNumber,
                Category = new List<string>() { "REFUND"}
            };

            var payoutResult = await _payOSService.CreatePayoutAsync(payoutRequest, ct);

            if (payoutResult == null || string.IsNullOrEmpty(payoutResult.Id))
                return Result<RefundResponse>.Failure("Payout to parent bank account failed.", "PAYOUT_FAILED");

            _logger.LogInformation(
                "PayOS payout created: PayoutId={PayoutId}, RefundId={RefundId}, Amount={Amount}",
                payoutResult.Id, refund.Id, refund.RefundAmount);

            // Step 7: Deduct school wallet balance
            wallet.Balance -= refund.RefundAmount;
            wallet.UpdatedAt = DateTime.UtcNow;

            // Step 8: Update refund status to Completed
            refund.RefundStatus = RefundStatus.Completed;
            refund.UpdatedAt = DateTime.UtcNow;
            refund.PaymentTransaction.EscrowStatus = EscrowStatus.Refunded;
            refund.PaymentTransaction.UpdatedAt = DateTime.UtcNow;

            // Step 9: Update order status to Refunded
            order.OrderStatus = OrderStatus.Refunded;
            order.UpdatedAt = DateTime.UtcNow;

            // Step 10: Save all changes
            await _db.SaveChangesAsync(ct);

            _logger.LogInformation(
                "Refund approved: RefundId={RefundId}, Amount={Amount}, Wallet={WalletId}, PayoutId={PayoutId}, PayoutTo={BankAccount}",
                refund.Id, refund.RefundAmount, wallet.Id, payoutResult.Id, parentBank.AccountNumber);

            return Result<RefundResponse>.Success(new RefundResponse
            {
                RefundId = refund.Id,
                OrderId = order.Id,
                PaymentTransactionId = refund.PaymentID,
                RefundAmount = refund.RefundAmount,
                RefundStatus = refund.RefundStatus.ToString(),
                DisputeReason = refund.DisputeReason,
                CreatedAt = refund.CreatedAt,
                UpdatedAt = refund.UpdatedAt ?? refund.CreatedAt
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error approving refund {RefundId}", command.RefundId);
            return Result<RefundResponse>.Failure($"Failed to approve refund: {ex.Message}", "APPROVE_REFUND_ERROR");
        }
    }
}
