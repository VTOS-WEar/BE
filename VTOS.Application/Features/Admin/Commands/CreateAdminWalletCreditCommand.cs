using Microsoft.EntityFrameworkCore;
using VTOS.Application.Abstractions;
using VTOS.Application.Common;
using VTOS.Application.Features.Notifications;
using VTOS.Domain.Entities;
using VTOS.Domain.Enums;

namespace VTOS.Application.Features.Admin.Commands;

public record CreateAdminWalletCreditCommand(
    Guid AdminUserId,
    Guid OwnerId,
    WalletOwnerType OwnerType,
    decimal Amount,
    string Reason,
    Guid? TicketId = null,
    Guid? OrderId = null);

public record AdminWalletCreditResponse(
    Guid TransactionId,
    Guid WalletId,
    Guid OwnerId,
    string OwnerType,
    decimal PreviousBalance,
    decimal CreditedAmount,
    decimal NewBalance,
    string Description,
    DateTime CreatedAt);

public interface ICreateAdminWalletCreditCommandHandler
{
    Task<Result<AdminWalletCreditResponse>> HandleAsync(CreateAdminWalletCreditCommand command, CancellationToken ct = default);
}

public class CreateAdminWalletCreditCommandHandler : ICreateAdminWalletCreditCommandHandler
{
    private readonly IApplicationDbContext _db;
    private readonly INotificationService _notificationService;

    public CreateAdminWalletCreditCommandHandler(IApplicationDbContext db, INotificationService notificationService)
    {
        _db = db;
        _notificationService = notificationService;
    }

    public async Task<Result<AdminWalletCreditResponse>> HandleAsync(CreateAdminWalletCreditCommand command, CancellationToken ct = default)
    {
        var admin = await _db.Users
            .AsNoTracking()
            .Include(u => u.Role)
            .FirstOrDefaultAsync(u => u.Id == command.AdminUserId && u.Role.RoleName == "Admin" && u.IsActive && !u.IsDeleted, ct);

        if (admin == null)
            return Result<AdminWalletCreditResponse>.Failure("Only active admins can credit wallets manually.", "FORBIDDEN");

        if (command.OwnerType != WalletOwnerType.Parent && command.OwnerType != WalletOwnerType.Provider)
            return Result<AdminWalletCreditResponse>.Failure("Only Parent and Provider wallets can be credited manually.", "INVALID_OWNER_TYPE");

        if (command.Amount <= 0)
            return Result<AdminWalletCreditResponse>.Failure("Credit amount must be greater than zero.", "INVALID_AMOUNT");

        var reason = command.Reason.Trim();
        if (string.IsNullOrWhiteSpace(reason))
            return Result<AdminWalletCreditResponse>.Failure("Admin reason is required.", "REASON_REQUIRED");

        if (reason.Length > 1000)
            return Result<AdminWalletCreditResponse>.Failure("Admin reason must be 1000 characters or shorter.", "REASON_TOO_LONG");

        var owner = await ResolveOwnerAsync(command.OwnerId, command.OwnerType, ct);
        if (!owner.IsSuccess)
            return Result<AdminWalletCreditResponse>.Failure(owner.Error!, owner.ErrorCode);

        if (command.TicketId.HasValue)
        {
            var ticketExists = await _db.SupportTickets.AsNoTracking().AnyAsync(t => t.Id == command.TicketId.Value, ct);
            if (!ticketExists)
                return Result<AdminWalletCreditResponse>.Failure("Linked support ticket was not found.", "TICKET_NOT_FOUND");
        }

        if (command.OrderId.HasValue)
        {
            var orderExists = await _db.Orders.AsNoTracking().AnyAsync(o => o.Id == command.OrderId.Value, ct);
            if (!orderExists)
                return Result<AdminWalletCreditResponse>.Failure("Linked order was not found.", "ORDER_NOT_FOUND");
        }

        var now = DateTime.UtcNow;
        var wallet = await _db.Wallets
            .FirstOrDefaultAsync(w => w.OwnerID == command.OwnerId && w.OwnerType == command.OwnerType, ct);

        if (wallet == null)
        {
            wallet = new Wallet
            {
                Id = Guid.NewGuid(),
                OwnerID = command.OwnerId,
                OwnerType = command.OwnerType,
                Balance = 0,
                IsActive = true,
                CreatedAt = now,
                UpdatedAt = now
            };
            _db.Wallets.Add(wallet);
        }
        else if (!wallet.IsActive)
        {
            wallet.IsActive = true;
        }

        var previousBalance = wallet.Balance;
        wallet.Balance += command.Amount;
        wallet.UpdatedAt = now;

        var ownerLabel = owner.Value!.OwnerName;
        var description = $"Admin manual wallet credit for {ownerLabel}";
        var transactionLog = BuildTransactionLog(admin, reason, command.TicketId, command.OrderId);

        var transaction = new PaymentTransaction
        {
            Id = Guid.NewGuid(),
            OrderID = command.OrderId,
            WalletID = wallet.Id,
            TransactionType = TransactionType.ManualWalletCredit,
            GatewayType = PaymentGatewayType.Other,
            TransactionStatus = PaymentStatus.Completed,
            Amount = command.Amount,
            TransactionTimestamp = now,
            Description = description.Length > 500 ? description[..500] : description,
            TransactionLog = transactionLog,
            CreatedAt = now,
            CreatedBy = admin.Email
        };

        _db.PaymentTransactions.Add(transaction);
        await _db.SaveChangesAsync(ct);

        await NotifyOwnerAsync(command.OwnerType, command.OwnerId, command.Amount, reason, ct);

        return Result<AdminWalletCreditResponse>.Success(new AdminWalletCreditResponse(
            transaction.Id,
            wallet.Id,
            command.OwnerId,
            command.OwnerType.ToString(),
            previousBalance,
            command.Amount,
            wallet.Balance,
            transaction.Description,
            now));
    }

    private async Task<Result<OwnerContext>> ResolveOwnerAsync(Guid ownerId, WalletOwnerType ownerType, CancellationToken ct)
    {
        if (ownerType == WalletOwnerType.Parent)
        {
            var user = await _db.Users
                .AsNoTracking()
                .Include(u => u.Role)
                .FirstOrDefaultAsync(u => u.Id == ownerId && u.Role.RoleName == "Parent" && u.IsActive && !u.IsDeleted, ct);

            return user == null
                ? Result<OwnerContext>.Failure("Parent account was not found or is inactive.", "OWNER_NOT_FOUND")
                : Result<OwnerContext>.Success(new OwnerContext(string.IsNullOrWhiteSpace(user.FullName) ? user.Email : user.FullName));
        }

        var provider = await _db.Providers
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == ownerId && !p.IsDeleted && p.Status == ProviderStatus.Active, ct);

        return provider == null
            ? Result<OwnerContext>.Failure("Provider was not found or is inactive.", "OWNER_NOT_FOUND")
            : Result<OwnerContext>.Success(new OwnerContext(provider.ProviderName));
    }

    private static string BuildTransactionLog(User admin, string reason, Guid? ticketId, Guid? orderId)
    {
        var parts = new List<string>
        {
            $"Admin: {admin.Email}",
            $"Reason: {reason}"
        };

        if (ticketId.HasValue)
            parts.Add($"TicketId: {ticketId.Value}");

        if (orderId.HasValue)
            parts.Add($"OrderId: {orderId.Value}");

        return string.Join(" | ", parts);
    }

    private async Task NotifyOwnerAsync(WalletOwnerType ownerType, Guid ownerId, decimal amount, string reason, CancellationToken ct)
    {
        try
        {
            if (ownerType == WalletOwnerType.Provider)
            {
                await _notificationService.NotifyProviderAsync(
                    ownerId,
                    "Admin đã nạp tiền vào ví",
                    $"Ví NCC được cộng {amount:N0}đ. Lý do: {reason}",
                    "Payment",
                    ownerId,
                    "Wallet",
                    "/provider/wallet",
                    ct);
            }
            else if (ownerType == WalletOwnerType.Parent)
            {
                await _notificationService.CreateAsync(
                    ownerId,
                    "Admin đã nạp tiền vào ví",
                    $"Ví hoàn tiền được cộng {amount:N0}đ. Lý do: {reason}",
                    "Payment",
                    ownerId,
                    "Wallet",
                    "/parentprofile/wallet",
                    ct);
            }
        }
        catch
        {
            // Wallet credit must not fail because notification delivery failed.
        }
    }

    private sealed record OwnerContext(string OwnerName);
}
