using System.Runtime.InteropServices;
using Microsoft.EntityFrameworkCore;
using VTOS.Application.Abstractions;
using VTOS.Application.Features.Admin.DTOs;

namespace VTOS.Application.Features.Admin.Queries;

public class GetParentListQueryHandler : IGetParentListQueryHandler
{
    private readonly IApplicationDbContext _context;

    public GetParentListQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<PaginatedResult<ParentListItemDto>> HandleAsync(
        GetParentListQuery query,
        CancellationToken cancellationToken)
    {
        var parentsQuery = _context.Users
            .Include(u => u.Role)
            .Include(u => u.ChildProfiles)
            .Where(u => u.Role.RoleName == "Parent" && !u.IsDeleted);

        // Apply search filter
        if (!string.IsNullOrEmpty(query.Search))
        {
            parentsQuery = parentsQuery.Where(u =>
                u.Email.Contains(query.Search) ||
                u.FullName.Contains(query.Search) ||
                u.Phone != null && u.Phone.Contains(query.Search)
            );
        }

        // Apply status filter
        if (!string.IsNullOrEmpty(query.Status))
        {
            parentsQuery = query.Status.ToUpper() switch
            {
                "ACTIVE" => parentsQuery.Where(u => u.IsActive),
                "BANNED" => parentsQuery.Where(u => !u.IsActive),
                _ => parentsQuery
            };
        }

        var totalCount = await parentsQuery.CountAsync(cancellationToken);

        var parents = await parentsQuery
            .OrderByDescending(u => u.CreatedAt)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync(cancellationToken);

        // Get order statistics for parents
        var parentIds = parents.Select(p => p.Id).ToList();
        var orderStats = await _context.Orders.Include(o => o.ChildProfile)
            .Where(o => parentIds.Contains(o.ChildProfile.ParentUserID?? new Guid()) && o.OrderStatus != Domain.Enums.OrderStatus.Cancelled)
            .GroupBy(o => o.ChildProfile.ParentUserID)
            .Select(g => new
            {
                UserId = g.Key,
                OrderCount = g.Count(),
                TotalSpending = g.Sum(o => o.TotalAmount)
            })
            .ToListAsync(cancellationToken);

        var items = parents.Select(p => new ParentListItemDto(
            p.Id,
            p.FullName,
            p.Email,
            p.Phone,
            p.ChildProfiles.Count(c => !c.IsDeleted),
            orderStats.FirstOrDefault(s => s.UserId == p.Id)?.OrderCount ?? 0,
            orderStats.FirstOrDefault(s => s.UserId == p.Id)?.TotalSpending ?? 0,
            p.IsActive ? "Active" : "Banned",
            p.CreatedAt
        )).ToList();

        return new PaginatedResult<ParentListItemDto>(items, totalCount, query.Page, query.PageSize);
    }
}
