namespace VTOS.Application.Features.Users.DTOs;

public record AddParentBankAccountRequest(
    string BankName,
    string? BankCode,
    string AccountNumber,
    string AccountHolderName,
    bool IsDefault = false);
