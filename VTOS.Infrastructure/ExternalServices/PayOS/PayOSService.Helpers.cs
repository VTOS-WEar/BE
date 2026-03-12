using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace VTOS.Infrastructure.ExternalServices.PayOS;

/// <summary>
/// Helper methods and internal models for PayOSService
/// </summary>
public partial class PayOSService
{
    #region Payment Helpers
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
#endregion
    #region Payout Helpers

    /// <summary>
    /// Validate PayOS payout credentials are configured
    /// </summary>
    private void ValidatePayoutCredentials()
    {
        if (string.IsNullOrEmpty(_settings.PayoutClientId) ||
            string.IsNullOrEmpty(_settings.PayoutApiKey) ||
            string.IsNullOrEmpty(_settings.PayoutChecksumKey))
        {
            throw new InvalidOperationException("Missing PayOS payout credentials (PayoutClientId/PayoutApiKey/PayoutChecksumKey)");
        }
    }

    /// <summary>
    /// Create payout signature for PayOS payout API request.
    /// Deep-sorts payload keys alphabetically, builds a URL-encoded query string,
    /// then computes HMAC-SHA256 with the payout checksum key.
    /// </summary>
    private string CreatePayoutSignature(Dictionary<string, object?> payload)
    {
        var sortedKeys = payload.Keys.OrderBy(k => k, StringComparer.Ordinal).ToList();

        var queryString = string.Join("&", sortedKeys.Select(key =>
        {
            var value = payload[key];
            string stringValue;

            if (value is null)
            {
                stringValue = string.Empty;
            }
            else if (value is JsonElement jsonElement)
            {
                stringValue = jsonElement.ValueKind switch
                {
                    JsonValueKind.Array or JsonValueKind.Object => jsonElement.GetRawText(),
                    JsonValueKind.Null => string.Empty,
                    _ => jsonElement.ToString()
                };
            }
            else if (value is IEnumerable<object> or System.Collections.IList)
            {
                stringValue = JsonSerializer.Serialize(value);
            }
            else if (value is IDictionary<string, object?>)
            {
                stringValue = JsonSerializer.Serialize(value);
            }
            else
            {
                stringValue = value.ToString() ?? string.Empty;
            }

            return $"{Uri.EscapeDataString(key)}={Uri.EscapeDataString(stringValue)}";
        }));

        _logger.LogDebug("Payout signature data string: {QueryString}", queryString);

        var signature = ComputeHmacSignature(queryString, _settings.PayoutChecksumKey);

        _logger.LogDebug("Generated payout signature: {Signature}", signature);

        return signature;
    }

    /// <summary>
    /// Set payout authorization headers with idempotency key and signature
    /// </summary>
    private void SetPayoutAuthorizedHeaders(string idempotencyKey, string signature)
    {
        _httpClient.DefaultRequestHeaders.Clear();
        _httpClient.DefaultRequestHeaders.Add("x-client-id", _settings.PayoutClientId);
        _httpClient.DefaultRequestHeaders.Add("x-api-key", _settings.PayoutApiKey);
        _httpClient.DefaultRequestHeaders.Add("x-idempotency-key", idempotencyKey);
        _httpClient.DefaultRequestHeaders.Add("x-signature", signature);
    }

    /// <summary>
    /// Set basic payout authorization headers (no signature/idempotency)
    /// </summary>
    private void SetPayoutGetHeaders()
    {
        _httpClient.DefaultRequestHeaders.Clear();
        _httpClient.DefaultRequestHeaders.Add("x-client-id", _settings.PayoutClientId);
        _httpClient.DefaultRequestHeaders.Add("x-api-key", _settings.PayoutApiKey);
    }

    /// <summary>
    /// Send signed POST request to PayOS payout API
    /// </summary>
    private async Task<HttpResponseMessage> SendPayoutPostRequestAsync(
        string endpoint,
        StringContent content,
        string idempotencyKey,
        string signature,
        CancellationToken cancellationToken = default)
    {
        SetPayoutAuthorizedHeaders(idempotencyKey, signature);
        return await _httpClient.PostAsync(endpoint, content, cancellationToken);
    }

    /// <summary>
    /// Send GET request to PayOS payout API
    /// </summary>
    private async Task<HttpResponseMessage> SendPayoutGetRequestAsync(
        string endpoint,
        CancellationToken cancellationToken = default)
    {
        SetPayoutGetHeaders();
        return await _httpClient.GetAsync(endpoint, cancellationToken);
    }

    #endregion

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
