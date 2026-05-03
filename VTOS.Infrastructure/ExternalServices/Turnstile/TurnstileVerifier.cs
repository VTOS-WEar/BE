using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Net.Http.Json;
using VTOS.Application.Abstractions;

namespace VTOS.Infrastructure.ExternalServices.Turnstile;

public class TurnstileVerifier : ITurnstileVerifier
{
    private readonly HttpClient _httpClient;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILogger<TurnstileVerifier> _logger;
    private readonly TurnstileSettings _settings;

    public TurnstileVerifier(
        HttpClient httpClient,
        IHttpContextAccessor httpContextAccessor,
        IOptions<TurnstileSettings> settings,
        ILogger<TurnstileVerifier> logger)
    {
        _httpClient = httpClient;
        _httpContextAccessor = httpContextAccessor;
        _settings = settings.Value;
        _logger = logger;
    }

    public async Task<TurnstileVerificationResult> VerifyAsync(
        string token,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            return Failure("missing-input-response");
        }

        if (string.IsNullOrWhiteSpace(_settings.SecretKey))
        {
            _logger.LogError("Cloudflare Turnstile secret key is not configured");
            return Failure("missing-input-secret");
        }

        var parameters = new Dictionary<string, string>
        {
            ["secret"] = _settings.SecretKey,
            ["response"] = token,
            ["idempotency_key"] = Guid.NewGuid().ToString()
        };

        var remoteIp = GetRemoteIp();
        if (!string.IsNullOrWhiteSpace(remoteIp))
        {
            parameters["remoteip"] = remoteIp;
        }

        try
        {
            using var content = new FormUrlEncodedContent(parameters);
            using var response = await _httpClient.PostAsync(_settings.SiteVerifyUrl, content, cancellationToken);
            var payload = await response.Content.ReadFromJsonAsync<TurnstileSiteVerifyResponse>(
                cancellationToken: cancellationToken);

            if (!response.IsSuccessStatusCode || payload is null)
            {
                _logger.LogWarning("Cloudflare Turnstile Siteverify returned {StatusCode}", response.StatusCode);
                return Failure("siteverify-unavailable");
            }

            if (!payload.Success)
            {
                return Failure(payload.ErrorCodes ?? Array.Empty<string>());
            }

            if (!string.IsNullOrWhiteSpace(_settings.ExpectedHostname)
                && !string.Equals(payload.Hostname, _settings.ExpectedHostname, StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogWarning(
                    "Cloudflare Turnstile hostname mismatch. Expected {ExpectedHostname}, got {ActualHostname}",
                    _settings.ExpectedHostname,
                    payload.Hostname);
                return Failure("hostname-mismatch");
            }

            return new TurnstileVerificationResult(true, Array.Empty<string>());
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or OperationCanceledException)
        {
            _logger.LogWarning(ex, "Cloudflare Turnstile verification failed");
            return Failure("siteverify-unavailable");
        }
    }

    private string? GetRemoteIp()
    {
        var request = _httpContextAccessor.HttpContext?.Request;
        if (request is null) return null;

        var forwardedFor = request.Headers["CF-Connecting-IP"].FirstOrDefault()
            ?? request.Headers["X-Forwarded-For"].FirstOrDefault();

        if (!string.IsNullOrWhiteSpace(forwardedFor))
        {
            return forwardedFor.Split(',')[0].Trim();
        }

        return _httpContextAccessor.HttpContext?.Connection.RemoteIpAddress?.ToString();
    }

    private static TurnstileVerificationResult Failure(params string[] errorCodes)
    {
        return Failure((IReadOnlyList<string>)errorCodes);
    }

    private static TurnstileVerificationResult Failure(IReadOnlyList<string>? errorCodes)
    {
        return new TurnstileVerificationResult(false, errorCodes ?? Array.Empty<string>());
    }

    private sealed class TurnstileSiteVerifyResponse
    {
        [JsonPropertyName("success")]
        public bool Success { get; set; }

        [JsonPropertyName("hostname")]
        public string? Hostname { get; set; }

        [JsonPropertyName("error-codes")]
        public string[]? ErrorCodes { get; set; }
    }
}
