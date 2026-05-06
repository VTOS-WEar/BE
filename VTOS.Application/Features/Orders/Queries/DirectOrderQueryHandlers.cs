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
    private const string OrderCancellationCategory = "OrderCancellation";

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
                .ThenInclude(c => c.School)
            .Include(o => o.Provider)
            .Include(o => o.SemesterPublication)
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

        var orderIds = orders.Select(o => o.Id).ToList();
        var cancellationTicketRows = await _context.SupportTickets
            .AsNoTracking()
            .Where(t => t.OrderID.HasValue && orderIds.Contains(t.OrderID.Value) && t.Category == OrderCancellationCategory)
            .OrderByDescending(t => t.CreatedAt)
            .ToListAsync(cancellationToken);

        var cancellationTickets = cancellationTicketRows
            .GroupBy(t => t.OrderID!.Value)
            .ToDictionary(g => g.Key, g => g.First());

        return Result<MyDirectOrdersResponse>.Success(new MyDirectOrdersResponse
        {
            Items = orders.Select(o =>
            {
                cancellationTickets.TryGetValue(o.Id, out var ticket);
                return new MyDirectOrderListItemDto
                {
                    OrderId = o.Id,
                    OrderDate = o.OrderDate,
                    OrderStatus = o.OrderStatus,
                    OrderStatusName = o.OrderStatus.ToString(),
                    TotalAmount = o.TotalAmount,
                    ChildName = o.ChildProfile.FullName,
                    ProviderName = o.Provider?.ProviderName ?? string.Empty,
                    PricingMode = DirectOrderQueryHelpers.ResolvePricingModeName(o.AppliedPricingMode, o.SemesterPublication?.EndDate, o.OrderDate),
                    FirstItemImageUrl = o.OrderItems.Select(oi => oi.ProductVariant.VariantImageURL ?? oi.ProductVariant.Outfit.MainImageURL).FirstOrDefault(),
                    PaymentStatusName = o.PaymentTransactions.OrderByDescending(t => t.TransactionTimestamp).FirstOrDefault()?.TransactionStatus.ToString(),
                    TrackingCode = o.TrackingCode,
                    CanCancel = DirectOrderQueryHelpers.CanRequestCancellation(o.OrderStatus, ticket?.Status),
                    CanReorder = DirectOrderQueryHelpers.CanReorder(o.OrderStatus),
                    CancelReason = o.CancelReason,
                    CancellationTicketId = ticket?.Id,
                    CancellationTicketStatus = ticket?.Status.ToString()
                };
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
    private const string OrderCancellationCategory = "OrderCancellation";

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
                .ThenInclude(c => c.School)
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

        var orderItemIds = order.OrderItems.Select(oi => oi.Id).ToList();
        var existingProviderRating = orderItemIds.Count == 0
            ? null
            : await _context.Feedbacks
                .AsNoTracking()
                .Where(f => f.UserID == query.ParentId && orderItemIds.Contains(f.OrderItemID))
                .OrderByDescending(f => f.Timestamp)
                .ThenByDescending(f => f.Id)
                .Select(f => new ExistingProviderRatingDto
                {
                    ProviderRatingId = f.Id,
                    Rating = f.Rating,
                    Comment = f.Comment,
                    CreatedAt = f.Timestamp
                })
                .FirstOrDefaultAsync(cancellationToken);

        var cancellationTicket = await _context.SupportTickets
            .AsNoTracking()
            .Where(t => t.OrderID == order.Id && t.Category == OrderCancellationCategory)
            .OrderByDescending(t => t.CreatedAt)
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
            SchoolId = order.SemesterPublication?.SchoolID ?? order.ChildProfile.SchoolID,
            SchoolName = order.ChildProfile.School?.SchoolName ?? string.Empty,
            PricingMode = DirectOrderQueryHelpers.ResolvePricingModeName(order.AppliedPricingMode, order.SemesterPublication?.EndDate, order.OrderDate),
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
            CanRateProvider = order.OrderStatus == VTOS.Domain.Enums.OrderStatus.Delivered && existingProviderRating == null,
            CanCancel = DirectOrderQueryHelpers.CanRequestCancellation(order.OrderStatus, cancellationTicket?.Status),
            CanReorder = DirectOrderQueryHelpers.CanReorder(order.OrderStatus),
            CancelReason = order.CancelReason,
            CancellationTicketId = cancellationTicket?.Id,
            CancellationTicketStatus = cancellationTicket?.Status.ToString(),
            ExistingProviderRating = existingProviderRating,
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

internal static class DirectOrderQueryHelpers
{
    internal static bool CanRequestCancellation(
        VTOS.Domain.Enums.OrderStatus orderStatus,
        VTOS.Domain.Enums.SupportTicketStatus? cancellationTicketStatus)
    {
        if (orderStatus != VTOS.Domain.Enums.OrderStatus.Paid)
            return false;

        return cancellationTicketStatus is null
            or VTOS.Domain.Enums.SupportTicketStatus.Resolved
            or VTOS.Domain.Enums.SupportTicketStatus.Closed;
    }

    internal static bool CanReorder(VTOS.Domain.Enums.OrderStatus orderStatus)
    {
        return orderStatus == VTOS.Domain.Enums.OrderStatus.Cancelled;
    }

    internal static string ResolvePricingModeName(
        VTOS.Domain.Enums.OrderPricingMode? appliedPricingMode,
        DateTime? publicationEndDate,
        DateTime orderDateUtc)
    {
        if (appliedPricingMode.HasValue)
            return appliedPricingMode.Value.ToString();

        if (!publicationEndDate.HasValue)
            return string.Empty;

        return orderDateUtc > publicationEndDate.Value
            ? VTOS.Domain.Enums.OrderPricingMode.PostDeadlineDirect.ToString()
            : VTOS.Domain.Enums.OrderPricingMode.PublicationWindow.ToString();
    }
}
