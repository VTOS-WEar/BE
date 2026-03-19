using Microsoft.EntityFrameworkCore;
using VTOS.Application.Abstractions;
using VTOS.Application.Common;

namespace VTOS.Application.Features.Payments.Queries;

// ── GetProviderWalletQuery ──────────────────────────────────────────
public record GetProviderWalletQuery(Guid UserId);

public interface IGetProviderWalletQueryHandler
{
    Task<Result<WalletDto>> HandleAsync(GetProviderWalletQuery query, CancellationToken ct = default);
}

public class GetProviderWalletQueryHandler : IGetProviderWalletQueryHandler
{
    private readonly IApplicationDbContext _db;

    public GetProviderWalletQueryHandler(IApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<Result<WalletDto>> HandleAsync(GetProviderWalletQuery query, CancellationToken ct = default)
    {
        var user = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == query.UserId, ct);
        if (user == null || user.ProviderID == null)
            return Result<WalletDto>.Failure("Access denied.", "ACCESS_DENIED");

        var wallet = await _db.Wallets.AsNoTracking()
            .FirstOrDefaultAsync(w => w.OwnerID == user.ProviderID && w.OwnerType == Domain.Enums.WalletOwnerType.Provider && w.IsActive, ct);

        if (wallet == null)
        {
            // Auto-create wallet for providers that were approved before wallet auto-creation was added
            wallet = new Domain.Entities.Wallet
            {
                Id = Guid.NewGuid(),
                OwnerID = user.ProviderID.Value,
                OwnerType = Domain.Enums.WalletOwnerType.Provider,
                Balance = 0,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            _db.Wallets.Add(wallet);
            await _db.SaveChangesAsync(ct);
        }

        return Result<WalletDto>.Success(new WalletDto(
            wallet.Id, wallet.Balance,
            wallet.BankCode, wallet.BankName,
            wallet.BankAccountNumber, wallet.BankAccountName,
            wallet.IsActive, wallet.UpdatedAt));
    }
}
