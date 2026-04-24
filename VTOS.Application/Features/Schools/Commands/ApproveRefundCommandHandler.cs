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
    private readonly ILogger<ApproveRefundCommandHandler> _logger;

    public ApproveRefundCommandHandler(
        IApplicationDbContext db,
        ILogger<ApproveRefundCommandHandler> logger)
    {
        _db = db;
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
                    .ThenInclude(pt => pt.Order!)
                        .ThenInclude(o => o.ChildProfile)
                .FirstOrDefaultAsync(r => r.Id == command.RefundId, ct);

            if (refund == null)
                return Result<RefundResponse>.Failure("Refund not found.", "REFUND_NOT_FOUND");

            if (refund.RefundStatus != RefundStatus.Pending)
                return Result<RefundResponse>.Failure($"Refund cannot be approved. Current status: {refund.RefundStatus}", "REFUND_NOT_APPROVABLE");

            // Step 3: Validate that this refund belongs to the current user's school
            var paymentTransaction = refund.PaymentTransaction;
            var order = paymentTransaction?.Order;
            var childProfile = order?.ChildProfile;
            if (order == null || childProfile == null)
                return Result<RefundResponse>.Failure("Refund is missing order context.", "ORDER_NOT_FOUND");

            if (childProfile.SchoolID != schoolId)
                return Result<RefundResponse>.Failure("This refund does not belong to your school.", "UNAUTHORIZED_REFUND_ACCESS");

            // Step 4: Resolve the parent wallet that will receive refund credit.
            var parentId = childProfile.ParentUserID;
            if (!parentId.HasValue)
                return Result<RefundResponse>.Failure("Order is not linked to a parent account.", "PARENT_NOT_FOUND");

            var parentWallet = await _db.Set<Wallet>()
                .FirstOrDefaultAsync(w => w.OwnerID == parentId.Value && w.OwnerType == WalletOwnerType.Parent && w.IsActive, ct);

            if (parentWallet == null)
            {
                parentWallet = new Wallet
                {
                    Id = Guid.NewGuid(),
                    OwnerID = parentId.Value,
                    OwnerType = WalletOwnerType.Parent,
                    Balance = 0,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };
                _db.Set<Wallet>().Add(parentWallet);
            }

            // Step 5: Credit parent wallet from system escrow.
            parentWallet.Balance += refund.RefundAmount;
            parentWallet.UpdatedAt = DateTime.UtcNow;

            var parentRefundTransaction = new PaymentTransaction
            {
                Id = Guid.NewGuid(),
                OrderID = order.Id,
                WalletID = parentWallet.Id,
                TransactionType = TransactionType.Refund,
                GatewayType = PaymentGatewayType.Other,
                TransactionStatus = PaymentStatus.Completed,
                EscrowStatus = EscrowStatus.Refunded,
                Amount = refund.RefundAmount,
                TransactionTimestamp = DateTime.UtcNow,
                Description = $"Refund credited for order {order.Id.ToString()[..5]}"
            };
            _db.PaymentTransactions.Add(parentRefundTransaction);

            // Step 6: Update refund status to Completed
            refund.RefundStatus = RefundStatus.Completed;
            refund.UpdatedAt = DateTime.UtcNow;
            refund.PaymentTransaction.EscrowStatus = EscrowStatus.Refunded;
            refund.PaymentTransaction.UpdatedAt = DateTime.UtcNow;

            // Step 7: Update order status to Refunded
            order.OrderStatus = OrderStatus.Refunded;
            order.UpdatedAt = DateTime.UtcNow;

            // Step 8: Save all changes
            await _db.SaveChangesAsync(ct);

            _logger.LogInformation(
                "Refund approved and credited to parent wallet: RefundId={RefundId}, Amount={Amount}, ParentWallet={ParentWalletId}",
                refund.Id, refund.RefundAmount, parentWallet.Id);

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
