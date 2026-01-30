using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using VTOS.Application.Abstractions;

namespace VTOS.Infrastructure.ExternalServices.TryOn;

/// <summary>
/// Implementation of virtual try-on service using 302.ai API
/// </summary>
public class VirtualTryOnService : IVirtualTryOnService
{
    private readonly HttpClient _httpClient;
    private readonly VirtualTryOnSettings _settings;
    private readonly ILogger<VirtualTryOnService> _logger;

    public VirtualTryOnService(
        HttpClient httpClient,
        IOptions<VirtualTryOnSettings> settings,
        ILogger<VirtualTryOnService> logger)
    {
        _httpClient = httpClient;
        _settings = settings.Value;
        _logger = logger;
    }

    public async Task<TryOnResult> ProcessAsync(
        string humanImageUrl, 
        string garmentImageUrl, 
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Processing try-on request. Human: {HumanUrl}, Garment: {GarmentUrl}", 
                humanImageUrl, garmentImageUrl);

            var requestBody = new
            {
                human_image_url = humanImageUrl,
                garment_image_url = garmentImageUrl
            };

            var content = new StringContent(
                JsonSerializer.Serialize(requestBody),
                new MediaTypeHeaderValue("application/json"));

            var request = new HttpRequestMessage(HttpMethod.Post, _settings.ApiUrl)
            {
                Content = content
            };
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _settings.ApiKey);

            var response = await _httpClient.SendAsync(request, cancellationToken);
            var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);

            _logger.LogInformation("302.ai raw response: {Response}", responseContent);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("302.ai API returned error. Status: {Status}, Response: {Response}",
                    response.StatusCode, responseContent);
                return new TryOnResult(false, null, $"API error: {response.StatusCode}");
            }

            var result = JsonSerializer.Deserialize<TryOnApiResponse>(responseContent, 
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (result?.Status != "success")
            {
                _logger.LogWarning("Try-on failed. Status: {Status}, Error: {Error}", 
                    result?.Status, result?.Err);
                return new TryOnResult(false, null, result?.Err ?? "Unknown error");
            }

            _logger.LogInformation("Try-on successful. Result URL: {ResultUrl}", result.ImageUrl);
            return new TryOnResult(true, result.ImageUrl, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing virtual try-on request");
            return new TryOnResult(false, null, $"Service error: {ex.Message}");
        }
    }

    private class TryOnApiResponse
    {
        [JsonPropertyName("err")]
        public string? Err { get; set; }
        
        [JsonPropertyName("image_url")]
        public string? ImageUrl { get; set; }
        
        [JsonPropertyName("status")]
        public string? Status { get; set; }
    }
}
