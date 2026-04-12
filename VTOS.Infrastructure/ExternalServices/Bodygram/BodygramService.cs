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
    private readonly IImageUploadService _imageUploadService;

    // Shared JsonSerializerOptions
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private static readonly Dictionary<string, string> MeasurementLabels = new(StringComparer.OrdinalIgnoreCase)
    {
        ["bustGirth"] = "Bust",
        ["waistGirth"] = "Waist",
        ["hipGirth"] = "Hip",
        ["upperArmGirthR"] = "Upper Arm",
        ["thighGirthR"] = "Thigh",
        ["calfGirthR"] = "Calf"
    };
    private const int TrialScanLimit = 5;
    private const int PollingTimeoutMinutes = 10;
    private static readonly TimeSpan PollingTimeout = TimeSpan.FromMinutes(PollingTimeoutMinutes);

    // Track current credential index for fallback strategy (sequential)
    private static int _currentCredentialIndex = 0;
    private static readonly object _credentialLock = new();

    public BodygramService(
        HttpClient httpClient,
        IOptions<BodygramSettings> options,
        ILogger<BodygramService> logger,
        IApplicationDbContext dbContext,
        IImageUploadService imageUploadService)
    {
        _httpClient = httpClient;
        _settings = options.Value;
        _logger = logger;
        _dbContext = dbContext;
        _imageUploadService = imageUploadService;
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

            await SelectCredentialForScanTokenAsync(cancellationToken);

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
                        OrganizationId = credential.OrganizationId,
                        CreatedAt = DateTime.UtcNow,
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
                var response = !string.IsNullOrWhiteSpace(scanLog.OrganizationId)
                    ? await GetScanAsync(bodygramScanId, scanLog.OrganizationId, cancellationToken)
                    : await GetScanAsync(bodygramScanId, cancellationToken);
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

                    await UpsertScanRecordAsync(scanLog.Child, response.Entry, cancellationToken);
                }
            }

            scanLog.Status = VTOS.Domain.Enums.BodygramScanStatus.Completed;
            scanLog.BodygramScanId = bodygramScanId;
            scanLog.UpdatedAt = DateTime.UtcNow;
            
            await CleanupRedundantScanLogsAsync(scanLog.ChildId, scanLog.CustomScanId, cancellationToken);
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
            var createdAtUtc = scanLog.CreatedAt == default
                ? DateTime.UtcNow
                : DateTime.SpecifyKind(scanLog.CreatedAt, DateTimeKind.Utc);

            if (DateTime.UtcNow - createdAtUtc >= PollingTimeout)
            {
                scanLog.Status = VTOS.Domain.Enums.BodygramScanStatus.Failed;
                scanLog.UpdatedAt = DateTime.UtcNow;
                await CleanupRedundantScanLogsAsync(scanLog.ChildId, scanLog.CustomScanId, cancellationToken);
                await _dbContext.SaveChangesAsync(cancellationToken);
            }
            else
            {
                await TrySyncPendingScanAsync(scanLog, parentId, cancellationToken);
            }
        }

        return new BodygramScanStatusResponse
        {
            Status = scanLog.Status.ToString(),
            ChildId = scanLog.ChildId,
            BodygramScanId = scanLog.BodygramScanId,
            HeightCm = scanLog.Status == VTOS.Domain.Enums.BodygramScanStatus.Completed ? scanLog.Child.HeightCm : null,
            WeightKg = scanLog.Status == VTOS.Domain.Enums.BodygramScanStatus.Completed ? scanLog.Child.WeightKg : null,
            Message = scanLog.Status == VTOS.Domain.Enums.BodygramScanStatus.Failed && string.IsNullOrWhiteSpace(scanLog.BodygramScanId)
                ? $"Khong nhan duoc ket qua scan sau {PollingTimeoutMinutes} phut polling. Vui long tao phien quet moi."
                : null,
            TimeoutMinutes = PollingTimeoutMinutes
        };
    }

    public async Task<PaginatedBodygramScanHistoryResponse> GetChildScanHistoryAsync(
        Guid childId, 
        Guid parentId, 
        int pageNumber = 1, 
        int pageSize = 3, 
        DateTime? startDate = null, 
        DateTime? endDate = null, 
        CancellationToken cancellationToken = default)
    {
        var child = await _dbContext.ChildProfiles
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == childId, cancellationToken);

        if (child == null)
        {
            throw new KeyNotFoundException("Student not found.");
        }

        if (child.ParentUserID != parentId)
        {
            throw new UnauthorizedAccessException("You do not have permission to view this student's Bodygram history.");
        }

        var query = _dbContext.BodygramScanRecords
            .AsNoTracking()
            .Include(r => r.Measurements)
            .Where(r => r.ChildId == childId);

        if (startDate.HasValue)
        {
            var start = startDate.Value.Date;
            query = query.Where(r => r.ScannedAt >= start);
        }

        if (endDate.HasValue)
        {
            var end = endDate.Value.Date.AddDays(1).AddTicks(-1);
            query = query.Where(r => r.ScannedAt <= end);
        }

        var totalCount = await query.CountAsync(cancellationToken);
        
        var records = await query
            .OrderByDescending(r => r.ScannedAt)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        var items = records.Select(record => new BodygramScanHistoryItemResponse
        {
            ScanRecordId = record.Id,
            ChildId = record.ChildId,
            ChildName = child.FullName,
            ScannedAt = record.ScannedAt,
            Status = record.Status,
            HeightCm = record.HeightCm,
            WeightKg = record.WeightKg,
            BustCm = GetMeasurementCm(record.Measurements, "bustGirth"),
            WaistGirthCm = GetMeasurementCm(record.Measurements, "waistGirth"),
            HipGirthCm = GetMeasurementCm(record.Measurements, "hipGirth"),
            WaistToHipRatio = record.WaistToHipRatio,
            AvatarThumbnailUrl = record.AvatarUrl
        }).ToList();

        return new PaginatedBodygramScanHistoryResponse
        {
            Items = items,
            TotalCount = totalCount,
            TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize),
            CurrentPage = pageNumber
        };
    }

    public async Task<BodygramScanDetailResponse> GetScanDetailAsync(Guid scanRecordId, Guid parentId, CancellationToken cancellationToken = default)
    {
        var record = await _dbContext.BodygramScanRecords
            .AsNoTracking()
            .Include(r => r.Child)
            .Include(r => r.Measurements)
            .FirstOrDefaultAsync(r => r.Id == scanRecordId, cancellationToken);

        if (record == null)
        {
            throw new KeyNotFoundException("Bodygram scan record not found.");
        }

        if (record.Child.ParentUserID != parentId)
        {
            throw new UnauthorizedAccessException("You do not have permission to view this scan.");
        }

        return new BodygramScanDetailResponse
        {
            ScanRecordId = record.Id,
            ChildId = record.ChildId,
            ChildName = record.Child.FullName,
            BodygramScanId = record.BodygramScanId,
            CustomScanId = record.CustomScanId,
            Status = record.Status,
            ScannedAt = record.ScannedAt,
            HeightCm = record.HeightCm,
            WeightKg = record.WeightKg,
            AvatarUrl = record.AvatarUrl,
            AvatarFormat = record.AvatarFormat,
            AvatarType = record.AvatarType,
            WaistToHipRatio = record.WaistToHipRatio,
            RiskLevel = GetRiskLevel(record.WaistToHipRatio),
            BustCm = GetMeasurementCm(record.Measurements, "bustGirth"),
            WaistCm = GetMeasurementCm(record.Measurements, "waistGirth"),
            HipCm = GetMeasurementCm(record.Measurements, "hipGirth"),
            UpperArmCm = GetMeasurementCm(record.Measurements, "upperArmGirthR"),
            ThighCm = GetMeasurementCm(record.Measurements, "thighGirthR"),
            CalfCm = GetMeasurementCm(record.Measurements, "calfGirthR"),
            Gender = record.Child.Gender == VTOS.Domain.Enums.Gender.Female ? "female" : "male",
            Measurements = record.Measurements
                .OrderBy(m => m.Name)
                .Select(m => new BodygramMeasurementDetailItem
                {
                    Name = m.Name,
                    Label = MeasurementLabels.TryGetValue(m.Name, out var label) ? label : m.Name,
                    Unit = m.Unit,
                    Value = m.Value,
                    ValueCm = string.Equals(m.Unit, "mm", StringComparison.OrdinalIgnoreCase)
                        ? Math.Round(m.Value / 10d, 1)
                        : null
                })
                .ToList()
        };
    }

    private async Task TrySyncPendingScanAsync(BodygramScanLog scanLog, Guid parentId, CancellationToken cancellationToken)
    {
        try
        {
            var scans = !string.IsNullOrWhiteSpace(scanLog.OrganizationId)
                ? await GetScansAsync(scanLog.OrganizationId, cancellationToken)
                : await GetScansAsync(cancellationToken);
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
                scanLog.UpdatedAt = DateTime.UtcNow;
                await CleanupRedundantScanLogsAsync(scanLog.ChildId, scanLog.CustomScanId, cancellationToken);
                await _dbContext.SaveChangesAsync(cancellationToken);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Unable to sync Bodygram pending scan {CustomScanId} during status polling.", scanLog.CustomScanId);
        }
    }

    private async Task<ScanListResponse> GetScansAsync(string organizationId, CancellationToken cancellationToken = default)
    {
        var credential = GetCredentialByOrganizationId(organizationId);
        return credential == null
            ? await GetScansAsync(cancellationToken)
            : await GetScansForCredentialAsync(credential, cancellationToken);
    }

    private async Task<BodygramScanResponse> GetScanAsync(string scanId, string organizationId, CancellationToken cancellationToken = default)
    {
        var credential = GetCredentialByOrganizationId(organizationId);
        return credential == null
            ? await GetScanAsync(scanId, cancellationToken)
            : await GetScanForCredentialAsync(scanId, credential, cancellationToken);
    }

    private async Task UpsertScanRecordAsync(ChildProfile child, ScanEntry entry, CancellationToken cancellationToken)
    {
        var existingRecord = await _dbContext.BodygramScanRecords
            .Include(r => r.Measurements)
            .FirstOrDefaultAsync(r => r.BodygramScanId == entry.Id || r.CustomScanId == entry.CustomScanId, cancellationToken);

        var scanRecord = existingRecord ?? new BodygramScanRecord
        {
            Id = Guid.NewGuid(),
            ChildId = child.Id,
            BodygramScanId = entry.Id,
            CustomScanId = entry.CustomScanId,
            CreatedAt = DateTime.UtcNow
        };

        scanRecord.ChildId = child.Id;
        scanRecord.BodygramScanId = entry.Id;
        scanRecord.CustomScanId = entry.CustomScanId;
        scanRecord.Status = entry.Status;
        scanRecord.ScannedAt = DateTimeOffset.FromUnixTimeSeconds(entry.CreatedAt).UtcDateTime;
        scanRecord.CreatedAtUnix = entry.CreatedAt;
        scanRecord.HeightCm = child.HeightCm;
        scanRecord.WeightKg = child.WeightKg;
        scanRecord.RawInputJson = entry.Input == null ? null : JsonSerializer.Serialize(entry.Input, JsonOptions);
        scanRecord.RawMeasurementsJson = JsonSerializer.Serialize(entry.Measurements ?? new List<Measurement>(), JsonOptions);
        scanRecord.WaistToHipRatio = CalculateWaistToHipRatio(entry.Measurements);
        scanRecord.AvatarFormat = entry.Avatar?.Format;
        scanRecord.AvatarType = entry.Avatar?.Type;
        scanRecord.UpdatedAt = DateTime.UtcNow;

        if (!string.IsNullOrWhiteSpace(entry.Avatar?.Data))
        {
            var avatarBytes = ImageHelper.ConvertBase64AvatarToBytes(entry.Avatar.Data);
            await using var avatarStream = new MemoryStream(avatarBytes);
            scanRecord.AvatarUrl = await _imageUploadService.UploadAsync(
                avatarStream,
                $"{entry.Id}.{entry.Avatar.Format}",
                $"bodygram/{child.Id}",
                cancellationToken);
        }

        if (existingRecord == null)
        {
            _dbContext.BodygramScanRecords.Add(scanRecord);
        }
        else if (existingRecord.Measurements.Count > 0)
        {
            _dbContext.BodygramMeasurementRecords.RemoveRange(existingRecord.Measurements);
        }

        if (entry.Measurements?.Count > 0)
        {
            var measurements = entry.Measurements.Select(m => new BodygramMeasurementRecord
            {
                Id = Guid.NewGuid(),
                ScanRecordId = scanRecord.Id,
                Name = m.Name,
                Unit = m.Unit,
                Value = m.Value
            });

            _dbContext.BodygramMeasurementRecords.AddRange(measurements);
        }
    }

    private async Task CleanupRedundantScanLogsAsync(Guid childId, string currentCustomScanId, CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        var logsToRemove = await _dbContext.BodygramScanLogs
            .Where(l => l.ChildId == childId && l.CustomScanId != currentCustomScanId)
            .Where(l =>
                l.Status != VTOS.Domain.Enums.BodygramScanStatus.Pending ||
                (l.CreatedAt != default && now - l.CreatedAt >= PollingTimeout))
            .ToListAsync(cancellationToken);

        if (logsToRemove.Count == 0)
        {
            return;
        }

        _dbContext.BodygramScanLogs.RemoveRange(logsToRemove);
    }

    private static double? GetMeasurementCm(IEnumerable<BodygramMeasurementRecord> measurements, string measurementName)
    {
        var measurement = measurements.FirstOrDefault(m => string.Equals(m.Name, measurementName, StringComparison.OrdinalIgnoreCase));
        if (measurement == null)
        {
            return null;
        }

        return string.Equals(measurement.Unit, "mm", StringComparison.OrdinalIgnoreCase)
            ? Math.Round(measurement.Value / 10d, 1)
            : Math.Round(measurement.Value, 1);
    }

    private static double? CalculateWaistToHipRatio(IEnumerable<Measurement> measurements)
    {
        var waist = measurements.FirstOrDefault(m => string.Equals(m.Name, "waistGirth", StringComparison.OrdinalIgnoreCase));
        var hip = measurements.FirstOrDefault(m => string.Equals(m.Name, "hipGirth", StringComparison.OrdinalIgnoreCase));

        if (waist == null || hip == null || hip.Value <= 0)
        {
            return null;
        }

        return Math.Round(waist.Value / hip.Value, 2);
    }

    private static string? GetRiskLevel(double? waistToHipRatio)
    {
        if (waistToHipRatio == null)
        {
            return null;
        }

        if (waistToHipRatio < 0.80d)
        {
            return "Low risk";
        }

        if (waistToHipRatio <= 0.85d)
        {
            return "Moderate risk";
        }

        return "High risk";
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

    private async Task SelectCredentialForScanTokenAsync(CancellationToken cancellationToken)
    {
        var credentials = GetAvailableCredentials();
        if (credentials.Count <= 1)
        {
            return;
        }

        var startIndex = GetCurrentCredentialIndex();

        for (int offset = 0; offset < credentials.Count; offset++)
        {
            var index = (startIndex + offset) % credentials.Count;
            var credential = credentials[index];
            var scanCount = await GetScanCountForCredentialAsync(credential, cancellationToken);

            _logger.LogInformation(
                "Bodygram credential {Index}/{Total} for organization {OrganizationId} currently has {ScanCount} scans.",
                index + 1,
                credentials.Count,
                credential.OrganizationId,
                scanCount);

            if (scanCount < TrialScanLimit)
            {
                SetCurrentCredentialIndex(index);
                return;
            }
        }

        throw new HttpRequestException($"Bodygram API: All available organizations have reached the configured trial scan limit of {TrialScanLimit} scans.");
    }

    private async Task<int> GetScanCountForCredentialAsync(BodygramCredential credential, CancellationToken cancellationToken)
    {
        var scansResponse = await GetScansForCredentialAsync(credential, cancellationToken);
        return scansResponse?.Results?.Count ?? 0;
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

    private int GetCurrentCredentialIndex()
    {
        lock (_credentialLock)
        {
            return _currentCredentialIndex;
        }
    }

    private void SetCurrentCredentialIndex(int index)
    {
        lock (_credentialLock)
        {
            _currentCredentialIndex = index;
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

    private BodygramCredential? GetCredentialByOrganizationId(string organizationId)
    {
        return GetAvailableCredentials()
            .FirstOrDefault(c => string.Equals(c.OrganizationId, organizationId, StringComparison.Ordinal));
    }

    private async Task<ScanListResponse> GetScansForCredentialAsync(BodygramCredential credential, CancellationToken cancellationToken)
    {
        var url = $"{_settings.BaseUrl}/orgs/{credential.OrganizationId}/scans";
        var request = CreateAuthenticatedRequest(HttpMethod.Get, url, credential);
        var response = await _httpClient.SendAsync(request, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return new ScanListResponse();
            }

            await HandleErrorResponseAsync(response, $"Retrieve scans for organization {credential.OrganizationId}", cancellationToken);
        }

        var jsonResponse = await response.Content.ReadAsStringAsync(cancellationToken);
        return DeserializeResponse<ScanListResponse>(jsonResponse);
    }

    private async Task<BodygramScanResponse> GetScanForCredentialAsync(string scanId, BodygramCredential credential, CancellationToken cancellationToken)
    {
        var url = $"{_settings.BaseUrl}/orgs/{credential.OrganizationId}/scans/{scanId}";
        var httpRequest = CreateAuthenticatedRequest(HttpMethod.Get, url, credential);
        var response = await _httpClient.SendAsync(httpRequest, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            await HandleErrorResponseAsync(response, $"Retrieve scan {scanId} for organization {credential.OrganizationId}", cancellationToken);
        }

        var jsonResponse = await response.Content.ReadAsStringAsync(cancellationToken);
        return DeserializeResponse<BodygramScanResponse>(jsonResponse);
    }
}
