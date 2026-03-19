using Microsoft.EntityFrameworkCore;
using VTOS.Application.Abstractions;
using VTOS.Application.Common;
using VTOS.Application.Features.Users.DTOs;

namespace VTOS.Application.Features.Users.Queries;

/// <summary>
/// Handler for user GetProfile query.
/// Checks if user email is verified before allowing GetProfile.
/// </summary>
public class GetProfileQueryHandler : IGetProfileQueryHandler
{
    private readonly IApplicationDbContext _context;

    public GetProfileQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result<GetProfileResponse>> HandleAsync(
        GetProfileQuery query,
        CancellationToken cancellationToken = default)
    {
        // Find user by id
        var user = await _context.Users
            .Include(u => u.Role)
            .Include(u => u.ParentProfile)
            .FirstOrDefaultAsync(u => u.Id == query.Id, cancellationToken);

        if (user == null)
        {
            return Result<GetProfileResponse>.Failure(
                "User not found",
                "USER_NOT_FOUND"
            );
        }

        // Check if user is deleted
        if (user.IsDeleted)
        {
            return Result<GetProfileResponse>.Failure(
                "Account is disabled",
                "ACCOUNT_DISABLED");
        }
        return Result<GetProfileResponse>.Success(new GetProfileResponse(
            user.Id,
            user.Email,
            user.FullName,
            user.Phone ?? string.Empty,
            user.ParentProfile?.DOB ?? DateTime.Now.AddYears(-18),
            (user.ParentProfile?.Gender ?? Domain.Enums.Gender.Other).ToString(),
            user.Role.RoleName,
            user.IsActive,
            user.IsDeleted,
            user.CreatedAt,
            user.LastLogin ?? DateTime.MinValue
        ));
    }
}
