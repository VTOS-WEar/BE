using VTOS.Application.Features.Admin.DTOs;

namespace VTOS.Application.Features.Admin.Queries;

public record GetPaymentCompletionRateQuery(
    DateTime? DateFrom = null,
    DateTime? DateTo = null
);

public interface IGetPaymentCompletionRateQueryHandler
{
    Task<PaymentCompletionRateDto> HandleAsync(
        GetPaymentCompletionRateQuery query,
        CancellationToken cancellationToken);
}
