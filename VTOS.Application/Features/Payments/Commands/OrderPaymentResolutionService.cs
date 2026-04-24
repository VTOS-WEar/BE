using Microsoft.EntityFrameworkCore;
using VTOS.Application.Abstractions;
using VTOS.Application.Common;
using VTOS.Domain.Entities;
using VTOS.Domain.Enums;

namespace VTOS.Application.Features.Payments.Commands;

public interface IOrderPaymentResolutionService
{
    Task<Result<(Guid ProviderId, string ProviderName)>> ResolveProviderAsync(Order order, CancellationToken ct = default);
    Task<Wallet> GetOrCreateProviderWalletAsync(Guid providerId, DateTime nowUtc, CancellationToken ct = default);
}

public class OrderPaymentResolutionService : IOrderPaymentResolutionService
{
    private readonly IApplicationDbContext _db;

    public OrderPaymentResolutionService(IApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<Result<(Guid ProviderId, string ProviderName)>> ResolveProviderAsync(Order order, CancellationToken ct = default)
    {
        if (order.ProviderID.HasValue)
        {
            var provider = await _db.Providers
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.Id == order.ProviderID.Value && !p.IsDeleted, ct);

            if (provider != null)
                return Result<(Guid, string)>.Success((provider.Id, provider.ProviderName));
        }

        return Result<(Guid, string)>.Failure(
            "Provider identity was not recorded on this order.",
            "PROVIDER_NOT_FOUND");
    }

    public async Task<Wallet> GetOrCreateProviderWalletAsync(Guid providerId, DateTime nowUtc, CancellationToken ct = default)
    {
        var wallet = await _db.Wallets
            .FirstOrDefaultAsync(w =>
                w.OwnerID == providerId &&
                w.OwnerType == WalletOwnerType.Provider &&
                w.IsActive, ct);

        if (wallet != null)
            return wallet;

        wallet = new Wallet
        {
            Id = Guid.NewGuid(),
            OwnerID = providerId,
            OwnerType = WalletOwnerType.Provider,
            Balance = 0,
            IsActive = true,
            CreatedAt = nowUtc,
            UpdatedAt = nowUtc
        };
        _db.Wallets.Add(wallet);
        return wallet;
    }
}
