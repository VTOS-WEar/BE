using Microsoft.EntityFrameworkCore;
using VTOS.Application.Abstractions;
using VTOS.Application.Common;

namespace VTOS.Application.Features.Schools.Queries;

/// <summary>
/// DTO for provider listing.
/// </summary>
public record ProviderDto(
    Guid Id,
    string ProviderName,
    string? ContactPersonName,
    string? Phone,
    string? Email,
    string? Address,
    string? Status
);

/// <summary>
/// Query to list all active providers.
/// </summary>
public record GetProvidersQuery(Guid UserId);

public interface IGetProvidersQueryHandler
{
    Task<Result<IReadOnlyList<ProviderDto>>> HandleAsync(GetProvidersQuery query, CancellationToken ct = default);
}

public class GetProvidersQueryHandler : IGetProvidersQueryHandler
{
    private readonly IApplicationDbContext _db;

    public GetProvidersQueryHandler(IApplicationDbContext db) => _db = db;

    public async Task<Result<IReadOnlyList<ProviderDto>>> HandleAsync(GetProvidersQuery query, CancellationToken ct = default)
    {
        // Verify user has a school
        var user = await _db.Users.AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == query.UserId, ct);

        if (user?.SchoolID == null)
            return Result<IReadOnlyList<ProviderDto>>.Failure("School not found.", "SCHOOL_NOT_FOUND");

        var providers = await _db.Providers
            .AsNoTracking()
            .Where(p => !p.IsDeleted)
            .OrderBy(p => p.ProviderName)
            .Select(p => new ProviderDto(
                p.Id,
                p.ProviderName,
                p.ContactPersonName,
                p.Phone,
                p.Email,
                p.Address,
                p.Status.ToString()
            ))
            .ToListAsync(ct);

        return Result<IReadOnlyList<ProviderDto>>.Success(providers);
    }
}
