using Microsoft.EntityFrameworkCore;
using VTOS.Application.Abstractions;
using VTOS.Application.Features.Admin.DTOs;
using VTOS.Domain.Enums;

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
            .FirstOrDefaultAsync(u => u.Id == query.UserId && !u.IsDeleted, cancellationToken);

        if (user == null)
            return null;

        // Calculate total spending from orders
        var totalSpending = await _context.Orders.Include(o=>o.ChildProfile)
            .Where(o => o.ChildProfile.ParentUserID == query.UserId && o.OrderStatus != OrderStatus.Cancelled)
            .SumAsync(o => o.TotalAmount, cancellationToken);

        return new UserDetailDto(
            user.Id,
            user.Email,
            user.FullName,
            user.Phone,
            user.DOB,
            user.Gender.ToString(),
            user.Avatar,
            user.Role.RoleName,
            user.SchoolID,
            user.School?.SchoolName,
            user.IsActive,
            user.CreatedAt,
            user.LastLogin,
            user.ChildProfiles.Count(c => !c.IsDeleted),
            totalSpending
        );
    }
}
