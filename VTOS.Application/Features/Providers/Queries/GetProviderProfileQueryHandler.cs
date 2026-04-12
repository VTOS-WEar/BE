using Microsoft.EntityFrameworkCore;
using VTOS.Application.Abstractions;
using VTOS.Application.Common;
using VTOS.Application.Features.Providers.DTOs;

namespace VTOS.Application.Features.Providers.Queries;

public class GetProviderProfileQueryHandler : IGetProviderProfileQueryHandler
{
    private readonly IApplicationDbContext _context;

    public GetProviderProfileQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result<ProviderProfileDto>> HandleAsync(
        GetProviderProfileQuery query, CancellationToken ct = default)
    {
        var user = await _context.Users
            .Include(u => u.ProviderManager)
                .ThenInclude(pm => pm!.Provider)
            .FirstOrDefaultAsync(u => u.Id == query.UserId, ct);

        if (user == null)
            return Result<ProviderProfileDto>.Failure("User not found.", "USER_NOT_FOUND");

        if (user.ProviderManager?.Provider == null)
            return Result<ProviderProfileDto>.Failure("No provider linked to this user.", "NO_PROVIDER");

        var p = user.ProviderManager?.Provider;
        return Result<ProviderProfileDto>.Success(new ProviderProfileDto(
            p.Id,
            p.ProviderName,
            p.ContactPersonName,
            p.Phone,
            p.Email,
            p.Address,
            p.Status.ToString(),
            user.IsTwoFactorEnabled
        ));
    }
}
