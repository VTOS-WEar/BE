using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using VTOS.Application.Abstractions;
using VTOS.Application.Common.Models.BodygramDTOs;
using VTOS.Infrastructure.Bodygram.Helpers;

namespace VTOS.Infrastructure.Bodygram;

/// <summary>
/// Service for interacting with Bodygram API for 3D body scanning and avatar generation
/// </summary>
public class BodygramService : IBodygramService
{
    private readonly HttpClient _httpClient;
    private readonly BodygramSettings _settings;
    private readonly ILogger<BodygramService> _logger;

    // Shared JsonSerializerOptions
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public BodygramService(
        HttpClient httpClient,
        IOptions<BodygramSettings> options,
        ILogger<BodygramService> logger)
    {
        _httpClient = httpClient;
        _settings = options.Value;
        _logger = logger;
    }

    /// <summary>
    /// Creates a new body scan in Bodygram with photo data
    /// </summary>
    public async Task<BodygramScanResponse> CreateScanAsync(CreateScanRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Creating Bodygram scan with custom ID: {CustomScanId}", request.CustomScanId);

            var url = $"{_settings.BaseUrl}/orgs/{_settings.OrganizationId}/scans";
            var httpRequest = CreatePostRequestWithBody(url, request);

            var response = await _httpClient.SendAsync(httpRequest, cancellationToken);
            
            if (!response.IsSuccessStatusCode)
            {
                await HandleErrorResponseAsync(response, "Bodygram scan creation", cancellationToken);
            }

            var jsonResponse = await response.Content.ReadAsStringAsync(cancellationToken);
            var scanResponse = DeserializeResponse<BodygramScanResponse>(jsonResponse);

            _logger.LogInformation("Bodygram scan created successfully with ID: {ScanId}", scanResponse?.Entry?.Id);
            return scanResponse;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating Bodygram scan");
            throw;
        }
    }

    /// <summary>
    /// Retrieves list of all scans for the organization
    /// </summary>
    public async Task<ScanListResponse> GetScansAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Retrieving scans for organization: {OrganizationId}", _settings.OrganizationId);

            var url = $"{_settings.BaseUrl}/orgs/{_settings.OrganizationId}/scans";
            var httpRequest = CreateAuthenticatedRequest(HttpMethod.Get, url);

            var response = await _httpClient.SendAsync(httpRequest, cancellationToken);
            
            if (!response.IsSuccessStatusCode)
            {
                await HandleErrorResponseAsync(response, "Retrieve scans", cancellationToken);
            }

            var jsonResponse = await response.Content.ReadAsStringAsync(cancellationToken);
            var scansResponse = DeserializeResponse<ScanListResponse>(jsonResponse);

            _logger.LogInformation("Retrieved {ScanCount} scans", scansResponse?.Results.Count ?? 0);
            return scansResponse;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving scans");
            throw;
        }
    }

    /// <summary>
    /// Retrieves a specific scan by ID
    /// </summary>
    public async Task<BodygramScanResponse> GetScanAsync(string scanId, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Retrieving scan with ID: {ScanId}", scanId);

            var url = $"{_settings.BaseUrl}/orgs/{_settings.OrganizationId}/scans/{scanId}";
            var httpRequest = CreateAuthenticatedRequest(HttpMethod.Get, url);

            var response = await _httpClient.SendAsync(httpRequest, cancellationToken);
            
            if (!response.IsSuccessStatusCode)
            {
                await HandleErrorResponseAsync(response, $"Retrieve scan {scanId}", cancellationToken);
            }

            var jsonResponse = await response.Content.ReadAsStringAsync(cancellationToken);
            var scanResponse = DeserializeResponse<BodygramScanResponse>(jsonResponse);

            _logger.LogInformation("Retrieved scan {ScanId} with status: {Status}", scanId, scanResponse?.Entry?.Status);
            return scanResponse;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving scan with ID: {ScanId}", scanId);
            throw;
        }
    }

    /// <summary>
    /// Helper: Create HTTP request with authorization header
    /// </summary>
    private HttpRequestMessage CreateAuthenticatedRequest(HttpMethod method, string url)
    {
        var request = new HttpRequestMessage(method, url);
        request.Headers.Add("Authorization", _settings.ApiKey);
        return request;
    }

    /// <summary>
    /// Helper: Create POST request with JSON content and authorization
    /// </summary>
    private HttpRequestMessage CreatePostRequestWithBody<T>(string url, T requestData)
    {
        var jsonContent = JsonSerializer.Serialize(requestData, JsonOptions);
        var request = new HttpRequestMessage(HttpMethod.Post, url)
        {
            Content = new StringContent(jsonContent, Encoding.UTF8, "application/json")
        };
        request.Headers.Add("Authorization", _settings.ApiKey);
        return request;
    }

    /// <summary>
    /// Helper: Handle API error response
    /// </summary>
    private async Task HandleErrorResponseAsync(HttpResponseMessage response, string operationName, CancellationToken cancellationToken)
    {
        var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
        _logger.LogError("{OperationName} failed with status {StatusCode}: {ErrorContent}", 
            operationName, response.StatusCode, errorContent);

        // Try to parse Bodygram validation errors
        var bodygramErrors = BodygramErrorHandler.ParseErrorResponse(errorContent, JsonOptions);
        if (bodygramErrors?.Errors.Any() == true)
        {
            var formattedErrors = BodygramErrorHandler.FormatErrors(bodygramErrors.Errors);
            throw new BodygramValidationException(formattedErrors);
        }

        throw new HttpRequestException($"Bodygram API error: {response.StatusCode} - {errorContent}");
    }

    /// <summary>
    /// Helper: Deserialize API response
    /// </summary>
    private T DeserializeResponse<T>(string jsonResponse) where T : new()
    {
        return JsonSerializer.Deserialize<T>(jsonResponse, JsonOptions) ?? new T();
    }
}
