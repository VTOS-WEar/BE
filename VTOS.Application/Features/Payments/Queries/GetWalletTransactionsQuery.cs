namespace VTOS.Application.Features.Payments.Queries;

public record WalletDto(
    Guid WalletId,
    decimal Balance,
    string? BankCode,
    string? BankName,
    string? BankAccountNumber,
    string? BankAccountName,
    bool IsActive,
    DateTime UpdatedAt
);

public record WalletTransactionDto(
    Guid PaymentId,
    string TransactionType,
    decimal Amount,
    string Status,
    string? Description,
    DateTime Timestamp
);

public record WalletTransactionsResponse(List<WalletTransactionDto> Items, int Total);
