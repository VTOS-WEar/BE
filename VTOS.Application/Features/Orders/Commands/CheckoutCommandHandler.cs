
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

/// <summary>
/// Handler for checkout command that validates items, calculates total price,
/// creates Order (status = PENDING) and PaymentTransaction (status = PENDING)
/// </summary>
public class CheckoutCommandHandler : ICheckoutCommandHandler
{
    private readonly IApplicationDbContext _context;
    private readonly IPayOSService _payOSService;
    private readonly ILogger<CheckoutCommandHandler> _logger;
    private readonly PaymentSettings _paymentSettings;

    public CheckoutCommandHandler(
        IApplicationDbContext context,
        IPayOSService payOSService,
        ILogger<CheckoutCommandHandler> logger,
        IOptions<PaymentSettings> paymentSettings)
    {
        _context = context;
        _payOSService = payOSService;
        _logger = logger;
        _paymentSettings = paymentSettings.Value;
    }

    public async Task<Result<CheckoutResponse>> HandleAsync(
        CheckoutCommand command,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Step 1: Validate checkout command
            var validationError = ValidateCheckoutCommand(command);
            if (!string.IsNullOrEmpty(validationError.Message))
            {
                return Result<CheckoutResponse>.Failure(validationError.Message, validationError.ErrorCode);
            }

            // Step 2: Validate child profile
            var childProfileResult = await ValidateChildProfileAsync(command.ChildProfileId, command.ParentId, cancellationToken);
            if (!childProfileResult.IsSuccess)
            {
                return Result<CheckoutResponse>.Failure(childProfileResult.Error ?? "Validation failed", childProfileResult.ErrorCode);
            }
            var childProfile = childProfileResult.Value!;

            // Step 3: Resolve immutable publication/provider pricing context
            var checkoutContextResult = await ResolveCheckoutContextAsync(command, cancellationToken);
            if (!checkoutContextResult.IsSuccess)
            {
                return Result<CheckoutResponse>.Failure(checkoutContextResult.Error!, checkoutContextResult.ErrorCode);
            }
            var checkoutContext = checkoutContextResult.Value!;

            // Step 4: Calculate total and create order items
            var (orderItems, totalAmount) = CalculateOrderItems(command.Items, checkoutContext.ProductVariants, checkoutContext.PricingByOutfit, checkoutContext.PricingMode);

            if (totalAmount <= 0)
            {
                return Result<CheckoutResponse>.Failure("Invalid cart total", "INVALID_TOTAL_AMOUNT");
            }

            // Step 5: Create order
            var order = CreateOrder(command, totalAmount, checkoutContext.ProviderId, checkoutContext.SemesterPublicationId, checkoutContext.PricingMode);
            AssignOrderToItems(orderItems, order.Id);
            _context.Orders.Add(order);

            // Step 6: Generate PayOS payment link
            var paymentLinkResult = await GeneratePaymentLinkAsync(totalAmount, cancellationToken);
            if (!string.IsNullOrEmpty(paymentLinkResult.Error))
            {
                return Result<CheckoutResponse>.Failure(paymentLinkResult.Error, "PAYMENT_LINK_ERROR");
            }
            var paymentLink = paymentLinkResult.PaymentLink!;
            var orderCode = paymentLinkResult.OrderCode;

            // Step 7: Create payment transaction
            var paymentTransaction = CreatePaymentTransaction(order.Id, totalAmount, paymentLink.PaymentLinkId);
            _context.PaymentTransactions.Add(paymentTransaction);

            // Step 8: Add order items to context and save
            foreach (var orderItem in orderItems)
            {
                _context.OrderItems.Add(orderItem);
            }

            await _context.SaveChangesAsync(cancellationToken);

            // Build response
            var response = new CheckoutResponse
            {
                OrderId = order.Id,
                PaymentTransactionId = paymentTransaction.Id,
                TotalAmount = totalAmount,
                PaymentLink = paymentLink.CheckoutUrl,
                OrderCode = orderCode
            };

            _logger.LogInformation("Checkout completed successfully: OrderId={OrderId}", order.Id);
            return Result<CheckoutResponse>.Success(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during checkout process");
            return Result<CheckoutResponse>.Failure($"Checkout failed: {ex.Message}", "CHECKOUT_ERROR");
        }
    }

    #region

    /// <summary>
    /// Validate child profile exists and belongs to the parent
    /// </summary>
    private async Task<Result<ChildProfile>> ValidateChildProfileAsync(Guid childProfileId, Guid parentId, CancellationToken cancellationToken)
    {
        var childProfile = await _context.ChildProfiles
            .FirstOrDefaultAsync(cp => cp.Id == childProfileId, cancellationToken);

        if (childProfile == null)
        {
            return Result<ChildProfile>.Failure("Child profile not found", "CHILD_PROFILE_NOT_FOUND");
        }

        // Check if the child profile belongs to the parent
        if (childProfile.ParentUserID != parentId)
        {
            _logger.LogWarning("Unauthorized checkout attempt: Parent {ParentId} trying to checkout for child {ChildId} owned by {OwnerParentId}",
                parentId, childProfileId, childProfile.ParentUserID);
            return Result<ChildProfile>.Failure("Child profile does not belong to this parent", "UNAUTHORIZED_CHILD_ACCESS");
        }

        return Result<ChildProfile>.Success(childProfile);
    }

    private async Task<Result<CheckoutContext>> ResolveCheckoutContextAsync(
        CheckoutCommand command,
        CancellationToken cancellationToken)
    {
        if (!command.CampaignId.HasValue || command.CampaignId.Value == Guid.Empty)
        {
            return Result<CheckoutContext>.Failure(
                "Semester publication is required for checkout.",
                "MISSING_PUBLICATION");
        }

        var publication = await _context.SemesterPublications
            .AsNoTracking()
            .FirstOrDefaultAsync(
                sp => sp.Id == command.CampaignId.Value && sp.Status != SemesterPublicationStatus.Draft,
                cancellationToken);

        if (publication == null)
        {
            return Result<CheckoutContext>.Failure(
                "Semester publication is not available for ordering.",
                "PUBLICATION_NOT_AVAILABLE");
        }

        var productVariantIds = command.Items.Select(i => i.ProductVariantId).Distinct().ToList();
        var productVariants = await _context.ProductVariants
            .Include(pv => pv.Outfit)
            .Where(pv => productVariantIds.Contains(pv.Id) && !pv.IsDeleted)
            .ToListAsync(cancellationToken);

        if (productVariants.Count != productVariantIds.Count)
        {
            return Result<CheckoutContext>.Failure("One or more products not found.", "PRODUCTS_NOT_FOUND");
        }

        var outfitIds = productVariants.Select(pv => pv.OutfitID).Distinct().ToList();

        var publishedOutfitIds = await _context.SemesterPublicationOutfits
            .AsNoTracking()
            .Where(spo => spo.SemesterPublicationID == publication.Id && outfitIds.Contains(spo.OutfitID))
            .Select(spo => spo.OutfitID)
            .Distinct()
            .ToListAsync(cancellationToken);

        if (publishedOutfitIds.Count != outfitIds.Count)
        {
            return Result<CheckoutContext>.Failure(
                "One or more outfits are not available in this semester publication.",
                "OUTFIT_NOT_IN_PUBLICATION");
        }

        var candidateProviders = await _context.ProviderCatalogItems
            .AsNoTracking()
            .Where(ci =>
                outfitIds.Contains(ci.OutfitID) &&
                (ci.Status == ProviderCatalogItemStatus.Published || ci.Status == ProviderCatalogItemStatus.Ready) &&
                ci.SemesterPublicationProvider.SemesterPublicationID == publication.Id &&
                ci.SemesterPublicationProvider.Status == SemPublicationProviderStatus.Active)
            .Select(ci => ci.ProviderID)
            .Distinct()
            .ToListAsync(cancellationToken);

        if (candidateProviders.Count != 1)
        {
            return Result<CheckoutContext>.Failure(
                "Checkout items must belong to exactly one provider in the selected semester publication.",
                "AMBIGUOUS_PROVIDER");
        }

        var providerId = candidateProviders[0];
        var publicationProvider = await _context.SemesterPublicationProviders
            .AsNoTracking()
            .FirstOrDefaultAsync(
                spp => spp.SemesterPublicationID == publication.Id
                    && spp.ProviderID == providerId
                    && spp.Status == SemPublicationProviderStatus.Active,
                cancellationToken);

        if (publicationProvider == null)
        {
            return Result<CheckoutContext>.Failure(
                "Provider is not approved for this semester publication.",
                "PROVIDER_NOT_APPROVED");
        }

        var catalogItems = await _context.ProviderCatalogItems
            .AsNoTracking()
            .Where(ci =>
                ci.SemesterPublicationProviderID == publicationProvider.Id
                && ci.ProviderID == providerId
                && outfitIds.Contains(ci.OutfitID)
                && (ci.Status == ProviderCatalogItemStatus.Published || ci.Status == ProviderCatalogItemStatus.Ready))
            .ToListAsync(cancellationToken);

        var contractItems = publicationProvider.ContractID.HasValue
            ? await _context.ContractItems
                .AsNoTracking()
                .Where(ci => ci.ContractID == publicationProvider.ContractID.Value && outfitIds.Contains(ci.OutfitID))
                .ToListAsync(cancellationToken)
            : new List<ContractItem>();

        var pricingMode = ResolvePricingMode(publication.EndDate, DateTime.UtcNow);
        var pricingByOutfit = new Dictionary<Guid, (decimal PublicationPrice, decimal PostDeadlinePrice)>();

        foreach (var outfitId in outfitIds)
        {
            var catalogItem = catalogItems.FirstOrDefault(ci => ci.OutfitID == outfitId);
            if (catalogItem != null)
            {
                if (catalogItem.PostDeadlinePrice < catalogItem.PublicationPrice)
                {
                    return Result<CheckoutContext>.Failure(
                        "Provider listing data is invalid because post-deadline price is lower than publication price.",
                        "INVALID_CATALOG_PRICING");
                }

                pricingByOutfit[outfitId] = (catalogItem.PublicationPrice, catalogItem.PostDeadlinePrice);
                continue;
            }

            var contractItem = contractItems.FirstOrDefault(ci => ci.OutfitID == outfitId);
            if (contractItem != null)
            {
                pricingByOutfit[outfitId] = (contractItem.PricePerUnit, contractItem.PricePerUnit);
                continue;
            }

            return Result<CheckoutContext>.Failure(
                "One or more outfits are not available for this provider in the selected semester publication.",
                "CATALOG_ITEM_NOT_AVAILABLE");
        }

        return Result<CheckoutContext>.Success(new CheckoutContext(
            publication.Id,
            providerId,
            pricingMode,
            productVariants,
            pricingByOutfit));
    }

    /// <summary>
    /// Calculate order items and total amount using variant prices.
    /// </summary>
    private (List<OrderItem>, decimal) CalculateOrderItems(
        List<CheckoutItemRequest> items,
        List<ProductVariant> productVariants,
        Dictionary<Guid, (decimal PublicationPrice, decimal PostDeadlinePrice)> pricingByOutfit,
        OrderPricingMode pricingMode)
    {
        var orderItems = new List<OrderItem>();
        decimal totalAmount = 0;

        foreach (var item in items)
        {
            var productVariant = productVariants.FirstOrDefault(pv => pv.Id == item.ProductVariantId);
            if (productVariant == null)
            {
                continue;
            }

            var pricing = pricingByOutfit[productVariant.OutfitID];
            decimal unitPrice = pricingMode == OrderPricingMode.PostDeadlineDirect
                ? pricing.PostDeadlinePrice
                : pricing.PublicationPrice;

            var itemTotal = unitPrice * item.Quantity;
            totalAmount += itemTotal;

            var orderItem = new OrderItem
            {
                Id = Guid.NewGuid(),
                ProductVariantID = item.ProductVariantId,
                Quantity = item.Quantity,
                UnitPrice = unitPrice,
                SizeOrdered = productVariant.Size,
                CreatedAt = DateTime.UtcNow
            };

            orderItems.Add(orderItem);
        }

        return (orderItems, totalAmount);
    }

    /// <summary>
    /// Create order entity with pending status from CheckoutCommand
    /// </summary>
    private Order CreateOrder(
        CheckoutCommand command,
        decimal totalAmount,
        Guid providerId,
        Guid semesterPublicationId,
        OrderPricingMode pricingMode)
    {
        return new Order
        {
            Id = Guid.NewGuid(),
            ChildProfileID = command.ChildProfileId,
            OrderDate = DateTime.UtcNow,
            OrderStatus = OrderStatus.Pending,
            TotalAmount = totalAmount,
            ShippingAddress = command.ShippingAddress,
            ProviderID = providerId,
            SemesterPublicationID = semesterPublicationId,
            AppliedPricingMode = pricingMode,
            DeliveryMethod = command.DeliveryMethod,
            CreatedAt = DateTime.UtcNow
        };
    }

    private static OrderPricingMode ResolvePricingMode(DateTime publicationEndDateUtc, DateTime orderDateUtc)
    {
        return orderDateUtc > publicationEndDateUtc
            ? OrderPricingMode.PostDeadlineDirect
            : OrderPricingMode.PublicationWindow;
    }

    /// <summary>
    /// Assign order ID to all order items
    /// </summary>
    private static void AssignOrderToItems(List<OrderItem> orderItems, Guid orderId)
    {
        foreach (var item in orderItems)
        {
            item.OrderID = orderId;
        }
    }

    /// <summary>
    /// Generate PayOS payment link
    /// </summary>
    private async Task<(string? Error, CreatePaymentLinkResponse? PaymentLink, int OrderCode)> GeneratePaymentLinkAsync(
        decimal totalAmount,
        CancellationToken cancellationToken)
    {
        try
        {
            var returnUrl = $"{_paymentSettings.ReturnBaseUrl}{_paymentSettings.ReturnSuccessPath}";
            var cancelUrl = $"{_paymentSettings.ReturnBaseUrl}{_paymentSettings.ReturnCancelPath}";

            var paymentLinkRequest = new CreatePaymentLinkRequest
            {
                Amount = (int)totalAmount,
                ReturnUrl = returnUrl,
                CancelUrl = cancelUrl,
            };

            var paymentLinkResponse = await _payOSService.CreatePayOSPaymentLinkAsync(paymentLinkRequest, cancellationToken);
            return (null, paymentLinkResponse,paymentLinkResponse.OrderCode);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating PayOS payment link");
            return (ex.Message, null, 0);
        }
    }

    /// <summary>
    /// Create payment transaction with pending status
    /// </summary>
    private PaymentTransaction CreatePaymentTransaction(Guid orderId, decimal totalAmount, string paymentLinkId)
    {
        return new PaymentTransaction
        {
            Id = Guid.NewGuid(),
            OrderID = orderId,
            WalletID = null,
            PaymentLinkId = paymentLinkId,
            GatewayType = PaymentGatewayType.PayOS,
            TransactionType = TransactionType.OrderPayment,
            TransactionStatus = PaymentStatus.Pending,
            Amount = totalAmount,
            TransactionTimestamp = DateTime.UtcNow,
            TransactionLog = "Payment transaction created for checkout",
            CreatedAt = DateTime.UtcNow
        };
    }

    /// <summary>
    /// Validate checkout command
    /// </summary>
    private static (string Message, string ErrorCode) ValidateCheckoutCommand(CheckoutCommand command)
    {
        if (command == null)
        {
            return ("Checkout command cannot be empty", "INVALID_REQUEST");
        }

        if (command.ChildProfileId == Guid.Empty)
        {
            return ("Child profile ID is required", "MISSING_CHILD_PROFILE_ID");
        }

        if (command.Items == null || command.Items.Count == 0)
        {
            return ("At least one item is required", "EMPTY_CART");
        }

        foreach (var item in command.Items)
        {
            if (item.ProductVariantId == Guid.Empty)
            {
                return ("Product variant ID is required for all items", "MISSING_PRODUCT_ID");
            }

            if (item.Quantity <= 0)
            {
                return ("Item quantity must be greater than 0", "INVALID_QUANTITY");
            }
        }

        if (string.IsNullOrWhiteSpace(command.ShippingAddress))
        {
            return ("Shipping address is required", "MISSING_ADDRESS");
        }

        if (!command.CampaignId.HasValue || command.CampaignId.Value == Guid.Empty)
        {
            return ("Semester publication is required", "MISSING_PUBLICATION");
        }

        return (string.Empty, string.Empty);
    }
    #endregion
}

internal sealed record CheckoutContext(
    Guid SemesterPublicationId,
    Guid ProviderId,
    OrderPricingMode PricingMode,
    List<ProductVariant> ProductVariants,
    Dictionary<Guid, (decimal PublicationPrice, decimal PostDeadlinePrice)> PricingByOutfit);
