using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using VTOS.Application.Abstractions;
using VTOS.Application.Common;
using VTOS.Application.Common.Models;
using VTOS.Application.Common.Settings;
using VTOS.Application.Features.Orders.DTOs;
using VTOS.Domain.Entities;
using VTOS.Domain.Enums;

namespace VTOS.Application.Features.Orders.Commands;

public record CreateDirectOrderCommand(
    Guid ParentId,
    Guid ChildProfileId,
    Guid SemesterPublicationId,
    Guid ProviderId,
    List<DirectOrderItemRequest> Items,
    string ShippingAddress,
    string? DeliveryMethod,
    string? RecipientName,
    string? RecipientPhone);

public interface ICreateDirectOrderCommandHandler
{
    Task<Result<CreateDirectOrderResponse>> HandleAsync(CreateDirectOrderCommand command, CancellationToken cancellationToken = default);
}

public record CancelDirectOrderCommand(Guid ParentId, Guid OrderId, string? Reason);

public interface ICancelDirectOrderCommandHandler
{
    Task<Result> HandleAsync(CancelDirectOrderCommand command, CancellationToken cancellationToken = default);
}

public record SubmitProviderRatingCommand(Guid ParentId, Guid OrderId, int Rating, string? Comment);

public interface ISubmitProviderRatingCommandHandler
{
    Task<Result<SubmitProviderRatingResponse>> HandleAsync(SubmitProviderRatingCommand command, CancellationToken cancellationToken = default);
}

public class CreateDirectOrderCommandHandler : ICreateDirectOrderCommandHandler
{
    private readonly IApplicationDbContext _context;
    private readonly IPayOSService _payOSService;
    private readonly ILogger<CreateDirectOrderCommandHandler> _logger;
    private readonly PaymentSettings _paymentSettings;

    public CreateDirectOrderCommandHandler(
        IApplicationDbContext context,
        IPayOSService payOSService,
        ILogger<CreateDirectOrderCommandHandler> logger,
        IOptions<PaymentSettings> paymentSettings)
    {
        _context = context;
        _payOSService = payOSService;
        _logger = logger;
        _paymentSettings = paymentSettings.Value;
    }

    public async Task<Result<CreateDirectOrderResponse>> HandleAsync(CreateDirectOrderCommand command, CancellationToken cancellationToken = default)
    {
        var validation = Validate(command);
        if (!validation.IsSuccess)
            return Result<CreateDirectOrderResponse>.Failure(validation.Error!, validation.ErrorCode);

        var child = await _context.ChildProfiles
            .AsNoTracking()
            .FirstOrDefaultAsync(cp => cp.Id == command.ChildProfileId, cancellationToken);

        if (child == null)
            return Result<CreateDirectOrderResponse>.Failure("Child profile not found.", "CHILD_NOT_FOUND");

        if (child.ParentUserID != command.ParentId)
            return Result<CreateDirectOrderResponse>.Failure("Child profile does not belong to current parent.", "FORBIDDEN_CHILD");

        var now = DateTime.UtcNow;
        var usableContractStatuses = new[] { "Active", "InUse" };

        var publication = await _context.SemesterPublications
            .AsNoTracking()
            .FirstOrDefaultAsync(
                sp => sp.Id == command.SemesterPublicationId
                    && sp.Status == SemesterPublicationStatus.Active,
                cancellationToken);

        if (publication == null)
            return Result<CreateDirectOrderResponse>.Failure("Semester publication is not available for ordering.", "PUBLICATION_NOT_AVAILABLE");

        var publicationProvider = await _context.SemesterPublicationProviders
            .AsNoTracking()
            .FirstOrDefaultAsync(
                spp => spp.SemesterPublicationID == command.SemesterPublicationId
                    && spp.ProviderID == command.ProviderId
                    && spp.Status == SemPublicationProviderStatus.Active
                    && spp.ContractID.HasValue
                    && spp.Contract != null
                    && usableContractStatuses.Contains(spp.Contract.Status)
                    && spp.Contract.ExpiresAt > now,
                cancellationToken);

        if (publicationProvider == null)
            return Result<CreateDirectOrderResponse>.Failure("Provider is not approved for this semester publication.", "PROVIDER_NOT_APPROVED");

        var variantIds = command.Items.Select(i => i.ProductVariantId).Distinct().ToList();
        var variants = await _context.ProductVariants
            .Include(v => v.Outfit)
            .Where(v => variantIds.Contains(v.Id) && !v.IsDeleted)
            .ToListAsync(cancellationToken);

        if (variants.Count != variantIds.Count)
            return Result<CreateDirectOrderResponse>.Failure("One or more product variants do not exist.", "VARIANT_NOT_FOUND");

        var outfitIds = variants.Select(v => v.OutfitID).Distinct().ToList();
        var publishedOutfitIds = await _context.SemesterPublicationOutfits
            .AsNoTracking()
            .Where(spo => spo.SemesterPublicationID == command.SemesterPublicationId && outfitIds.Contains(spo.OutfitID))
            .Select(spo => spo.OutfitID)
            .ToListAsync(cancellationToken);

        if (publishedOutfitIds.Count != outfitIds.Count)
            return Result<CreateDirectOrderResponse>.Failure("One or more outfits are not available in this semester publication.", "OUTFIT_NOT_IN_PUBLICATION");

        var catalogItems = await _context.ProviderCatalogItems
            .AsNoTracking()
            .Where(ci =>
                ci.SemesterPublicationProviderID == publicationProvider.Id
                && ci.ProviderID == command.ProviderId
                && outfitIds.Contains(ci.OutfitID)
                && (ci.Status == ProviderCatalogItemStatus.Published || ci.Status == ProviderCatalogItemStatus.Ready))
            .ToListAsync(cancellationToken);

        var pricingMode = ResolvePricingMode(publication.EndDate, now);

        var pricingByOutfit = new Dictionary<Guid, (decimal PublicationPrice, decimal PostDeadlinePrice)>();
        foreach (var outfitId in outfitIds)
        {
            var catalogItem = catalogItems.FirstOrDefault(ci => ci.OutfitID == outfitId);
            if (catalogItem == null)
            {
                return Result<CreateDirectOrderResponse>.Failure(
                    "One or more outfits are not available for this provider in the selected semester publication.",
                    "CATALOG_ITEM_NOT_AVAILABLE");
            }

            if (catalogItem.PostDeadlinePrice < catalogItem.PublicationPrice)
                return Result<CreateDirectOrderResponse>.Failure(
                    "Provider listing data is invalid because post-deadline price is lower than publication price.",
                    "INVALID_CATALOG_PRICING");

            pricingByOutfit[outfitId] = (catalogItem.PublicationPrice, catalogItem.PostDeadlinePrice);
        }

        var order = new Order
        {
            Id = Guid.NewGuid(),
            ChildProfileID = command.ChildProfileId,
            OrderDate = DateTime.UtcNow,
            OrderStatus = OrderStatus.Pending,
            TotalAmount = 0,
            ShippingAddress = command.ShippingAddress,
            ProviderID = command.ProviderId,
            SemesterPublicationID = command.SemesterPublicationId,
            AppliedPricingMode = pricingMode,
            DeliveryMethod = command.DeliveryMethod,
            RecipientName = command.RecipientName,
            RecipientPhone = command.RecipientPhone,
            CreatedAt = DateTime.UtcNow
        };

        decimal totalAmount = 0;
        var orderItems = new List<OrderItem>();

        foreach (var item in command.Items)
        {
            var variant = variants.First(v => v.Id == item.ProductVariantId);
            var pricing = pricingByOutfit[variant.OutfitID];
            var unitPrice = pricingMode == OrderPricingMode.PostDeadlineDirect
                ? pricing.PostDeadlinePrice
                : pricing.PublicationPrice;

            totalAmount += unitPrice * item.Quantity;

            orderItems.Add(new OrderItem
            {
                Id = Guid.NewGuid(),
                OrderID = order.Id,
                ProductVariantID = variant.Id,
                Quantity = item.Quantity,
                UnitPrice = unitPrice,
                SizeOrdered = variant.Size,
                IsCustomOrder = item.IsCustomOrder,
                CustomMeasurements = item.CustomMeasurements,
                CreatedAt = DateTime.UtcNow
            });
        }

        order.TotalAmount = totalAmount;

        CreatePaymentLinkResponse paymentLink;
        try
        {
            paymentLink = await _payOSService.CreatePayOSPaymentLinkAsync(
                new CreatePaymentLinkRequest
                {
                    Amount = (int)totalAmount,
                    Description = $"Thanh toan don hang {order.Id.ToString()[..8]}",
                    ReturnUrl = $"{_paymentSettings.ReturnBaseUrl}{_paymentSettings.ReturnSuccessPath}",
                    CancelUrl = $"{_paymentSettings.ReturnBaseUrl}{_paymentSettings.ReturnCancelPath}"
                },
                cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create PayOS link for direct order.");
            return Result<CreateDirectOrderResponse>.Failure("Unable to create payment link.", "PAYMENT_LINK_ERROR");
        }

        var paymentTransaction = new PaymentTransaction
        {
            Id = Guid.NewGuid(),
            OrderID = order.Id,
            PaymentLinkId = paymentLink.PaymentLinkId,
            GatewayType = PaymentGatewayType.PayOS,
            TransactionType = TransactionType.OrderPayment,
            TransactionStatus = PaymentStatus.Pending,
            Amount = totalAmount,
            TransactionTimestamp = DateTime.UtcNow,
            TransactionLog = "Direct order payment transaction created",
            CreatedAt = DateTime.UtcNow
        };

        _context.Orders.Add(order);
        _context.PaymentTransactions.Add(paymentTransaction);
        foreach (var orderItem in orderItems)
            _context.OrderItems.Add(orderItem);

        await _context.SaveChangesAsync(cancellationToken);

        return Result<CreateDirectOrderResponse>.Success(new CreateDirectOrderResponse
        {
            OrderId = order.Id,
            PaymentTransactionId = paymentTransaction.Id,
            TotalAmount = totalAmount,
            PaymentLink = paymentLink.CheckoutUrl,
            OrderCode = paymentLink.OrderCode
        });
    }

    private static Result Validate(CreateDirectOrderCommand command)
    {
        if (command.ChildProfileId == Guid.Empty)
            return Result.Failure("ChildProfileId is required.", "MISSING_CHILD");
        if (command.SemesterPublicationId == Guid.Empty)
            return Result.Failure("SemesterPublicationId is required.", "MISSING_PUBLICATION");
        if (command.ProviderId == Guid.Empty)
            return Result.Failure("ProviderId is required.", "MISSING_PROVIDER");
        if (command.Items == null || command.Items.Count == 0)
            return Result.Failure("At least one order item is required.", "EMPTY_ITEMS");
        if (string.IsNullOrWhiteSpace(command.ShippingAddress))
            return Result.Failure("ShippingAddress is required.", "MISSING_SHIPPING_ADDRESS");
        if (command.Items.Any(i => i.ProductVariantId == Guid.Empty || i.Quantity <= 0))
            return Result.Failure("Each item must have a valid ProductVariantId and Quantity.", "INVALID_ITEM");

        return Result.Success();
    }

    private static OrderPricingMode ResolvePricingMode(DateTime publicationEndDateUtc, DateTime orderDateUtc)
    {
        return orderDateUtc > publicationEndDateUtc
            ? OrderPricingMode.PostDeadlineDirect
            : OrderPricingMode.PublicationWindow;
    }
}

public class CancelDirectOrderCommandHandler : ICancelDirectOrderCommandHandler
{
    private readonly IApplicationDbContext _context;
    private readonly IPayOSService _payOSService;

    public CancelDirectOrderCommandHandler(IApplicationDbContext context, IPayOSService payOSService)
    {
        _context = context;
        _payOSService = payOSService;
    }

    public async Task<Result> HandleAsync(CancelDirectOrderCommand command, CancellationToken cancellationToken = default)
    {
        var order = await _context.Orders
            .Include(o => o.ChildProfile)
            .Include(o => o.PaymentTransactions)
            .FirstOrDefaultAsync(
                o => o.Id == command.OrderId
                    && o.ProviderID != null
                    && o.SemesterPublicationID != null
                    && o.ChildProfile.ParentUserID == command.ParentId,
                cancellationToken);

        if (order == null)
            return Result.Failure("Direct order not found.", "ORDER_NOT_FOUND");

        if (order.OrderStatus == OrderStatus.Accepted
            || order.OrderStatus == OrderStatus.InProduction
            || order.OrderStatus == OrderStatus.ReadyToShip
            || order.OrderStatus == OrderStatus.Shipped
            || order.OrderStatus == OrderStatus.Delivered)
            return Result.Failure("Order cannot be cancelled after provider has started fulfillment.", "ORDER_NOT_CANCELLABLE");

        order.OrderStatus = OrderStatus.Cancelled;
        order.CancelReason = command.Reason;
        order.UpdatedAt = DateTime.UtcNow;

        var latestTransaction = order.PaymentTransactions
            .OrderByDescending(t => t.TransactionTimestamp)
            .FirstOrDefault();

        if (latestTransaction != null && latestTransaction.TransactionStatus == PaymentStatus.Pending)
        {
            latestTransaction.TransactionStatus = PaymentStatus.Cancelled;
            latestTransaction.TransactionLog = "Direct order payment cancelled by parent";

            if (!string.IsNullOrWhiteSpace(latestTransaction.PaymentLinkId))
            {
                try
                {
                    await _payOSService.CancelPaymentLinkAsync(latestTransaction.PaymentLinkId, cancellationToken);
                }
                catch
                {
                }
            }
        }

        await _context.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}

public class SubmitProviderRatingCommandHandler : ISubmitProviderRatingCommandHandler
{
    private readonly IApplicationDbContext _context;

    public SubmitProviderRatingCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result<SubmitProviderRatingResponse>> HandleAsync(SubmitProviderRatingCommand command, CancellationToken cancellationToken = default)
    {
        if (command.Rating < 1 || command.Rating > 5)
            return Result<SubmitProviderRatingResponse>.Failure("Rating must be between 1 and 5.", "INVALID_RATING");

        if (command.Comment != null && command.Comment.Length > 1000)
            return Result<SubmitProviderRatingResponse>.Failure("Comment cannot exceed 1000 characters.", "COMMENT_TOO_LONG");

        var order = await _context.Orders
            .Include(o => o.ChildProfile)
            .Include(o => o.OrderItems)
            .FirstOrDefaultAsync(
                o => o.Id == command.OrderId
                    && o.ProviderID != null
                    && o.SemesterPublicationID != null
                    && o.ChildProfile.ParentUserID == command.ParentId,
                cancellationToken);

        if (order == null)
            return Result<SubmitProviderRatingResponse>.Failure("Direct order not found.", "ORDER_NOT_FOUND");

        if (order.OrderStatus != OrderStatus.Delivered)
            return Result<SubmitProviderRatingResponse>.Failure("Provider can only be rated after delivery.", "ORDER_NOT_DELIVERED");

        var orderItemIds = order.OrderItems.Select(oi => oi.Id).ToList();
        if (orderItemIds.Count == 0)
            return Result<SubmitProviderRatingResponse>.Failure("Order does not contain any items.", "ORDER_ITEMS_NOT_FOUND");

        var existingFeedbacks = await _context.Feedbacks
            .Where(f => f.UserID == command.ParentId && orderItemIds.Contains(f.OrderItemID))
            .OrderByDescending(f => f.Timestamp)
            .ThenByDescending(f => f.Id)
            .ToListAsync(cancellationToken);

        var feedback = existingFeedbacks.FirstOrDefault();
        if (feedback == null)
        {
            var targetOrderItem = order.OrderItems
                .OrderBy(oi => oi.CreatedAt)
                .ThenBy(oi => oi.Id)
                .First();

            feedback = new Feedback
            {
                Id = Guid.NewGuid(),
                UserID = command.ParentId,
                OrderItemID = targetOrderItem.Id,
                Rating = command.Rating,
                Comment = command.Comment?.Trim(),
                Timestamp = DateTime.UtcNow,
                ModerationStatus = ModerationStatus.Pending,
                CreatedAt = DateTime.UtcNow
            };

            _context.Feedbacks.Add(feedback);
        }
        else
        {
            feedback.Rating = command.Rating;
            feedback.Comment = command.Comment?.Trim();
            feedback.Timestamp = DateTime.UtcNow;
            feedback.ModerationStatus = ModerationStatus.Pending;
            feedback.UpdatedAt = DateTime.UtcNow;
            _context.Feedbacks.Update(feedback);
        }

        await _context.SaveChangesAsync(cancellationToken);

        return Result<SubmitProviderRatingResponse>.Success(new SubmitProviderRatingResponse
        {
            ProviderRatingId = feedback.Id,
            OrderId = order.Id,
            ProviderId = order.ProviderID!.Value,
            Rating = feedback.Rating,
            Comment = feedback.Comment,
            CreatedAt = feedback.Timestamp
        });
    }
}
