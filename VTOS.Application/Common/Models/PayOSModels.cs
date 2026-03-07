namespace VTOS.Application.Common.Models;

public class CreatePaymentLinkRequest
{
    public int Amount { get; set; }
    public string Description { get; set; } = string.Empty;
    public string ReturnUrl { get; set; } = string.Empty;
    public string CancelUrl { get; set; } = string.Empty;
}

public class CreatePaymentLinkResponse
{
    public string CheckoutUrl { get; set; } = string.Empty;
    public int OrderCode { get; set; }
    public string PaymentLinkId { get; set; }= string.Empty;
    public string Signature { get; set; } = string.Empty;
    public object? Raw { get; set; }
}

public class PayOSTransaction
{
    public string? Reference { get; set; }
    public long Amount { get; set; }
    public string? TransactionDateTime { get; set; }
    public string? Status { get; set; }
}

public class GetPaymentLinkInfoResponse
{
    public string? Id { get; set; }
    public int OrderCode { get; set; }
    public long Amount { get; set; }
    public long AmountPaid { get; set; }
    public long AmountRemaining { get; set; }
    public string? Status { get; set; }
    public string? CreatedAt { get; set; }
    public List<PayOSTransaction>? Transactions { get; set; }
}

public class CancelPaymentLinkResponse
{
    public string? Id { get; set; }
    public int OrderCode { get; set; }
    public long Amount { get; set; }
    public long AmountPaid { get; set; }
    public long AmountRemaining { get; set; }
    public string? Status { get; set; }
    public string? CreatedAt { get; set; }
    public string? CanceledAt { get; set; }
    public string? CancellationReason { get; set; }
    public List<PayOSTransaction>? Transactions { get; set; }
}

public class PayOSInvoice
{
    public string? InvoiceId { get; set; }
    public string? InvoiceNumber { get; set; }
    public long IssuedTimestamp { get; set; }
    public string? IssuedDatetime { get; set; }
    public string? TransactionId { get; set; }
    public string? ReservationCode { get; set; }
    public string? CodeOfTax { get; set; }
}

public class GetPaymentInvoicesResponse
{
    public List<PayOSInvoice>? Invoices { get; set; }
}


