using VTOS.Application.Features.Admin.DTOs;

namespace VTOS.Application.Features.Admin.Queries;

public record MonitorPaymentTransactionsQuery(
    DateTime? DateFrom = null,
    DateTime? DateTo = null, 
    string? Status = null,
    string? PaymentGateway = null,
    int Page = 1,
    int PageSize = 10
);

public interface IMonitorPaymentTransactionsQueryHandler
{
    Task<PaginatedResult<PaymentTransactionDto>> HandleAsync(
        MonitorPaymentTransactionsQuery query,
        CancellationToken cancellationToken);
}
