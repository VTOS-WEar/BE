using VTOS.Application.Common.Models.PayOSDTOs;

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

    #region Payout Methods

    /// <summary>
    /// Get payout account balance from PayOS
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Payout account balance detail</returns>
    Task<PayoutAccountDetailResponse> GetPayoutAccountDetailAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Get list of payouts from PayOS with optional filters
    /// </summary>
    /// <param name="query">Query parameters for filtering and pagination</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Paginated payout list</returns>
    Task<PayoutListResponse> GetPayoutListAsync(PayoutListQuery? query = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get payout detail by payout ID from PayOS
    /// </summary>
    /// <param name="payoutId">Payout ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Payout detail including transactions</returns>
    Task<PayoutDetailResponse> GetPayoutDetailAsync(string payoutId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Create a new payout (disbursement) on PayOS
    /// </summary>
    /// <param name="request">Payout creation parameters</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Created payout response</returns>
    Task<CreatePayoutResponse> CreatePayoutAsync(CreatePayoutRequest request, CancellationToken cancellationToken = default);

    #endregion
}
