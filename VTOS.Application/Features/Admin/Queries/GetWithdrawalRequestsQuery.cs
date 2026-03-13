namespace VTOS.Application.Features.Admin.Queries;

/// <summary>
/// Query for admin to get all withdrawal requests with optional status filter.
/// </summary>
public record GetWithdrawalRequestsQuery(
    int Page = 1,
    int PageSize = 10,
    string? Status = null);

public interface IGetWithdrawalRequestsQueryHandler
{
    Task<WithdrawalRequestListResponse> HandleAsync(GetWithdrawalRequestsQuery query, CancellationToken ct = default);
}

public class WithdrawalRequestListResponse
{
    public List<WithdrawalRequestDto> Items { get; set; } = new();
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
}

public class WithdrawalRequestDto
{
    public Guid WithdrawalRequestId { get; set; }
    public Guid WalletId { get; set; }
    public Guid SchoolId { get; set; }
    public string SchoolName { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? BankCode { get; set; }
    public string? BankName { get; set; }
    public string? BankAccountNumber { get; set; }
    public string? BankAccountName { get; set; }
    public DateTime RequestedAt { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public DateTime? PaidAt { get; set; }
    public string? AdminNote { get; set; }
}
