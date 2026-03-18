using Microsoft.EntityFrameworkCore;
using VTOS.Application.Abstractions;
using VTOS.Application.Common;

namespace VTOS.Application.Features.Payments.Queries;

// ── GetSchoolWalletQuery ────────────────────────────────────────────
public record GetSchoolWalletQuery(Guid UserId);

public record WalletDto(
    Guid WalletId,
    decimal Balance,
    string? BankCode,
    string? BankName,
    string? BankAccountNumber,
    string? BankAccountName,
    bool IsActive,
    DateTime UpdatedAt
);

public interface IGetSchoolWalletQueryHandler
{
    Task<Result<WalletDto>> HandleAsync(GetSchoolWalletQuery query, CancellationToken ct = default);
}

public class GetSchoolWalletQueryHandler : IGetSchoolWalletQueryHandler
{
    private readonly IApplicationDbContext _db;

    public GetSchoolWalletQueryHandler(IApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<Result<WalletDto>> HandleAsync(GetSchoolWalletQuery query, CancellationToken ct = default)
    {
        var user = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == query.UserId, ct);
        if (user == null || user.SchoolID == null)
            return Result<WalletDto>.Failure("Access denied.", "ACCESS_DENIED");

        var wallet = await _db.Wallets.AsNoTracking()
            .FirstOrDefaultAsync(w => w.OwnerID == user.SchoolID && w.OwnerType == Domain.Enums.WalletOwnerType.School && w.IsActive, ct);

        if (wallet == null)
        {
            // Return empty wallet - it will be auto-created on first payment
            return Result<WalletDto>.Success(new WalletDto(
                Guid.Empty, 0, null, null, null, null, false, DateTime.UtcNow));
        }

        return Result<WalletDto>.Success(new WalletDto(
            wallet.Id, wallet.Balance,
            wallet.BankCode, wallet.BankName,
            wallet.BankAccountNumber, wallet.BankAccountName,
            wallet.IsActive, wallet.UpdatedAt));
    }
}
