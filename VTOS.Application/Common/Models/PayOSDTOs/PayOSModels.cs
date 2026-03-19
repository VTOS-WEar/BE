namespace VTOS.Application.Common.Models.PayOSDTOs;

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

#region Payout Models

/// <summary>
/// Response for payout account balance inquiry
/// GET /v1/payouts-account/balance
/// </summary>
public class PayoutAccountDetailResponse
{
    public string? AccountNumber { get; set; }
    public string? AccountName { get; set; }
    public string? Currency { get; set; }
    public string? Balance { get; set; }
}

/// <summary>
/// Query parameters for listing payouts
/// GET /v1/payouts
/// </summary>
public class PayoutListQuery
{
    public int Limit { get; set; } = 10;
    public int Offset { get; set; } = 0;
    public string? ReferenceId { get; set; }
    public string? ApprovalState { get; set; }
    public string? Category { get; set; }
    public string? FromDate { get; set; }
    public string? ToDate { get; set; }
}

/// <summary>
/// Response for listing payouts
/// </summary>
public class PayoutListResponse
{
    public List<PayoutDetailResponse>? Data { get; set; }
    public int? Total { get; set; }
    public int? Limit { get; set; }
    public int? Offset { get; set; }
}

/// <summary>
/// Individual transaction within a payout
/// </summary>
public class PayoutTransaction
{
    public string? Id { get; set; }
    public string? ReferenceId { get; set; }
    public long Amount { get; set; }
    public string? Description { get; set; }
    public string? ToBin { get; set; }
    public string? ToAccountNumber { get; set; }
    public string? ToAccountName { get; set; }
    public string? State { get; set; }
}

/// <summary>
/// Response for payout detail
/// GET /v1/payouts/{payoutId}
/// </summary>
public class PayoutDetailResponse
{
    public string? Id { get; set; }
    public string? ReferenceId { get; set; }
    public List<PayoutTransaction>? Transactions { get; set; }
    public List<string>? Category { get; set; }
    public string? ApprovalState { get; set; }
    public string? CreatedAt { get; set; }
}

/// <summary>
/// Request body for creating a payout
/// POST /v1/payouts
/// </summary>
public class CreatePayoutRequest
{
    public string ReferenceId { get; set; } = string.Empty;
    public long Amount { get; set; }
    public string Description { get; set; } = string.Empty;
    public string ToBin { get; set; } = string.Empty;
    public string ToAccountNumber { get; set; } = string.Empty;
    public List<string>? Category { get; set; }
}

/// <summary>
/// Response for creating a payout (same structure as payout detail)
/// POST /v1/payouts
/// </summary>
public class CreatePayoutResponse
{
    public string? Id { get; set; }
    public string? ReferenceId { get; set; }
    public List<PayoutTransaction>? Transactions { get; set; }
    public List<string>? Category { get; set; }
    public string? ApprovalState { get; set; }
    public string? CreatedAt { get; set; }
}

#endregion
