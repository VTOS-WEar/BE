using Microsoft.EntityFrameworkCore;
using VTOS.Application.Abstractions;
using VTOS.Application.Features.Admin.DTOs;

namespace VTOS.Application.Features.Admin.Queries;

public class GetParentDetailQueryHandler : IGetParentDetailQueryHandler
{
    private readonly IApplicationDbContext _context;

    public GetParentDetailQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<ParentDetailDto?> HandleAsync(
        GetParentDetailQuery query,
        CancellationToken cancellationToken)
    {
        var parent = await _context.Users
            .Include(u => u.Role)
            .Include(u => u.ParentProfile)
            .Include(u => u.ChildProfiles)
                .ThenInclude(c => c.School)
            .FirstOrDefaultAsync(u => u.Id == query.ParentId && u.Role.RoleName == "Parent" && !u.IsDeleted, cancellationToken);

        if (parent == null)
            return null;

        // Get children
        var children = parent.ChildProfiles
            .Where(c => !c.IsDeleted)
            .Select(c => new ParentChildDto(
                c.Id,
                c.FullName,
                c.School?.SchoolName,
                c.Grade
            ))
            .ToList();

        // Get orders
        var orders = await _context.Orders.Include(o => o.ChildProfile)
            .Where(o => o.ChildProfile.ParentUserID == query.ParentId && o.OrderStatus != Domain.Enums.OrderStatus.Cancelled)
            .OrderByDescending(o => o.CreatedAt)
            .Take(20)
            .Select(o => new ParentOrderDto(
                o.Id,
                o.Id.GetHashCode(), // Simple order number
                o.OrderStatus.ToString(),
                o.TotalAmount,
                o.CreatedAt
            ))
            .ToListAsync(cancellationToken);

        var totalSpending = await _context.Orders.Include(o => o.ChildProfile)
            .Where(o => o.ChildProfile.ParentUserID == query.ParentId && o.OrderStatus != Domain.Enums.OrderStatus.Cancelled)
            .SumAsync(o => o.TotalAmount, cancellationToken);

        return new ParentDetailDto(
            parent.Id,
            parent.FullName,
            parent.Email,
            parent.Phone,
            parent.Avatar,
            parent.ParentProfile?.DOB,
            (parent.ParentProfile?.Gender ?? Domain.Enums.Gender.Other).ToString(),
            parent.IsActive ? "Active" : "Banned",
            parent.CreatedAt,
            parent.LastLogin,
            children,
            orders,
            totalSpending
        );
    }
}
