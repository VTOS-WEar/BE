namespace VTOS.Application.Features.Admin.DTOs;

public record PaymentTransactionDto(
    Guid PaymentId,
    Guid? OrderId,
    string PaymentGateway,
    string Status,
    decimal Amount,
    DateTime TransactionTimestamp,
    DateTime CreatedAt
);
