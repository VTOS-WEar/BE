using VTOS.Application.Common;

namespace VTOS.Application.Features.Admin.Commands;

public record ConfigurePaymentMethodCommand(
    string PaymentGateway,  // "PayOS", "VNPay", "MoMo", "Wallet"
    bool IsEnabled,
    string? ApiKey = null,
    string? SecretKey = null
);

public interface IConfigurePaymentMethodCommandHandler
{
    Task<Result<string>> HandleAsync(
        ConfigurePaymentMethodCommand command,
        CancellationToken cancellationToken);
}
