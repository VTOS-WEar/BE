using Microsoft.EntityFrameworkCore;
using VTOS.Application.Abstractions;
using VTOS.Application.Features.Admin.DTOs;

namespace VTOS.Application.Features.Admin.Queries;

public class GetAllUsersQueryHandler : IGetAllUsersQueryHandler
{
    private readonly IApplicationDbContext _context;

    public GetAllUsersQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<UserListItemDto>> HandleAsync(
        GetAllUsersQuery query,
        CancellationToken cancellationToken)
    {
        return await _context.Users
            .Include(u => u.Role)
            .OrderByDescending(u => u.CreatedAt)
            .Select(u => new UserListItemDto(
                u.Id,
                u.Email,
                u.FullName,
                u.Role.RoleName,   // ✅ đúng field
                u.IsActive,
                u.IsDeleted,
                u.CreatedAt
            ))
            .ToListAsync(cancellationToken);
    }
}
