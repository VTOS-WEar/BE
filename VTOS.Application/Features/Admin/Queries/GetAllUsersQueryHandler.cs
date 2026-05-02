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

    public async Task<UserListPagedResult> HandleAsync(
        GetAllUsersQuery query,
        CancellationToken cancellationToken)
    {
        var q = _context.Users
            .Include(u => u.Role)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var search = query.Search.Trim().ToLower();
            q = q.Where(u =>
                u.FullName.ToLower().Contains(search) ||
                u.Email.ToLower().Contains(search));
        }

        if (!string.IsNullOrWhiteSpace(query.Role))
        {
            q = q.Where(u => u.Role.RoleName == query.Role);
        }

        if (!string.IsNullOrWhiteSpace(query.Status))
        {
            if (query.Status == "Active")
                q = q.Where(u => u.IsActive && !u.IsDeleted);
            else if (query.Status == "Suspended")
                q = q.Where(u => !u.IsActive || u.IsDeleted);
        }

        var totalCount = await q.CountAsync(cancellationToken);

        var items = await q
            .OrderByDescending(u => u.CreatedAt)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(u => new UserListItemDto(
                u.Id,
                u.Email,
                u.FullName,
                u.Role.RoleName,
                u.IsActive,
                u.IsDeleted,
                u.CreatedAt))
            .ToListAsync(cancellationToken);

        return new UserListPagedResult(items, totalCount, query.Page, query.PageSize);
    }
}
