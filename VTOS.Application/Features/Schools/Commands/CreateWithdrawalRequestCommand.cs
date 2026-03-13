using VTOS.Application.Common;

namespace VTOS.Application.Features.Schools.Commands;

public record CreateWithdrawalRequestCommand(Guid SchoolUserId, decimal Amount);

public interface ICreateWithdrawalRequestCommandHandler
{
    Task<Result<WithdrawalRequestResponse>> HandleAsync(CreateWithdrawalRequestCommand command, CancellationToken ct = default);
}

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
