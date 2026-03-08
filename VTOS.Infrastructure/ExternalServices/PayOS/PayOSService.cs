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
public class PayOSService : IPayOSService
{
    private readonly HttpClient _httpClient;
    private readonly PayOSSettings _settings;
    private readonly ILogger<PayOSService> _logger;
    private const string ApiVersion = "v2";
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

            var endpoint = $"{_settings.ApiUrl.TrimEnd('/')}/{ApiVersion}/payment-requests";
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

            var endpoint = $"{_settings.ApiUrl.TrimEnd('/')}/{ApiVersion}/payment-requests/{paymentLinkId}";
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

            var endpoint = $"{_settings.ApiUrl.TrimEnd('/')}/{ApiVersion}/payment-requests/{paymentLinkId}/cancel";
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

            var endpoint = $"{_settings.ApiUrl.TrimEnd('/')}/{ApiVersion}/payment-requests/{paymentLinkId}/invoices";
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

    /// <summary>
    /// Validate PayOS credentials are configured
    /// </summary>
    private void ValidatePayOSCredentials()
    {
        if (string.IsNullOrEmpty(_settings.ClientId) || 
            string.IsNullOrEmpty(_settings.ApiKey) || 
            string.IsNullOrEmpty(_settings.ChecksumKey))
        {
            throw new InvalidOperationException("Missing PayOS credentials (ClientId/ApiKey/ChecksumKey)");
        }
    }

    /// <summary>
    /// Set authorization headers for PayOS API requests
    /// </summary>
    private void SetPaymentAuthorizedHeaders()
    {
        _httpClient.DefaultRequestHeaders.Clear();
        _httpClient.DefaultRequestHeaders.Add("x-client-id", _settings.ClientId);
        _httpClient.DefaultRequestHeaders.Add("x-api-key", _settings.ApiKey);
    }

    /// <summary>
    /// Send authorized POST request to PayOS API
    /// </summary>
    private async Task<HttpResponseMessage> SendAuthorizedPostRequestAsync(
        string endpoint, 
        StringContent content,
        CancellationToken cancellationToken = default)
    {
        SetPaymentAuthorizedHeaders();
        return await _httpClient.PostAsync(endpoint, content, cancellationToken);
    }

    /// <summary>
    /// Send authorized GET request to PayOS API
    /// </summary>
    private async Task<HttpResponseMessage> SendAuthorizedGetRequestAsync(
        string endpoint,
        CancellationToken cancellationToken = default)
    {
        SetPaymentAuthorizedHeaders();
        return await _httpClient.GetAsync(endpoint, cancellationToken);
    }

    /// <summary>
    /// Serialize object to JSON StringContent
    /// </summary>
    private static StringContent SerializeToJsonContent(object obj)
    {
        return new StringContent(
            JsonSerializer.Serialize(obj),
            Encoding.UTF8,
            "application/json"
        );
    }

    /// <summary>
    /// Deserialize JSON response to generic PayOS response model
    /// </summary>
    private PayOSResponse<T> DeserializeResponse<T>(string jsonContent) where T : class
    {
        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        return JsonSerializer.Deserialize<PayOSResponse<T>>(jsonContent, options)!;
    }

    /// <summary>
    /// Create payment signature for PayOS API request
    /// Computes the PayOS HMAC-SHA256 Signature based on specific properties alphabetically ordered
    /// amount={amount}&cancelUrl={cancelUrl}&description={description}&orderCode={orderCode}&returnUrl={returnUrl}
    /// </summary>
    private string CreatePaymentSignature(
        int amount, 
        string cancelUrl, 
        string description, 
        int orderCode, 
        string returnUrl)
    {
        return ComputeHmacSignature(
            $"amount={amount}&cancelUrl={cancelUrl}&description={description}&orderCode={orderCode}&returnUrl={returnUrl}",
            _settings.ChecksumKey);
    }

    /// <summary>
    /// Compute HMAC-SHA256 signature from data string
    /// </summary>
    private static string ComputeHmacSignature(string data, string checksumKey)
    {
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(checksumKey));
        var hashBytes = hmac.ComputeHash(Encoding.UTF8.GetBytes(data));
        
        // Return lowercase HEX string format
        return BitConverter.ToString(hashBytes).Replace("-", "").ToLowerInvariant();
    }

    [System.Diagnostics.CodeAnalysis.SuppressMessage("CodeQuality", "IDE0051:Remove unused private members", Justification = "Used for JSON deserialization")]
    private sealed class PayOSResponse<T> where T : class
    {
        public string? Code { get; set; } = default;
        public string? Desc { get; set; } = default;
        public T? Data { get; set; } = default;
        public string? Signature { get; set; } = default;
    }
    //raw response data model from payos for create payment link API (for debugging/logging purposes)
    [System.Diagnostics.CodeAnalysis.SuppressMessage("CodeQuality", "IDE0051:Remove unused private members", Justification = "Used for JSON deserialization")]
    private sealed class CreatePaymentLinkResponseData
    {
        public string? Bin { get; set; } = default;
        public string? AccountNumber { get; set; } = default;
        public string? AccountName { get; set; } = default;
        public int Amount { get; set; } = default;
        public string? Description { get; set; } = default;
        public int OrderCode { get; set; } = default;
        public string? Currency { get; set; } = default;
        public string? PaymentLinkId { get; set; } = default;
        public string? Status { get; set; } = default;
        public string? CheckoutUrl { get; set; } = default;
        public string? QrCode { get; set; } = default;
    }
}
