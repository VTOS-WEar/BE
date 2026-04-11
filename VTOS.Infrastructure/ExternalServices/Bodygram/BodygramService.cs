using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using VTOS.Application.Abstractions;
using VTOS.Application.Common.Models.BodygramDTOs;
using VTOS.Domain.Entities;
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
    private readonly IApplicationDbContext _dbContext;

    // Shared JsonSerializerOptions
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    // Track current credential index for fallback strategy (sequential)
    private static int _currentCredentialIndex = 0;
    private static readonly object _credentialLock = new();

    public BodygramService(
        HttpClient httpClient,
        IOptions<BodygramSettings> options,
        ILogger<BodygramService> logger,
        IApplicationDbContext dbContext)
    {
        _httpClient = httpClient;
        _settings = options.Value;
        _logger = logger;
        _dbContext = dbContext;
    }

    /// <summary>
    /// Creates a new body scan in Bodygram with photo data (with fallback credentials on 402/429)
    /// </summary>
    public async Task<BodygramScanResponse> CreateScanAsync(CreateScanRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Creating Bodygram scan with custom ID: {CustomScanId}", request.CustomScanId);

            int maxRetries = GetAvailableCredentials().Count;
            HttpResponseMessage response = null;
            
            for (int attempt = 0; attempt < maxRetries; attempt++)
            {
                var credential = GetCurrentCredential();
                var url = $"{_settings.BaseUrl}/orgs/{credential.OrganizationId}/scans";
                var httpRequest = CreatePostRequestWithBody(url, request, credential);

                response = await _httpClient.SendAsync(httpRequest, cancellationToken);
                
                // Success - return response
                if (response.IsSuccessStatusCode)
                {
                    var jsonResponse = await response.Content.ReadAsStringAsync(cancellationToken);
                    var scanResponse = DeserializeResponse<BodygramScanResponse>(jsonResponse);
                    _logger.LogInformation("Bodygram scan created successfully with ID: {ScanId}", scanResponse?.Entry?.Id);
                    return scanResponse;
                }

                // 402 Payment Required or 429 Too Many Requests - try next credential
                if (response.StatusCode == System.Net.HttpStatusCode.PaymentRequired || 
                    response.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
                {
                    _logger.LogWarning("Bodygram API returned {StatusCode} ({Reason}). Attempting fallback credential {Attempt}/{MaxRetries}", 
                        response.StatusCode, response.ReasonPhrase, attempt + 1, maxRetries);

                    if (attempt < maxRetries - 1)
                    {
                        TryNextCredential();
                        continue;
                    }
                    // If this was last attempt, fall through to error handling
                }
                else
                {
                    // Other errors - don't retry, handle immediately
                    await HandleErrorResponseAsync(response, "Bodygram scan creation", cancellationToken);
                }
            }

            // If we get here, all credentials have been exhausted for 402/429, or unknown error
            if (response != null)
            {
                await HandleErrorResponseAsync(response, "Bodygram scan creation - all credentials exhausted", cancellationToken);
            }

            throw new HttpRequestException("Bodygram API: All available credentials exhausted");
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
            foreach (var credential in GetAvailableCredentials())
            {
                _logger.LogInformation("Retrieving scans for organization: {OrganizationId}", credential.OrganizationId);

                var url = $"{_settings.BaseUrl}/orgs/{credential.OrganizationId}/scans";
                var httpRequest = CreateAuthenticatedRequest(HttpMethod.Get, url, credential);

                var response = await _httpClient.SendAsync(httpRequest, cancellationToken);

                if (!response.IsSuccessStatusCode)
                {
                    if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                    {
                        continue;
                    }

                    await HandleErrorResponseAsync(response, "Retrieve scans", cancellationToken);
                }

                var jsonResponse = await response.Content.ReadAsStringAsync(cancellationToken);
                var scansResponse = DeserializeResponse<ScanListResponse>(jsonResponse);

                _logger.LogInformation("Retrieved {ScanCount} scans", scansResponse?.Results.Count ?? 0);
                return scansResponse;
            }

            return new ScanListResponse();
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

            HttpResponseMessage? lastResponse = null;
            foreach (var credential in GetAvailableCredentials())
            {
                var url = $"{_settings.BaseUrl}/orgs/{credential.OrganizationId}/scans/{scanId}";
                var httpRequest = CreateAuthenticatedRequest(HttpMethod.Get, url, credential);

                var response = await _httpClient.SendAsync(httpRequest, cancellationToken);
                lastResponse = response;

                if (!response.IsSuccessStatusCode)
                {
                    if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                    {
                        continue;
                    }

                    await HandleErrorResponseAsync(response, $"Retrieve scan {scanId}", cancellationToken);
                }

                var jsonResponse = await response.Content.ReadAsStringAsync(cancellationToken);
                var scanResponse = DeserializeResponse<BodygramScanResponse>(jsonResponse);

                _logger.LogInformation("Retrieved scan {ScanId} with status: {Status}", scanId, scanResponse?.Entry?.Status);
                return scanResponse;
            }

            if (lastResponse != null)
            {
                await HandleErrorResponseAsync(lastResponse, $"Retrieve scan {scanId}", cancellationToken);
            }

            return new BodygramScanResponse();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving scan with ID: {ScanId}", scanId);
            throw;
        }
    }

    /// <summary>
    /// Generates a scan token for a specific child after validating parent permissions
    /// </summary>
    public async Task<GenerateScanTokenResponse> GenerateScanTokenForChildAsync(Guid childId, Guid parentId, CancellationToken cancellationToken = default)
    {
        try
        {
            var child = await _dbContext.ChildProfiles
                .FirstOrDefaultAsync(c => c.Id == childId && c.ParentUserID == parentId, cancellationToken);
                
            if (child == null)
            {
                throw new UnauthorizedAccessException("You do not have permission to perform a scan for this student.");
            }

            var customScanId = $"child:{childId}:{Guid.NewGuid()}";

            var request = new GenerateScanTokenRequest
            {
                CustomScanId = customScanId,
                Scope = new List<string> { "api.platform.bodygram.com/scans:create", "api.platform.bodygram.com/scans:read" }
            };

            _logger.LogInformation("Generating Bodygram scan token for custom ID: {CustomScanId}", customScanId);

            int maxRetries = GetAvailableCredentials().Count;
            HttpResponseMessage response = null;
            
            for (int attempt = 0; attempt < maxRetries; attempt++)
            {
                var credential = GetCurrentCredential();
                var url = $"{_settings.BaseUrl}/orgs/{credential.OrganizationId}/scan-tokens";
                var httpRequest = CreatePostRequestWithBody(url, request, credential);

                response = await _httpClient.SendAsync(httpRequest, cancellationToken);
                
                if (response.IsSuccessStatusCode)
                {
                    var jsonResponse = await response.Content.ReadAsStringAsync(cancellationToken);
                    var tokenResponse = DeserializeResponse<GenerateScanTokenResponse>(jsonResponse);
                    
                    // Log scan to DB as pending
                    var scanLog = new BodygramScanLog
                    {
                        ChildId = childId,
                        CustomScanId = customScanId,
                        Status = VTOS.Domain.Enums.BodygramScanStatus.Pending
                    };
                    
                    _dbContext.BodygramScanLogs.Add(scanLog);
                    await _dbContext.SaveChangesAsync(cancellationToken);

                    tokenResponse.CustomScanId = customScanId;
                    tokenResponse.ScannerUrl = BuildScannerUrl(tokenResponse.Token, credential.OrganizationId);

                    _logger.LogInformation("Bodygram scan token generated successfully");
                    return tokenResponse;
                }

                if (response.StatusCode == System.Net.HttpStatusCode.PaymentRequired || 
                    response.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
                {
                    _logger.LogWarning("Bodygram API returned {StatusCode} ({Reason}). Attempting fallback credential {Attempt}/{MaxRetries}", 
                        response.StatusCode, response.ReasonPhrase, attempt + 1, maxRetries);

                    if (attempt < maxRetries - 1)
                    {
                        TryNextCredential();
                        continue;
                    }
                }
                else
                {
                    await HandleErrorResponseAsync(response, "Bodygram scan token generation", cancellationToken);
                }
            }

            if (response != null)
            {
                await HandleErrorResponseAsync(response, "Bodygram scan token generation - all credentials exhausted", cancellationToken);
            }

            throw new HttpRequestException("Bodygram API: All available credentials exhausted");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating Bodygram scan token");
            throw;
        }
    }

    /// <summary>
    /// Completes the scan session, logs the results and updates the child profile
    /// </summary>
    public async Task CompleteScanAsync(Guid childId, Guid parentId, string customScanId, string bodygramScanId, CancellationToken cancellationToken = default)
    {
        try
        {
            var scanLog = await _dbContext.BodygramScanLogs
                .Include(l => l.Child)
                .FirstOrDefaultAsync(l => l.CustomScanId == customScanId && l.ChildId == childId, cancellationToken);

            if (scanLog == null)
            {
                throw new KeyNotFoundException("Bodygram session not found.");
            }

            if (scanLog.Child.ParentUserID != parentId)
            {
                throw new UnauthorizedAccessException("You do not have permission to perform operations on this student.");
            }

            // Retrieve data to save it locally
            if (!string.IsNullOrWhiteSpace(bodygramScanId))
            {
                var response = await GetScanAsync(bodygramScanId, cancellationToken);
                if (response?.Entry != null && response.Entry.CustomScanId != customScanId)
                {
                    throw new InvalidOperationException("Bodygram scan does not belong to the requested session.");
                }

                if (response?.Entry != null)
                {
                    var heightMeasurement = response.Entry.Measurements?.FirstOrDefault(x => x.Name == "height");
                    var weightMeasurement = response.Entry.Measurements?.FirstOrDefault(x => x.Name == "weight");

                    if (heightMeasurement != null)
                    {
                        scanLog.Child.HeightCm = (int)(heightMeasurement.Value / 10.0); // mm -> cm
                    }
                    else if (response.Entry.Input?.PhotoScan?.Height > 0)
                    {
                        scanLog.Child.HeightCm = (int)(response.Entry.Input.PhotoScan.Height / 10.0); // mm -> cm
                    }

                    if (weightMeasurement != null)
                    {
                        scanLog.Child.WeightKg = (float)(weightMeasurement.Value / 1000.0); // g -> kg
                    }
                    else if (response.Entry.Input?.PhotoScan?.Weight > 0)
                    {
                        scanLog.Child.WeightKg = (float)(response.Entry.Input.PhotoScan.Weight / 1000.0); // g -> kg
                    }
                }
            }

            scanLog.Status = VTOS.Domain.Enums.BodygramScanStatus.Completed;
            scanLog.BodygramScanId = bodygramScanId;
            
            await _dbContext.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Bodygram scan {CustomScanId} completed successfully.", customScanId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error completing Bodygram scan");
            throw;
        }
    }

    public async Task<BodygramScanStatusResponse> GetScanStatusAsync(string customScanId, Guid parentId, CancellationToken cancellationToken = default)
    {
        var scanLog = await _dbContext.BodygramScanLogs
            .Include(l => l.Child)
            .FirstOrDefaultAsync(l => l.CustomScanId == customScanId, cancellationToken);

        if (scanLog == null)
        {
            throw new KeyNotFoundException("Bodygram session not found.");
        }

        if (scanLog.Child.ParentUserID != parentId)
        {
            throw new UnauthorizedAccessException("You do not have permission to view this scan session.");
        }

        if (scanLog.Status == VTOS.Domain.Enums.BodygramScanStatus.Pending)
        {
            await TrySyncPendingScanAsync(scanLog, parentId, cancellationToken);
        }

        return new BodygramScanStatusResponse
        {
            Status = scanLog.Status.ToString(),
            ChildId = scanLog.ChildId,
            BodygramScanId = scanLog.BodygramScanId,
            HeightCm = scanLog.Status == VTOS.Domain.Enums.BodygramScanStatus.Completed ? scanLog.Child.HeightCm : null,
            WeightKg = scanLog.Status == VTOS.Domain.Enums.BodygramScanStatus.Completed ? scanLog.Child.WeightKg : null,
        };
    }

    private async Task TrySyncPendingScanAsync(BodygramScanLog scanLog, Guid parentId, CancellationToken cancellationToken)
    {
        try
        {
            var scans = await GetScansAsync(cancellationToken);
            var matchedScan = scans.Results
                .FirstOrDefault(scan => string.Equals(scan.CustomScanId, scanLog.CustomScanId, StringComparison.Ordinal));

            if (matchedScan == null)
            {
                return;
            }

            if (string.Equals(matchedScan.Status, "success", StringComparison.OrdinalIgnoreCase))
            {
                await CompleteScanAsync(scanLog.ChildId, parentId, scanLog.CustomScanId, matchedScan.Id, cancellationToken);
                return;
            }

            if (string.Equals(matchedScan.Status, "failure", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(matchedScan.Status, "failed", StringComparison.OrdinalIgnoreCase))
            {
                scanLog.Status = VTOS.Domain.Enums.BodygramScanStatus.Failed;
                scanLog.BodygramScanId = matchedScan.Id;
                await _dbContext.SaveChangesAsync(cancellationToken);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Unable to sync Bodygram pending scan {CustomScanId} during status polling.", scanLog.CustomScanId);
        }
    }

    /// <summary>
    /// Helper: Create HTTP request with authorization header
    /// </summary>
    private HttpRequestMessage CreateAuthenticatedRequest(HttpMethod method, string url)
    {
        var credential = GetCurrentCredential();
        return CreateAuthenticatedRequest(method, url, credential);
    }

    private HttpRequestMessage CreateAuthenticatedRequest(HttpMethod method, string url, BodygramCredential credential)
    {
        var request = new HttpRequestMessage(method, url);
        request.Headers.Add("Authorization", credential.ApiKey);
        return request;
    }

    /// <summary>
    /// Helper: Create POST request with JSON content and authorization
    /// </summary>
    private HttpRequestMessage CreatePostRequestWithBody<T>(string url, T requestData)
    {
        var credential = GetCurrentCredential();
        return CreatePostRequestWithBody(url, requestData, credential);
    }

    /// <summary>
    /// Helper: Create POST request with specific credential
    /// </summary>
    private HttpRequestMessage CreatePostRequestWithBody<T>(string url, T requestData, BodygramCredential credential)
    {
        var jsonContent = JsonSerializer.Serialize(requestData, JsonOptions);
        var request = new HttpRequestMessage(HttpMethod.Post, url)
        {
            Content = new StringContent(jsonContent, Encoding.UTF8, "application/json")
        };
        request.Headers.Add("Authorization", credential.ApiKey);
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

    private string BuildScannerUrl(string token, string organizationId)
    {
        var locale = string.IsNullOrWhiteSpace(_settings.Locale) ? "en" : _settings.Locale.Trim();
        var baseUrl = _settings.ScannerBaseUrl.TrimEnd('/');
        var encodedToken = Uri.EscapeDataString(token);

        return $"{baseUrl}/{locale}/{organizationId}/scan?token={encodedToken}&system=metric&tap=true";
    }

    /// <summary>
    /// Helper: Get current credential (with fallback to legacy settings)
    /// </summary>
    private BodygramCredential GetCurrentCredential()
    {
        lock (_credentialLock)
        {
            var credentials = GetAvailableCredentials();
            if (credentials.Count == 0)
            {
                // Backward compatibility: use legacy ApiKey/OrganizationId
                return new BodygramCredential
                {
                    ApiKey = _settings.ApiKey,
                    OrganizationId = _settings.OrganizationId
                };
            }
            return credentials[_currentCredentialIndex];
        }
    }

    /// <summary>
    /// Helper: Try to move to next available credential
    /// </summary>
    private bool TryNextCredential()
    {
        lock (_credentialLock)
        {
            var credentials = GetAvailableCredentials();
            if (_currentCredentialIndex < credentials.Count - 1)
            {
                _currentCredentialIndex++;
                var nextCredential = credentials[_currentCredentialIndex];
                _logger.LogInformation("Credential fallback: Now using credential pair {Index}/{Total}", 
                    _currentCredentialIndex + 1, credentials.Count);
                return true;
            }
            return false;
        }
    }

    /// <summary>
    /// Helper: Get list of available credentials
    /// </summary>
    private List<BodygramCredential> GetAvailableCredentials()
    {
        if (_settings.Credentials?.Count > 0)
        {
            return _settings.Credentials;
        }

        // Fallback: create list from legacy settings if no credentials configured
        if (!string.IsNullOrEmpty(_settings.ApiKey) && !string.IsNullOrEmpty(_settings.OrganizationId))
        {
            return new List<BodygramCredential>
            {
                new() { ApiKey = _settings.ApiKey, OrganizationId = _settings.OrganizationId }
            };
        }

        return new List<BodygramCredential>();
    }
}
