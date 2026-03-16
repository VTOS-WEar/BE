namespace VTOS.Application.Features.Admin.DTOs;

public record PaymentCompletionRateDto(
    int TotalPaymentAttempts,
    int CompletedPayments,
    int FailedPayments,
    decimal CompletionRate,
    List<PaymentStatusBreakdownDto> PaymentsByStatus
);

public record PaymentStatusBreakdownDto(
    string Status,
    int Count,
    decimal Percentage
);
