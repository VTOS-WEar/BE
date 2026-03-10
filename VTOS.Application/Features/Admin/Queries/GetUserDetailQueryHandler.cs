using Microsoft.EntityFrameworkCore;
using VTOS.Application.Abstractions;
using VTOS.Application.Features.Admin.DTOs;

namespace VTOS.Application.Features.Admin.Queries;

public class GetUserDetailQueryHandler : IGetUserDetailQueryHandler
{
    private readonly IApplicationDbContext _context;

    public GetUserDetailQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<UserDetailDto?> HandleAsync(
        GetUserDetailQuery query,
        CancellationToken cancellationToken)
    {
        var user = await _context.Users
            .Include(u => u.Role)
            .Include(u => u.School)
            .Include(u => u.ChildProfiles)
            .Where(u => u.Id == query.UserId)
            .Select(u => new UserDetailDto(
                u.Id,
                u.Email,
                u.FullName,
                u.Role.RoleName,
                u.IsActive,
                u.IsDeleted,
                u.CreatedAt,
                u.LastLogin,
                u.Phone,
                u.School != null ? u.School.SchoolName : null,
                u.ChildProfiles.Count
            ))
            .FirstOrDefaultAsync(cancellationToken);

        return user;
    }
}