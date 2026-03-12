using VTOS.Application.Common;

namespace VTOS.Application.Features.Schools.Commands;

public record UpdateSchoolBankAccountCommand(
    Guid SchoolUserId,
    string? BankCode,
    string? BankName,
    string? BankAccountNumber,
    string? BankAccountName);

public interface IUpdateSchoolBankAccountCommandHandler
{
    Task<Result<SchoolBankAccountResponse>> HandleAsync(UpdateSchoolBankAccountCommand command, CancellationToken ct = default);
}

public class SchoolBankAccountResponse
{
    public Guid WalletId { get; set; }
    public string? BankCode { get; set; }
    public string? BankName { get; set; }
    public string? BankAccountNumber { get; set; }
    public string? BankAccountName { get; set; }
}
