using Microsoft.EntityFrameworkCore;
using VTOS.Application.Abstractions;
using VTOS.Application.Common;
using VTOS.Domain.Enums;

namespace VTOS.Application.Features.Schools.Queries;

public class GetSchoolOrdersQueryHandler : IGetSchoolOrdersQueryHandler
{
    private readonly IApplicationDbContext _db;

    public GetSchoolOrdersQueryHandler(IApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<Result<SchoolOrderListResponse>> HandleAsync(GetSchoolOrdersQuery query, CancellationToken ct = default)
    {
        var ordersQuery = _db.Orders
            .AsNoTracking()
            .Where(o => o.ChildProfile.SchoolID == query.SchoolId);

        if (!string.IsNullOrWhiteSpace(query.Status) && Enum.TryParse<OrderStatus>(query.Status, true, out var parsedStatus))
        {
            ordersQuery = ordersQuery.Where(o => o.OrderStatus == parsedStatus);
        }

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var search = query.Search.Trim();
            ordersQuery = ordersQuery.Where(o =>
                o.ChildProfile.FullName.Contains(search) ||
                (o.ChildProfile.ParentUser != null && o.ChildProfile.ParentUser.FullName.Contains(search)) ||
                (o.RecipientName != null && o.RecipientName.Contains(search)) ||
                (o.RecipientPhone != null && o.RecipientPhone.Contains(search)) ||
                (o.TrackingCode != null && o.TrackingCode.Contains(search)));
        }

        var totalCount = await ordersQuery.CountAsync(ct);

        var items = await ordersQuery
            .OrderByDescending(o => o.OrderDate)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(o => new SchoolOrderDto
            {
                OrderId = o.Id,
                OrderDate = o.OrderDate,
                OrderStatus = o.OrderStatus.ToString(),
                TotalAmount = o.TotalAmount,
                ChildName = o.ChildProfile.FullName,
                ParentName = o.ChildProfile.ParentUser != null ? o.ChildProfile.ParentUser.FullName : string.Empty,
                RecipientName = o.RecipientName,
                RecipientPhone = o.RecipientPhone,
                TrackingCode = o.TrackingCode,
                ShippingCompany = o.ShippingCompany,
                DeliveryMethod = o.DeliveryMethod,
                LatestPaymentStatus = o.PaymentTransactions
                    .OrderByDescending(pt => pt.TransactionTimestamp)
                    .Select(pt => pt.TransactionStatus.ToString())
                    .FirstOrDefault(),
                TotalQuantity = o.OrderItems.Sum(oi => oi.Quantity),
                Items = o.OrderItems
                    .OrderBy(oi => oi.ProductVariant.Outfit.OutfitName)
                    .Select(oi => new SchoolOrderItemDto
                    {
                        ProductVariantId = oi.ProductVariantID,
                        OutfitId = oi.ProductVariant.OutfitID,
                        OutfitName = oi.ProductVariant.Outfit.OutfitName,
                        Size = oi.SizeOrdered,
                        Quantity = oi.Quantity,
                        UnitPrice = oi.UnitPrice
                    })
                    .ToList()
            })
            .ToListAsync(ct);

        return Result<SchoolOrderListResponse>.Success(new SchoolOrderListResponse
        {
            Items = items,
            TotalCount = totalCount,
            Page = query.Page,
            PageSize = query.PageSize
        });
    }
}
