using Microsoft.EntityFrameworkCore;
using VTOS.Application.Abstractions;
using VTOS.Application.Common;
using VTOS.Application.Features.Orders.DTOs;
using VTOS.Domain.Enums;

namespace VTOS.Application.Features.Orders.Queries;

public record GetMyDirectOrdersQuery(Guid ParentId, int Page = 1, int PageSize = 10, string? Status = null);

public interface IGetMyDirectOrdersQueryHandler
{
    Task<Result<MyDirectOrdersResponse>> HandleAsync(GetMyDirectOrdersQuery query, CancellationToken cancellationToken = default);
}

public record GetMyDirectOrderDetailQuery(Guid ParentId, Guid OrderId);

public interface IGetMyDirectOrderDetailQueryHandler
{
    Task<Result<MyDirectOrderDetailDto>> HandleAsync(GetMyDirectOrderDetailQuery query, CancellationToken cancellationToken = default);
}

public class GetMyDirectOrdersQueryHandler : IGetMyDirectOrdersQueryHandler
{
    private readonly IApplicationDbContext _context;

    public GetMyDirectOrdersQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result<MyDirectOrdersResponse>> HandleAsync(GetMyDirectOrdersQuery query, CancellationToken cancellationToken = default)
    {
        var page = Math.Max(1, query.Page);
        var pageSize = Math.Clamp(query.PageSize, 1, 50);

        var ordersQuery = _context.Orders
            .AsNoTracking()
            .Include(o => o.ChildProfile)
            .Include(o => o.Provider)
            .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.ProductVariant)
                    .ThenInclude(v => v.Outfit)
            .Include(o => o.PaymentTransactions)
            .Where(o => o.ProviderID != null && o.SemesterPublicationID != null && o.ChildProfile.ParentUserID == query.ParentId);

        if (!string.IsNullOrWhiteSpace(query.Status)
            && Enum.TryParse<VTOS.Domain.Enums.OrderStatus>(query.Status, true, out var parsedStatus))
        {
            ordersQuery = ordersQuery.Where(o => o.OrderStatus == parsedStatus);
        }

        var totalCount = await ordersQuery.CountAsync(cancellationToken);
        var orders = await ordersQuery
            .OrderByDescending(o => o.OrderDate)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return Result<MyDirectOrdersResponse>.Success(new MyDirectOrdersResponse
        {
            Items = orders.Select(o => new MyDirectOrderListItemDto
            {
                OrderId = o.Id,
                OrderDate = o.OrderDate,
                OrderStatus = o.OrderStatus,
                OrderStatusName = o.OrderStatus.ToString(),
                TotalAmount = o.TotalAmount,
                ChildName = o.ChildProfile.FullName,
                ProviderName = o.Provider?.ProviderName ?? string.Empty,
                FirstItemImageUrl = o.OrderItems.Select(oi => oi.ProductVariant.VariantImageURL ?? oi.ProductVariant.Outfit.MainImageURL).FirstOrDefault(),
                PaymentStatusName = o.PaymentTransactions.OrderByDescending(t => t.TransactionTimestamp).FirstOrDefault()?.TransactionStatus.ToString(),
                TrackingCode = o.TrackingCode
            }).ToList(),
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize,
            TotalPages = (int)Math.Ceiling((double)totalCount / pageSize)
        });
    }
}

public class GetMyDirectOrderDetailQueryHandler : IGetMyDirectOrderDetailQueryHandler
{
    private readonly IApplicationDbContext _context;

    public GetMyDirectOrderDetailQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result<MyDirectOrderDetailDto>> HandleAsync(GetMyDirectOrderDetailQuery query, CancellationToken cancellationToken = default)
    {
        var order = await _context.Orders
            .AsNoTracking()
            .Include(o => o.ChildProfile)
            .Include(o => o.Provider)
            .Include(o => o.SemesterPublication)
            .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.ProductVariant)
                    .ThenInclude(v => v.Outfit)
            .Include(o => o.PaymentTransactions)
            .FirstOrDefaultAsync(
                o => o.Id == query.OrderId
                    && o.ProviderID != null
                    && o.SemesterPublicationID != null
                    && o.ChildProfile.ParentUserID == query.ParentId,
                cancellationToken);

        if (order == null)
            return Result<MyDirectOrderDetailDto>.Failure("Direct order not found.", "ORDER_NOT_FOUND");

        var existingRating = await _context.ProviderRatings
            .AsNoTracking()
            .Where(x => x.OrderID == order.Id && x.ParentUserID == query.ParentId)
            .Select(x => new ExistingProviderRatingDto
            {
                ProviderRatingId = x.Id,
                Rating = x.Rating,
                Comment = x.Comment,
                CreatedAt = x.CreatedAt
            })
            .FirstOrDefaultAsync(cancellationToken);

        return Result<MyDirectOrderDetailDto>.Success(new MyDirectOrderDetailDto
        {
            OrderId = order.Id,
            ChildProfileId = order.ChildProfileID,
            ChildName = order.ChildProfile.FullName,
            ProviderId = order.ProviderID!.Value,
            ProviderName = order.Provider?.ProviderName ?? string.Empty,
            SemesterPublicationId = order.SemesterPublicationID!.Value,
            Semester = order.SemesterPublication?.Semester ?? string.Empty,
            AcademicYear = order.SemesterPublication?.AcademicYear ?? string.Empty,
            OrderDate = order.OrderDate,
            OrderStatus = order.OrderStatus.ToString(),
            TotalAmount = order.TotalAmount,
            ShippingAddress = order.ShippingAddress,
            DeliveryMethod = order.DeliveryMethod,
            RecipientName = order.RecipientName,
            RecipientPhone = order.RecipientPhone,
            TrackingCode = order.TrackingCode,
            ShippingCompany = order.ShippingCompany,
            PaymentStatusName = order.PaymentTransactions.OrderByDescending(t => t.TransactionTimestamp).FirstOrDefault()?.TransactionStatus.ToString(),
            CanRateProvider = order.OrderStatus == OrderStatus.Delivered && existingRating == null,
            ExistingProviderRating = existingRating,
            Items = order.OrderItems.Select(oi => new MyDirectOrderDetailItemDto
            {
                OrderItemId = oi.Id,
                ProductVariantId = oi.ProductVariantID,
                OutfitId = oi.ProductVariant.OutfitID,
                OutfitName = oi.ProductVariant.Outfit.OutfitName,
                ImageUrl = oi.ProductVariant.VariantImageURL ?? oi.ProductVariant.Outfit.MainImageURL,
                Size = oi.SizeOrdered,
                Quantity = oi.Quantity,
                UnitPrice = oi.UnitPrice
            }).ToList()
        });
    }
}
