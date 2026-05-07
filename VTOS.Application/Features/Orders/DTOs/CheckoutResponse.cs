namespace VTOS.Application.Features.Orders.DTOs;

/// <summary>
/// Response returned after successful checkout
/// </summary>
public class CheckoutResponse
{
    /// <summary>
    /// Order ID created
    /// </summary>
    public Guid OrderId { get; set; }

    /// <summary>
    /// Payment transaction ID created
    /// </summary>
    public Guid PaymentTransactionId { get; set; }

    /// <summary>
    /// Total amount to be paid
    /// </summary>
    public decimal TotalAmount { get; set; }

    /// <summary>
    /// Shipping fee included in total amount
    /// </summary>
    public decimal ShippingFee { get; set; }

    /// <summary>
    /// Payment link from PayOS for checkout
    /// </summary>
    public string PaymentLink { get; set; } = string.Empty;

    /// <summary>
    /// Order code generated for payment
    /// </summary>
    public int OrderCode { get; set; }
}
