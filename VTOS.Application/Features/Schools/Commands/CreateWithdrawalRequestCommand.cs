namespace VTOS.Application.Features.Schools.Commands;

public class WithdrawalRequestResponse
{
    public Guid WithdrawalRequestId { get; set; }
    public Guid WalletId { get; set; }
    public decimal Amount { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime RequestedAt { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public DateTime? PaidAt { get; set; }
    public string? AdminNote { get; set; }
}
