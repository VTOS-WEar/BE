using Microsoft.EntityFrameworkCore;
using VTOS.Application.Abstractions;
using VTOS.Application.Common;
using VTOS.Application.Features.Schools.DTOs;

namespace VTOS.Application.Features.Schools.Queries;

/// <summary>
/// UC-45: Get orders associated with the school.
/// Orders are linked via Campaign.SchoolID or ChildProfile.SchoolID.
/// </summary>
public class GetSchoolOrdersQueryHandler : IGetSchoolOrdersQueryHandler
{
    private readonly IApplicationDbContext _db;

    public GetSchoolOrdersQueryHandler(IApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<Result<SchoolOrderListResponse>> HandleAsync(GetSchoolOrdersQuery query, CancellationToken ct = default)
    {
        // Get orders related to this school via Campaign or ChildProfile
        var ordersQuery = _db.Orders
            .AsNoTracking()
            .Include(o => o.ChildProfile)
                .ThenInclude(cp => cp.ParentUser)
            .Include(o => o.Campaign)
            .Include(o => o.OrderItems)
            .Where(o =>
                (o.Campaign != null && o.Campaign.SchoolID == query.SchoolId) ||
                o.ChildProfile.SchoolID == query.SchoolId
            );

        // Apply status filter
        if (!string.IsNullOrEmpty(query.Status) && Enum.TryParse<VTOS.Domain.Enums.OrderStatus>(query.Status, true, out var status))
        {
            ordersQuery = ordersQuery.Where(o => o.OrderStatus == status);
        }

        var totalCount = await ordersQuery.CountAsync(ct);

        var orders = await ordersQuery
            .OrderByDescending(o => o.OrderDate)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(o => new SchoolOrderDto
            {
                OrderId = o.Id,
                ParentName = o.ChildProfile.ParentUser.FullName,
                ChildName = o.ChildProfile.FullName,
                TotalAmount = o.TotalAmount,
                OrderDate = o.OrderDate,
                OrderStatus = o.OrderStatus.ToString(),
                CampaignName = o.Campaign != null ? o.Campaign.CampaignName : null,
                ItemCount = o.OrderItems.Count
            })
            .ToListAsync(ct);

        return Result<SchoolOrderListResponse>.Success(new SchoolOrderListResponse
        {
            Items = orders,
            TotalCount = totalCount,
            Page = query.Page,
            PageSize = query.PageSize
        });
    }
}
