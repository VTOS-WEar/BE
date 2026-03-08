using VTOS.Application.Common.Models;

namespace VTOS.Application.Abstractions;

public interface IPayOSService
{
    /// <summary>
    /// Create PayOS payment link for customer payment
    /// </summary>
    /// <param name="input">Payment link parameters</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Payment link result</returns>
    Task<CreatePaymentLinkResponse> CreatePayOSPaymentLinkAsync(CreatePaymentLinkRequest input, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get payment link information from PayOS
    /// </summary>
    /// <param name="paymentLinkId">Payment request ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Payment link information</returns>
    Task<GetPaymentLinkInfoResponse> GetPaymentLinkInfoAsync(string paymentLinkId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Cancel a payment link on PayOS
    /// </summary>
    /// <param name="paymentLinkId">Payment request ID to cancel</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Cancelled payment link information</returns>
    Task<CancelPaymentLinkResponse> CancelPaymentLinkAsync(string paymentLinkId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get payment invoices from PayOS
    /// </summary>
    /// <param name="paymentLinkId">Payment request ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Payment invoices information</returns>
    Task<GetPaymentInvoicesResponse> GetPaymentInvoicesAsync(string paymentLinkId, CancellationToken cancellationToken = default);
}
