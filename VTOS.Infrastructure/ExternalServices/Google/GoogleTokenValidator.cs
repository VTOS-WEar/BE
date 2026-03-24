using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using VTOS.Application.Abstractions;
using System.Net.Http.Json;

namespace VTOS.Infrastructure.ExternalServices.Google;

/// <summary>
/// Validates Google tokens by calling Google's userinfo API with the access token.
/// </summary>
public class GoogleTokenValidator : IGoogleTokenValidator
{
    private readonly string _clientId;
    private readonly ILogger<GoogleTokenValidator> _logger;
    private readonly HttpClient _httpClient;

    public GoogleTokenValidator(IConfiguration configuration, ILogger<GoogleTokenValidator> logger, HttpClient httpClient)
    {
        _clientId = configuration["GoogleAuth:ClientId"]
            ?? throw new ArgumentException("GoogleAuth:ClientId is not configured");
        _logger = logger;
        _httpClient = httpClient;
    }

    public async Task<GoogleUserInfo?> ValidateAsync(string accessToken)
    {
        try
        {
            // Use the access token to fetch user info from Google
            var request = new HttpRequestMessage(HttpMethod.Get, "https://www.googleapis.com/oauth2/v3/userinfo");
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);

            var response = await _httpClient.SendAsync(request);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Google userinfo API returned {StatusCode}", response.StatusCode);
                return null;
            }

            var payload = await response.Content.ReadFromJsonAsync<GoogleUserInfoResponse>();
            if (payload == null || string.IsNullOrEmpty(payload.Sub))
            {
                _logger.LogWarning("Google userinfo response missing required fields");
                return null;
            }

            return new GoogleUserInfo(
                Sub: payload.Sub,
                Email: payload.Email ?? string.Empty,
                Name: payload.Name ?? string.Empty,
                Picture: payload.Picture
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating Google token");
            return null;
        }
    }

    private class GoogleUserInfoResponse
    {
        public string Sub { get; set; } = string.Empty;
        public string? Email { get; set; }
        public string? Name { get; set; }
        public string? Picture { get; set; }
    }
}
