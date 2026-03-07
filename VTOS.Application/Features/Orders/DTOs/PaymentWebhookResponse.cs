namespace VTOS.Application.Features.Orders.DTOs;

/// <summary>
/// PayOS payment webhook response data
/// </summary>
public class PaymentWebhookData
{
    public string? AccountNumber { get; set; }
    public long Amount { get; set; }
    public string? Description { get; set; }
    public string? Reference { get; set; }
    public string? TransactionDateTime { get; set; }
    public string? VirtualAccountNumber { get; set; }
    public string? CounterAccountBankId { get; set; }
    public string? CounterAccountBankName { get; set; }
    public string? CounterAccountName { get; set; }
    public string? CounterAccountNumber { get; set; }
    public string? VirtualAccountName { get; set; }
    public string? Currency { get; set; }
    public int OrderCode { get; set; }
    public string? PaymentLinkId { get; set; }
    public string? Code { get; set; }
    public string? Desc { get; set; }
}

/// <summary>
/// PayOS payment webhook response
/// </summary>
public class PaymentWebhookResponse
{
    public string? Code { get; set; }
    public string? Desc { get; set; }
    public bool Success { get; set; }
    public PaymentWebhookData? Data { get; set; }
    public string? Signature { get; set; }
}

/// <summary>
/// Response for payment webhook endpoint
/// </summary>
public record PaymentWebhookProcessResponse(string Message);
