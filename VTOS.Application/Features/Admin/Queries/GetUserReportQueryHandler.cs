using Microsoft.EntityFrameworkCore;
using VTOS.Application.Abstractions;
using VTOS.Application.Features.Admin.DTOs;

namespace VTOS.Application.Features.Admin.Queries;

public class GetUserReportQueryHandler : IGetUserReportQueryHandler
{
    private readonly IApplicationDbContext _context;

    public GetUserReportQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<UserReportDto> HandleAsync(
        GetUserReportQuery query,
        CancellationToken cancellationToken)
    {
        // Build filter query
        var usersQuery = _context.Users
            .Include(u => u.Role)
            .Where(u => !u.IsDeleted);

        // Apply date filters if provided
        if (query.DateFrom.HasValue)
            usersQuery = usersQuery.Where(u => u.CreatedAt >= query.DateFrom.Value);
        
        if (query.DateTo.HasValue)
            usersQuery = usersQuery.Where(u => u.CreatedAt <= query.DateTo.Value);

        // Apply role filter if provided
        if (!string.IsNullOrEmpty(query.Role))
            usersQuery = usersQuery.Where(u => u.Role.RoleName == query.Role);

        // Get user list
        var users = await usersQuery.ToListAsync(cancellationToken);

        // Apply status filter
        if (!string.IsNullOrEmpty(query.Status))
        {
            users = query.Status.ToUpper() switch
            {
                "ACTIVE" => users.Where(u => u.IsActive).ToList(),
                "BANNED" => users.Where(u => !u.IsActive).ToList(),
                _ => users
            };
        }

        // Calculate summaries
        var totalUsers = users.Count;
        var totalParents = users.Count(u => u.Role.RoleName == "Parent");
        var totalSchools = users.Count(u => u.Role.RoleName == "School");
        var totalProviders = users.Count(u => u.Role.RoleName == "Provider");
        var totalAdmins = users.Count(u => u.Role.RoleName == "Admin");
        var activeUsers = users.Count(u => u.IsActive);
        var bannedUsers = users.Count(u => !u.IsActive);

        // Calculate total spending and orders
        var userIds = users.Select(u => u.Id).ToList();
        var ordersData = await _context.Orders.Include(o=>o.ChildProfile)
            .Where(o => userIds.Contains(o.ChildProfile.ParentUserID ?? new Guid()) && o.OrderStatus != Domain.Enums.OrderStatus.Cancelled)
            .GroupBy(o => o.ChildProfile.ParentUserID)
            .Select(g => new
            {
                UserId = g.Key,
                OrderCount = g.Count(),
                TotalSpending = g.Sum(o => o.TotalAmount)
            })
            .ToListAsync(cancellationToken);

        var totalSpending = ordersData.Sum(x => x.TotalSpending);
        var totalOrders = ordersData.Sum(x => x.OrderCount);

        // Group by role
        var usersByRole = users
            .GroupBy(u => u.Role.RoleName)
            .Select(g => new UserByRoleDto(
                g.Key,
                g.Count(),
                g.Count(u => u.IsActive),
                g.Count(u => !u.IsActive)
            ))
            .ToList();

        // Recent activity (last 10 active users)
        var recentActivity = users
            .OrderByDescending(u => u.LastLogin ?? u.CreatedAt)
            .Take(10)
            .Select(u => new UserActivityDto(
                u.Id,
                u.FullName,
                u.Role.RoleName,
                ordersData.FirstOrDefault(o => o.UserId == u.Id)?.OrderCount ?? 0,
                ordersData.FirstOrDefault(o => o.UserId == u.Id)?.TotalSpending ?? 0,
                u.LastLogin
            ))
            .ToList();

        return new UserReportDto(
            totalUsers,
            totalParents,
            totalSchools,
            totalProviders,
            totalAdmins,
            activeUsers,
            bannedUsers,
            totalSpending,
            totalOrders,
            usersByRole,
            recentActivity
        );
     }
}
