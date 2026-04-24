using Microsoft.EntityFrameworkCore;
using VTOS.Application.Abstractions;
using VTOS.Application.Common;

namespace VTOS.Application.Features.Payments.Commands;

// ── UpdateWalletBankInfoCommand ─────────────────────────────────────
public record UpdateWalletBankInfoCommand(Guid UserId, string BankCode, string BankName, string AccountNumber, string AccountName);

public record UpdateWalletBankInfoResponse(bool Success);

public interface IUpdateWalletBankInfoCommandHandler
{
    Task<Result<UpdateWalletBankInfoResponse>> HandleAsync(UpdateWalletBankInfoCommand command, CancellationToken ct = default);
}

public class UpdateWalletBankInfoCommandHandler : IUpdateWalletBankInfoCommandHandler
{
    private readonly IApplicationDbContext _db;

    public UpdateWalletBankInfoCommandHandler(IApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<Result<UpdateWalletBankInfoResponse>> HandleAsync(UpdateWalletBankInfoCommand command, CancellationToken ct = default)
    {
        var user = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == command.UserId, ct);
        if (user == null)
            return Result<UpdateWalletBankInfoResponse>.Failure("Access denied.", "ACCESS_DENIED");

        var providerMgr = await _db.ProviderManagers.AsNoTracking().FirstOrDefaultAsync(m => m.UserID == user.Id, ct);


        // Determine wallet owner: Provider only. Parent bank accounts are stored separately.
        Guid? ownerId = null;
        Domain.Enums.WalletOwnerType ownerType;
        if (providerMgr != null)
        {
            ownerId = providerMgr.ProviderID;
            ownerType = Domain.Enums.WalletOwnerType.Provider;
        }
        else
        {
            return Result<UpdateWalletBankInfoResponse>.Failure("Access denied.", "ACCESS_DENIED");
        }

        if (!ownerId.HasValue)
            return Result<UpdateWalletBankInfoResponse>.Failure("Wallet owner could not be resolved.", "ACCESS_DENIED");

        var wallet = await _db.Wallets.FirstOrDefaultAsync(w => w.OwnerID == ownerId && w.OwnerType == ownerType && w.IsActive, ct);
        if (wallet == null)
        {
            wallet = new Domain.Entities.Wallet
            {
                Id = Guid.NewGuid(),
                OwnerID = ownerId.Value,
                OwnerType = ownerType,
                Balance = 0,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            _db.Wallets.Add(wallet);
        }

        wallet.BankCode = command.BankCode;
        wallet.BankName = command.BankName;
        wallet.BankAccountNumber = command.AccountNumber;
        wallet.BankAccountName = command.AccountName;
        wallet.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync(ct);

        return Result<UpdateWalletBankInfoResponse>.Success(new UpdateWalletBankInfoResponse(true));
    }
}
