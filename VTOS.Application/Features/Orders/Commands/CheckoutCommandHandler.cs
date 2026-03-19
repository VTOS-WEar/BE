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

            // Step 3: Fetch and validate product variants
            var productVariantsResult = await FetchAndValidateProductVariantsAsync(command.Items, cancellationToken);
            if (!string.IsNullOrEmpty(productVariantsResult.Error))
            {
                return Result<CheckoutResponse>.Failure(productVariantsResult.Error, "PRODUCTS_NOT_FOUND");
            }
            var productVariants = productVariantsResult.Variants!;

            // Step 3.5: If campaign is specified, validate that outfits are in the campaign
            Dictionary<Guid, decimal>? campaignOutfitPrices = null;
            if (command.CampaignId.HasValue && command.CampaignId != Guid.Empty)
            {
                var outfitValidationResult = await ValidateCampaignOutfitsAsync(command.CampaignId.Value, productVariants, cancellationToken);
                if (!outfitValidationResult.Result.IsSuccess)
                {
                    return Result<CheckoutResponse>.Failure(outfitValidationResult.Result.Error ?? "Validation failed", outfitValidationResult.Result.ErrorCode);
                }
                campaignOutfitPrices = outfitValidationResult.CampaignOutfitPrices;
            }

            // Step 4: Calculate total and create order items
            var (orderItems, totalAmount) = CalculateOrderItems(command.Items, productVariants, campaignOutfitPrices);

            if (totalAmount <= 0)
            {
                return Result<CheckoutResponse>.Failure("Invalid cart total", "INVALID_TOTAL_AMOUNT");
            }

            // Step 5: Create order
            var order = CreateOrder(command, totalAmount);
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

            // Step 6.5: Get Wallet ID if campaign is specified
            Guid? schoolWalletId = null;
            if (command.CampaignId.HasValue && command.CampaignId != Guid.Empty)
            {
                schoolWalletId = await GetWalletIdFromCampaignAsync(command.CampaignId.Value, cancellationToken);
            }

            // Step 7: Create payment transaction
            var paymentTransaction = CreatePaymentTransaction(order.Id, totalAmount, paymentLink.PaymentLinkId, schoolWalletId);
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
    private async Task<Result<CheckoutResponse>> ValidateChildProfileAsync(Guid childProfileId, Guid parentId, CancellationToken cancellationToken)
    {
        var childProfile = await _context.ChildProfiles
            .FirstOrDefaultAsync(cp => cp.Id == childProfileId, cancellationToken);

        if (childProfile == null)
        {
            return Result<CheckoutResponse>.Failure("Child profile not found", "CHILD_PROFILE_NOT_FOUND");
        }

        // Check if the child profile belongs to the parent
        if (childProfile.ParentUserID != parentId)
        {
            _logger.LogWarning("Unauthorized checkout attempt: Parent {ParentId} trying to checkout for child {ChildId} owned by {OwnerParentId}",
                parentId, childProfileId, childProfile.ParentUserID);
            return Result<CheckoutResponse>.Failure("Child profile does not belong to this parent", "UNAUTHORIZED_CHILD_ACCESS");
        }

        return Result<CheckoutResponse>.Success(null!);
    }

    /// <summary>
    /// Fetch and validate product variants exist
    /// </summary>
    private async Task<(string? Error, List<ProductVariant>? Variants)> FetchAndValidateProductVariantsAsync(
        List<CheckoutItemRequest> items,
        CancellationToken cancellationToken)
    {
        var productVariantIds = items.Select(i => i.ProductVariantId).ToList();
        var productVariants = await _context.ProductVariants
            .Where(pv => productVariantIds.Contains(pv.Id))
            .ToListAsync(cancellationToken);

        if (productVariants.Count != items.Count)
        {
            return ("One or more products not found", null);
        }

        return (null, productVariants);
    }

    /// <summary>
    /// Validate that product variants belong to the campaign (via their outfits)
    /// Returns campaign outfit prices for each outfit
    /// </summary>
    private async Task<(Result<CheckoutResponse> Result, Dictionary<Guid, decimal>? CampaignOutfitPrices)> ValidateCampaignOutfitsAsync(
        Guid campaignId,
        List<ProductVariant> productVariants,
        CancellationToken cancellationToken)
    {
        // Get all outfit IDs from product variants
        var outfitIds = productVariants.Select(pv => pv.OutfitID).Distinct().ToList();

        // Fetch campaign outfits with prices
        var campaignOutfits = await _context.CampaignOutfits
            .Where(co => co.CampaignID == campaignId && outfitIds.Contains(co.OutfitID))
            .Select(co => new { co.OutfitID, co.CampaignPrice })
            .ToListAsync(cancellationToken);

        // Check if all outfits are in the campaign
        var foundOutfitIds = campaignOutfits.Select(co => co.OutfitID).ToList();
        var missingOutfits = outfitIds.Except(foundOutfitIds).ToList();

        if (missingOutfits.Count > 0)
        {
            _logger.LogWarning("Campaign {CampaignId} does not contain outfits: {MissingOutfits}",
                campaignId, string.Join(", ", missingOutfits));
            return (
                Result<CheckoutResponse>.Failure(
                    "One or more products are not available in this campaign",
                    "OUTFIT_NOT_IN_CAMPAIGN"),
                null);
        }

        // Build price dictionary: OutfitID -> CampaignPrice
        var priceMap = campaignOutfits.ToDictionary(co => co.OutfitID, co => co.CampaignPrice);

        return (Result<CheckoutResponse>.Success(null!), priceMap);
    }

    /// <summary>
    /// Calculate order items and total amount
    /// If campaign outfit prices provided, use those; otherwise use product variant prices
    /// </summary>
    private (List<OrderItem>, decimal) CalculateOrderItems(
        List<CheckoutItemRequest> items,
        List<ProductVariant> productVariants,
        Dictionary<Guid, decimal>? campaignOutfitPrices = null)
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

            // Determine price: campaign outfit price if available, otherwise product variant price
            decimal unitPrice = productVariant.Price;
            if (campaignOutfitPrices != null && campaignOutfitPrices.TryGetValue(productVariant.OutfitID, out var campaignPrice))
            {
                unitPrice = campaignPrice;
                _logger.LogDebug("Using campaign outfit price {CampaignPrice} for outfit {OutfitId}", campaignPrice, productVariant.OutfitID);
            }

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
    private Order CreateOrder(CheckoutCommand command, decimal totalAmount)
    {
        return new Order
        {
            Id = Guid.NewGuid(),
            ChildProfileID = command.ChildProfileId,
            OrderDate = DateTime.UtcNow,
            OrderStatus = OrderStatus.Pending,
            TotalAmount = totalAmount,
            ShippingAddress = command.ShippingAddress,
            CampaignID = command.CampaignId,
            DeliveryMethod = command.DeliveryMethod,
            CreatedAt = DateTime.UtcNow
        };
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
    private PaymentTransaction CreatePaymentTransaction(Guid orderId, decimal totalAmount, string paymentLinkId, Guid? schoolWalletId = null)
    {
        return new PaymentTransaction
        {
            Id = Guid.NewGuid(),
            OrderID = orderId,
            WalletID = schoolWalletId,
            PaymentLinkId = paymentLinkId,
            GatewayType = PaymentGatewayType.PayOS,
            TransactionStatus = PaymentStatus.Pending,
            Amount = totalAmount,
            TransactionTimestamp = DateTime.UtcNow,
            TransactionLog = "Payment transaction created for checkout",
            CreatedAt = DateTime.UtcNow
        };
    }

    /// <summary>
    /// Get Wallet ID from Campaign
    /// </summary>
    private async Task<Guid?> GetWalletIdFromCampaignAsync(Guid campaignId, CancellationToken cancellationToken)
    {
        try
        {
            var walletId = await (
                from c in _context.Campaigns
                join w in _context.Wallets on c.SchoolID equals w.OwnerID
                where c.Id == campaignId && w.OwnerType == Domain.Enums.WalletOwnerType.School && w.IsActive
                select w.Id
            ).FirstOrDefaultAsync(cancellationToken);

            if (walletId != Guid.Empty)
            {
                _logger.LogDebug("Retrieved Wallet {WalletId} from Campaign {CampaignId}", walletId, campaignId);
                return walletId;
            }

            _logger.LogWarning("No Wallet found for Campaign {CampaignId}", campaignId);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving Wallet from Campaign {CampaignId}", campaignId);
            return null;
        }
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

        return (string.Empty, string.Empty);
    }
    #endregion
}
