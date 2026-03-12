using VTOS.Application.Common;

namespace VTOS.Application.Features.Users.Commands;

public record AddParentBankAccountCommand(
    Guid ParentUserId,
    string BankName,
    string? BankCode,
    string AccountNumber,
    string AccountHolderName,
    bool IsDefault);

public interface IAddParentBankAccountCommandHandler
{
    Task<Result<ParentBankAccountResponse>> HandleAsync(AddParentBankAccountCommand command, CancellationToken ct = default);
}

public class ParentBankAccountResponse
{
    public Guid BankAccountId { get; set; }
    public string BankName { get; set; } = string.Empty;
    public string? BankCode { get; set; }
    public string AccountNumber { get; set; } = string.Empty;
    public string AccountHolderName { get; set; } = string.Empty;
    public bool IsDefault { get; set; }
    public bool IsVerified { get; set; }
}
