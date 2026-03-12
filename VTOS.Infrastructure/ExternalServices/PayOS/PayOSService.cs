using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using VTOS.Application.Abstractions;
using VTOS.Application.Common.Models;

namespace VTOS.Infrastructure.ExternalServices.PayOS;

/// <summary>
/// Implementation of PayOS payment service
/// </summary>
public partial class PayOSService : IPayOSService
{
    private readonly HttpClient _httpClient;
    private readonly PayOSSettings _settings;
    private readonly ILogger<PayOSService> _logger;
    private const string ApiErrorMessage = "PayOS API returned error. Status: {Status}, Response: {Response}";

    public PayOSService(
        HttpClient httpClient,
        IOptions<PayOSSettings> settings,
        ILogger<PayOSService> logger)
    {
        _httpClient = httpClient;
        _settings = settings.Value;
        _logger = logger;
    }
#region     Payment Methods
    /// <summary>
    /// Generate a random 6-digit order code for PayOS payment
    /// </summary>
    public int GenerateOrderCode()
    {
        // Generate random 6-digit number (100000 to 999999)
        return RandomNumberGenerator.GetInt32(100000, 1000000);
    }

    /// <summary>
    /// Create PayOS payment link for customer payment
    /// </summary>
    public async Task<CreatePaymentLinkResponse> CreatePayOSPaymentLinkAsync(
        CreatePaymentLinkRequest input, 
        CancellationToken cancellationToken = default)
    {
        try
        {
            ValidatePayOSCredentials();

            var orderCode = GenerateOrderCode();
            _logger.LogInformation("Creating PayOS payment link for order code: {OrderCode}", orderCode);

            // Calculate the Hash Signature (HMAC-SHA256) of sorted values
            var signature = CreatePaymentSignature(
                input.Amount,
                input.CancelUrl,
                string.IsNullOrWhiteSpace(input.Description) ? $"Payment for order #{orderCode}" : input.Description,
                orderCode,
                input.ReturnUrl);

            var payload = new
            {
                orderCode = orderCode,
                amount = input.Amount,
                description = string.IsNullOrWhiteSpace(input.Description) ? $"Payment for order #{orderCode}" : input.Description,
                returnUrl = input.ReturnUrl,
                cancelUrl = input.CancelUrl,
                signature = signature
            };

            var endpoint = $"{_settings.ApiUrl.TrimEnd('/')}/{_settings.PaymentApiPrefix.TrimStart('/')}";
            var jsonContent = SerializeToJsonContent(payload);

            var response = await SendAuthorizedPostRequestAsync(endpoint, jsonContent, cancellationToken);
            var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);
            var result = DeserializeResponse<CreatePaymentLinkResponseData>(responseContent);

            _logger.LogDebug("PayOS create link response: {Response}", responseContent);

            if (!response.IsSuccessStatusCode || result.Data == null)
            {
                _logger.LogError(ApiErrorMessage,
                    response.StatusCode, responseContent);
                throw new InvalidOperationException($"Create payment link failed: {response.StatusCode} - {responseContent}");
            }


            _logger.LogInformation("Payment link created successfully. Order Code: {OrderCode}", orderCode);
            
            return new CreatePaymentLinkResponse
            {
                CheckoutUrl = result?.Data?.CheckoutUrl ?? string.Empty,
                OrderCode = orderCode,
                PaymentLinkId = result?.Data?.PaymentLinkId ?? string.Empty,
                Signature = signature,
                Raw = result?.Data 
            };
        }
        catch (Exception ex) when (ex is not InvalidOperationException)
        {
            _logger.LogError(ex, "Error creating PayOS payment link");
            throw new InvalidOperationException($"Create payment link failed: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Get payment link information from PayOS
    /// </summary>
    public async Task<GetPaymentLinkInfoResponse> GetPaymentLinkInfoAsync(
        string paymentLinkId, 
        CancellationToken cancellationToken = default)
    {
        try
        {
            ValidatePayOSCredentials();
            
            if (string.IsNullOrWhiteSpace(paymentLinkId))
            {
                throw new ArgumentException("Payment link ID cannot be empty", nameof(paymentLinkId));
            }

            _logger.LogInformation("Fetching payment link info for ID: {PaymentLinkId}", paymentLinkId);

            var endpoint = $"{_settings.ApiUrl.TrimEnd('/')}/{_settings.PaymentApiPrefix.TrimStart('/')}/{paymentLinkId}";
            var response = await SendAuthorizedGetRequestAsync(endpoint, cancellationToken);
            var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);

            _logger.LogDebug("PayOS get link info response: {Response}", responseContent);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError(ApiErrorMessage,
                    response.StatusCode, responseContent);
                throw new InvalidOperationException($"Get payment link info failed: {response.StatusCode} - {responseContent}");
            }

            var result = DeserializeResponse<GetPaymentLinkInfoResponse>(responseContent);

            if (result?.Data == null)
            {
                throw new InvalidOperationException("Invalid response from PayOS API - no data returned");
            }

            _logger.LogInformation("Payment link info retrieved successfully. Payment ID: {PaymentId}", paymentLinkId);

            return result.Data;
        }
        catch (Exception ex) when (ex is not InvalidOperationException and not ArgumentException)
        {
            _logger.LogError(ex, "Error getting payment link info");
            throw new InvalidOperationException($"Get payment link info failed: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Cancel a payment link on PayOS
    /// </summary>
    public async Task<CancelPaymentLinkResponse> CancelPaymentLinkAsync(
        string paymentLinkId, 
        CancellationToken cancellationToken = default)
    {
        try
        {
            ValidatePayOSCredentials();
            
            if (string.IsNullOrWhiteSpace(paymentLinkId))
            {
                throw new ArgumentException("Payment link ID cannot be empty", nameof(paymentLinkId));
            }

            _logger.LogInformation("Cancelling payment link for ID: {PaymentLinkId}", paymentLinkId);

            var endpoint = $"{_settings.ApiUrl.TrimEnd('/')}/{_settings.PaymentApiPrefix.TrimStart('/')}/{paymentLinkId}/cancel";
            var response = await SendAuthorizedPostRequestAsync(endpoint, new StringContent(string.Empty, Encoding.UTF8, "application/json"), cancellationToken);
            var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);

            _logger.LogDebug("PayOS cancel link response: {Response}", responseContent);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError(ApiErrorMessage,
                    response.StatusCode, responseContent);
                throw new InvalidOperationException($"Cancel payment link failed: {response.StatusCode} - {responseContent}");
            }

            var result = DeserializeResponse<CancelPaymentLinkResponse>(responseContent);

            if (result?.Data == null)
            {
                throw new InvalidOperationException("Invalid response from PayOS API - no data returned");
            }

            _logger.LogInformation("Payment link cancelled successfully. Payment ID: {PaymentId}, Status: {Status}", paymentLinkId, result.Data.Status);

            return result.Data;
        }
        catch (Exception ex) when (ex is not InvalidOperationException and not ArgumentException)
        {
            _logger.LogError(ex, "Error cancelling payment link");
            throw new InvalidOperationException($"Cancel payment link failed: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Get payment invoices from PayOS
    /// </summary>
    public async Task<GetPaymentInvoicesResponse> GetPaymentInvoicesAsync(
        string paymentLinkId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            ValidatePayOSCredentials();

            if (string.IsNullOrWhiteSpace(paymentLinkId))
            {
                throw new ArgumentException("Payment link ID cannot be empty", nameof(paymentLinkId));
            }

            _logger.LogInformation("Fetching payment invoices for ID: {PaymentLinkId}", paymentLinkId);

            var endpoint = $"{_settings.ApiUrl.TrimEnd('/')}/{_settings.PaymentApiPrefix.TrimStart('/')}/{paymentLinkId}/invoices";
            var response = await SendAuthorizedGetRequestAsync(endpoint, cancellationToken);
            var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);

            _logger.LogDebug("PayOS get invoices response: {Response}", responseContent);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError(ApiErrorMessage,
                    response.StatusCode, responseContent);
                throw new InvalidOperationException($"Get payment invoices failed: {response.StatusCode} - {responseContent}");
            }

            var result = DeserializeResponse<GetPaymentInvoicesResponse>(responseContent);

            if (result?.Data == null)
            {
                throw new InvalidOperationException("Invalid response from PayOS API - no data returned");
            }

            _logger.LogInformation("Payment invoices retrieved successfully. Payment ID: {PaymentId}, Invoice Count: {InvoiceCount}",
                paymentLinkId, result.Data.Invoices?.Count ?? 0);

            return result.Data;
        }
        catch (Exception ex) when (ex is not InvalidOperationException and not ArgumentException)
        {
            _logger.LogError(ex, "Error getting payment invoices");
            throw new InvalidOperationException($"Get payment invoices failed: {ex.Message}", ex);
        }
    }
#endregion 

#region Payout Methods

    /// <summary>
    /// Get payout account balance from PayOS
    /// </summary>
    public async Task<PayoutAccountDetailResponse> GetPayoutAccountDetailAsync(
        CancellationToken cancellationToken = default)
    {
        try
        {
            ValidatePayoutCredentials();

            _logger.LogInformation("Fetching payout account balance");

            var endpoint = $"{_settings.ApiUrl}/v1/payouts-account/balance";
            var response = await SendPayoutGetRequestAsync(endpoint, cancellationToken);
            var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);

            _logger.LogDebug("PayOS payout account detail response: {Response}", responseContent);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError(ApiErrorMessage, response.StatusCode, responseContent);
                throw new InvalidOperationException($"Get payout account detail failed: {response.StatusCode} - {responseContent}");
            }

            var result = DeserializeResponse<PayoutAccountDetailResponse>(responseContent);

            if (result?.Data == null)
            {
                throw new InvalidOperationException("Invalid response from PayOS Payout API - no data returned");
            }

            _logger.LogInformation("Payout account balance retrieved successfully. Balance: {Balance}", result.Data.Balance);

            return result.Data;
        }
        catch (Exception ex) when (ex is not InvalidOperationException)
        {
            _logger.LogError(ex, "Error fetching payout account balance");
            throw new InvalidOperationException($"Get payout account detail failed: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Get list of payouts from PayOS with optional filters
    /// </summary>
    public async Task<PayoutListResponse> GetPayoutListAsync(
        PayoutListQuery? query = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            ValidatePayoutCredentials();

            query ??= new PayoutListQuery();

            _logger.LogInformation("Fetching payout list with Limit={Limit}, Offset={Offset}", query.Limit, query.Offset);

            // Build query string parameters
            var queryParams = new List<string>
            {
                $"limit={query.Limit}",
                $"offset={query.Offset}"
            };

            if (!string.IsNullOrWhiteSpace(query.ReferenceId))
                queryParams.Add($"referenceId={Uri.EscapeDataString(query.ReferenceId)}");
            if (!string.IsNullOrWhiteSpace(query.ApprovalState))
                queryParams.Add($"approvalState={Uri.EscapeDataString(query.ApprovalState)}");
            if (!string.IsNullOrWhiteSpace(query.Category))
                queryParams.Add($"category={Uri.EscapeDataString(query.Category)}");
            if (!string.IsNullOrWhiteSpace(query.FromDate))
                queryParams.Add($"fromDate={Uri.EscapeDataString(query.FromDate)}");
            if (!string.IsNullOrWhiteSpace(query.ToDate))
                queryParams.Add($"toDate={Uri.EscapeDataString(query.ToDate)}");

            var queryString = string.Join("&", queryParams);
            var endpoint = $"{_settings.ApiUrl}/{_settings.PayoutApiPrefix}?{queryString}";

            var response = await SendPayoutGetRequestAsync(endpoint, cancellationToken);
            var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);

            _logger.LogDebug("PayOS payout list response: {Response}", responseContent);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError(ApiErrorMessage, response.StatusCode, responseContent);
                throw new InvalidOperationException($"Get payout list failed: {response.StatusCode} - {responseContent}");
            }

            var result = DeserializeResponse<PayoutListResponse>(responseContent);

            if (result?.Data == null)
            {
                throw new InvalidOperationException("Invalid response from PayOS Payout API - no data returned");
            }

            _logger.LogInformation("Payout list retrieved successfully. Count: {Count}", result.Data.Data?.Count ?? 0);

            return result.Data;
        }
        catch (Exception ex) when (ex is not InvalidOperationException)
        {
            _logger.LogError(ex, "Error fetching payout list");
            throw new InvalidOperationException($"Get payout list failed: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Get payout detail by payout ID from PayOS
    /// </summary>
    public async Task<PayoutDetailResponse> GetPayoutDetailAsync(
        string payoutId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            ValidatePayoutCredentials();

            if (string.IsNullOrWhiteSpace(payoutId))
            {
                throw new ArgumentException("Payout ID cannot be empty", nameof(payoutId));
            }

            _logger.LogInformation("Fetching payout detail for ID: {PayoutId}", payoutId);

            var endpoint = $"{_settings.ApiUrl}/{_settings.PayoutApiPrefix}/{Uri.EscapeDataString(payoutId)}";
            var response = await SendPayoutGetRequestAsync(endpoint, cancellationToken);
            var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);

            _logger.LogDebug("PayOS payout detail response: {Response}", responseContent);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError(ApiErrorMessage, response.StatusCode, responseContent);
                throw new InvalidOperationException($"Get payout detail failed: {response.StatusCode} - {responseContent}");
            }

            var result = DeserializeResponse<PayoutDetailResponse>(responseContent);

            if (result?.Data == null)
            {
                throw new InvalidOperationException("Invalid response from PayOS Payout API - no data returned");
            }

            _logger.LogInformation("Payout detail retrieved successfully. Payout ID: {PayoutId}, State: {State}",
                payoutId, result.Data.ApprovalState);

            return result.Data;
        }
        catch (Exception ex) when (ex is not InvalidOperationException and not ArgumentException)
        {
            _logger.LogError(ex, "Error fetching payout detail for ID: {PayoutId}", payoutId);
            throw new InvalidOperationException($"Get payout detail failed: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Create a new payout (disbursement) on PayOS
    /// POST /v1/payouts
    /// </summary>
    public async Task<CreatePayoutResponse> CreatePayoutAsync(
        CreatePayoutRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            ValidatePayoutCredentials();

            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            if (string.IsNullOrWhiteSpace(request.ReferenceId))
            {
                throw new ArgumentException("ReferenceId is required", nameof(request));
            }

            if (request.Amount <= 0)
            {
                throw new ArgumentException("Amount must be greater than zero", nameof(request));
            }

            _logger.LogInformation("Creating payout. ReferenceId: {ReferenceId}, Amount: {Amount}, ToBin: {ToBin}, ToAccount: {ToAccount}",
                request.ReferenceId, request.Amount, request.ToBin, request.ToAccountNumber);

            // Build payload dictionary for signature computation
            var payloadDict = new Dictionary<string, object?>
            {
                { "amount", request.Amount },
                { "description", request.Description },
                { "referenceId", request.ReferenceId },
                { "toBin", request.ToBin },
                { "toAccountNumber", request.ToAccountNumber }
            };

            if (request.Category != null && request.Category.Count > 0)
            {
                payloadDict["category"] = request.Category;
            }

            // Compute signature from payload
            var signature = CreatePayoutSignature(payloadDict);

            // Use referenceId as idempotency key to prevent duplicate payouts
            var idempotencyKey = request.ReferenceId;

            var payload = new
            {
                referenceId = request.ReferenceId,
                amount = request.Amount,
                description = request.Description,
                toBin = request.ToBin,
                toAccountNumber = request.ToAccountNumber,
                category = request.Category
            };

            var endpoint = $"{_settings.ApiUrl}/{_settings.PayoutApiPrefix}";
            var jsonContent = SerializeToJsonContent(payload);

            var response = await SendPayoutPostRequestAsync(endpoint, jsonContent, idempotencyKey, signature, cancellationToken);
            var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);

            _logger.LogDebug("PayOS create payout response: {Response}", responseContent);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError(ApiErrorMessage, response.StatusCode, responseContent);
                throw new InvalidOperationException($"Create payout failed: {response.StatusCode} - {responseContent}");
            }

            var result = DeserializeResponse<CreatePayoutResponse>(responseContent);

            if (result?.Data == null)
            {
                throw new InvalidOperationException("Invalid response from PayOS Payout API - no data returned");
            }

            _logger.LogInformation("Payout created successfully. Payout ID: {PayoutId}, ReferenceId: {ReferenceId}, State: {State}",
                result.Data.Id, result.Data.ReferenceId, result.Data.ApprovalState);

            return result.Data;
        }
        catch (Exception ex) when (ex is not InvalidOperationException and not ArgumentException and not ArgumentNullException)
        {
            _logger.LogError(ex, "Error creating payout");
            throw new InvalidOperationException($"Create payout failed: {ex.Message}", ex);
        }
    }

#endregion
}
