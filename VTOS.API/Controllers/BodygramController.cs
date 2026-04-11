using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VTOS.API.Controllers.Dtos;
using VTOS.Application.Abstractions;
using VTOS.Application.Common.Models.BodygramDTOs;
using VTOS.Infrastructure.Bodygram.Helpers;

namespace VTOS.API.Controllers;

/// <summary>
/// Controller for Bodygram 3D body scanning integration
/// </summary>
[ApiController]
[Route("api/bodygram")]
[Authorize(Roles = "Parent")]
public class BodygramController : ControllerBase
{
    private readonly IBodygramService _bodygramService;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<BodygramController> _logger;

    public BodygramController(
        IBodygramService bodygramService,
        ICurrentUserService currentUserService,
        ILogger<BodygramController> logger)
    {
        _bodygramService = bodygramService;
        _currentUserService = currentUserService;
        _logger = logger;
    }

    /// <summary>
    /// Creates a new body scan from front and right side photos
    /// </summary>
    /// <remarks>
    /// This endpoint accepts two images (front and right side) along with user measurements
    /// and creates a 3D avatar using Bodygram's scanning technology.
    /// 
    /// Photo Requirements:
    /// - Format: JPEG only (.jpg or .jpeg)
    /// - Size: Maximum 3 MB per photo
    /// - Resolution: Either 1080 × 1920 or 720 × 1280 pixels
    /// 
    /// Input format (from user):
    /// - Weight: in kilograms (kg) - e.g., 54.5 for 54.5kg
    /// - Height: in centimeters (cm) - e.g., 164 for 164cm
    /// 
    /// The endpoint automatically converts:
    /// - Weight (kg) → grams (multiply by 1000)
    /// - Height (cm) → millimeters (multiply by 10)
    /// 
    /// The response includes:
    /// - Unique scan ID
    /// - 3D avatar model (OBJ format, base64 encoded)
    /// - Extracted body measurements (all in millimeters)
    /// </remarks>
    [HttpPost("create-scan")]
    [Consumes("multipart/form-data")]
    public async Task<ActionResult<BodygramScanResponse>> CreateScan(
        [FromForm] CreateBodygramScanRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Validate photo files (format, size, resolution)
            var frontPhotoError = ImageHelper.ValidatePhotoFile(request.FrontPhoto, "Front photo");
            if (frontPhotoError != null)
                return BadRequest(new { error = frontPhotoError });

            var rightPhotoError = ImageHelper.ValidatePhotoFile(request.RightPhoto, "Right photo");
            if (rightPhotoError != null)
                return BadRequest(new { error = rightPhotoError });

            // Validate user measurements
            if (request.Age <= 0 || request.Age > 150)
                return BadRequest(new { error = "Age must be between 1 and 150" });

            if (request.Weight <= 0 || request.Weight > 300)
                return BadRequest(new { error = "Weight must be between 0 and 300 kg" });

            if (request.Height <= 0 || request.Height > 250)
                return BadRequest(new { error = "Height must be between 0 and 250 cm" });

            if (!new[] { "male", "female" }.Contains(request.Gender.ToLower()))
                return BadRequest(new { error = "Gender must be 'male' or 'female'" });

            _logger.LogInformation("Creating Bodygram scan for age={Age}, weight={Weight}kg, height={Height}cm", 
                request.Age, request.Weight, request.Height);

            // Convert images to base64
            string frontPhotoBase64 = ImageHelper.ConvertImageToBase64(request.FrontPhoto.OpenReadStream());
            string rightPhotoBase64 = ImageHelper.ConvertImageToBase64(request.RightPhoto.OpenReadStream());

            // Convert user input (kg, cm) to Bodygram format (grams, mm)
            int weightInGrams = (int)(request.Weight * 1000); // kg to grams
            int heightInMm = request.Height * 10; // cm to mm

            // Create scan request
            var scanRequest = new CreateScanRequest
            {
                CustomScanId = request.CustomScanId ?? $"scan_{Guid.NewGuid()}",
                PhotoScan = new PhotoScanData
                {
                    Age = request.Age,
                    Weight = weightInGrams,
                    Height = heightInMm,
                    Gender = request.Gender.ToLower(),
                    FrontPhoto = frontPhotoBase64,
                    RightPhoto = rightPhotoBase64
                }
            };

            // Call Bodygram API
            var response = await _bodygramService.CreateScanAsync(scanRequest, cancellationToken);

            if (response?.Entry?.Status == "success")
            {
                _logger.LogInformation("Bodygram scan created successfully with ID: {ScanId}", response.Entry.Id);
                return Ok(response);
            }
            else
            {
                _logger.LogWarning("Bodygram scan creation returned status: {Status}", response?.Entry?.Status);
                return StatusCode(202, response); // Accepted but processing
            }
        }
        catch (VTOS.Infrastructure.Bodygram.BodygramValidationException ex)
        {
            _logger.LogWarning("Bodygram validation failed with {ErrorCount} errors", ex.Errors.Count);
            return BadRequest(new 
            { 
                error = "Bodygram validation failed",
                errors = ex.Errors,
                summary = VTOS.Infrastructure.Bodygram.Helpers.BodygramErrorHandler.CreateErrorSummary(ex.Errors)
            });
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Bodygram API request failed");
            return StatusCode(502, new { error = "Bodygram service error", details = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error creating scan");
            return StatusCode(500, new { error = "Internal server error", details = ex.Message });
        }
    }

    /// <summary>
    /// Retrieves all scans for the organization
    /// </summary>
    /// <remarks>
    /// Returns a list of all scans created for this organization on the Bodygram platform.
    /// </remarks>
    [HttpGet("scans")]
    public async Task<ActionResult<ScanListResponse>> GetAllScans(CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Retrieving all scans for organization");
            var response = await _bodygramService.GetScansAsync(cancellationToken);
            return Ok(response);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Bodygram API request failed");
            return StatusCode(502, new { error = "Bodygram service error", details = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error retrieving scans");
            return StatusCode(500, new { error = "Internal server error", details = ex.Message });
        }
    }

    /// <summary>
    /// Retrieves a specific scan by ID
    /// </summary>
    /// <param name="scanId">The Bodygram scan ID</param>
    /// <remarks>
    /// Returns detailed information about a specific scan including:
    /// - 3D avatar model
    /// - Body measurements
    /// - Scan status and metadata
    /// </remarks>
    [HttpGet("scans/{scanId}")]
    public async Task<ActionResult<BodygramScanResponse>> GetScan(
        string scanId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(scanId))
                return BadRequest(new { error = "Scan ID is required" });

            _logger.LogInformation("Retrieving scan with ID: {ScanId}", scanId);
            var response = await _bodygramService.GetScanAsync(scanId, cancellationToken);
            
            if (response?.Entry == null)
                return NotFound(new { error = "Scan not found" });

            return Ok(response);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Bodygram API request failed for scan {ScanId}", scanId);
            return StatusCode(502, new { error = "Bodygram service error", details = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error retrieving scan {ScanId}", scanId);
            return StatusCode(500, new { error = "Internal server error", details = ex.Message });
        }
    }

    /// <summary>
    /// Downloads the avatar model as an OBJ file
    /// </summary>
    /// <param name="scanId">The Bodygram scan ID</param>
    /// <remarks>
    /// Downloads the 3D avatar model for a scan as a downloadable OBJ file.
    /// The file can be imported into 3D modeling applications or AR engines.
    /// </remarks>
    [HttpGet("scans/{scanId}/avatar")]
    public async Task<IActionResult> DownloadAvatar(
        string scanId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(scanId))
                return BadRequest(new { error = "Scan ID is required" });

            var response = await _bodygramService.GetScanAsync(scanId, cancellationToken);
            
            if (response?.Entry?.Avatar?.Data == null)
                return NotFound(new { error = "Avatar not found for this scan" });

            // Convert base64 to bytes
            var avatarBytes = ImageHelper.ConvertBase64AvatarToBytes(response.Entry.Avatar.Data);

            // Return as file download
            return File(avatarBytes, "application/octet-stream", $"{scanId}.obj");
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Bodygram API request failed for scan {ScanId}", scanId);
            return StatusCode(502, new { error = "Bodygram service error", details = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error downloading avatar for scan {ScanId}", scanId);
            return StatusCode(500, new { error = "Internal server error", details = ex.Message });
        }
    }

    /// <summary>
    /// Gets body measurements from a scan
    /// </summary>
    /// <param name="scanId">The Bodygram scan ID</param>
    /// <remarks>
    /// Returns all extracted body measurements from a scan.
    /// These measurements can be used for fit analysis and size recommendations.
    /// </remarks>
    [HttpGet("scans/{scanId}/measurements")]
    public async Task<ActionResult<List<Measurement>>> GetMeasurements(
        string scanId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(scanId))
                return BadRequest(new { error = "Scan ID is required" });

            var response = await _bodygramService.GetScanAsync(scanId, cancellationToken);
            
            if (response?.Entry?.Measurements == null)
                return NotFound(new { error = "Measurements not found for this scan" });

            return Ok(response.Entry.Measurements);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Bodygram API request failed");
            return StatusCode(502, new { error = "Bodygram service error", details = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error retrieving measurements");
            return StatusCode(500, new { error = "Internal server error", details = ex.Message });
        }
    }

    /// <summary>
    /// Generates a scan token for Bodygram SDK client based on a specific child ID
    /// </summary>
    [HttpPost("scan-tokens")]
    [Authorize]
    public async Task<ActionResult<GenerateScanTokenResponse>> GenerateScanToken(
        [FromBody] ClientGenerateScanTokenRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var userId = _currentUserService.UserId;
            
            var response = await _bodygramService.GenerateScanTokenForChildAsync(request.ChildId, userId, cancellationToken);

            return Ok(response);
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(403, new { error = ex.Message });
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Bodygram API request failed");
            return StatusCode(502, new { error = "Bodygram service error", details = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error generating scan token");
            return StatusCode(500, new { error = "Internal server error", details = ex.Message });
        }
    }

    /// <summary>
    /// Completed a scan and updates the child profile logically
    /// </summary>
    [HttpPost("scans/complete")]
    [Authorize]
    public async Task<IActionResult> CompleteScan(
        [FromBody] CompleteScanRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var userId = _currentUserService.UserId;
            
            await _bodygramService.CompleteScanAsync(request.ChildId, userId, request.CustomScanId, request.BodygramScanId, cancellationToken);

            return Ok(new { message = "Bodygram scan results saved successfully." });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { error = ex.Message });
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(403, new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error completing scan update");
            return StatusCode(500, new { error = "Internal server error", details = ex.Message });
        }
    }

    [HttpGet("scans/status")]
    [Authorize]
    public async Task<ActionResult<BodygramScanStatusResponse>> GetScanStatus(
        [FromQuery] string customScanId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var userId = _currentUserService.UserId;
            var response = await _bodygramService.GetScanStatusAsync(customScanId, userId, cancellationToken);
            return Ok(response);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { error = ex.Message });
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(403, new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving scan status");
            return StatusCode(500, new { error = "Internal server error", details = ex.Message });
        }
    }

    [HttpGet("children/{childId:guid}/scans")]
    [Authorize]
    public async Task<ActionResult<IReadOnlyList<BodygramScanHistoryItemResponse>>> GetChildScans(
        Guid childId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var userId = _currentUserService.UserId;
            var response = await _bodygramService.GetChildScanHistoryAsync(childId, userId, cancellationToken);
            return Ok(response);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { error = ex.Message });
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(403, new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving child Bodygram scan history for child {ChildId}", childId);
            return StatusCode(500, new { error = "Internal server error", details = ex.Message });
        }
    }

    [HttpGet("records/{scanRecordId:guid}")]
    [Authorize]
    public async Task<ActionResult<BodygramScanDetailResponse>> GetScanRecordDetail(
        Guid scanRecordId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var userId = _currentUserService.UserId;
            var response = await _bodygramService.GetScanDetailAsync(scanRecordId, userId, cancellationToken);
            return Ok(response);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { error = ex.Message });
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(403, new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving Bodygram scan record detail {ScanRecordId}", scanRecordId);
            return StatusCode(500, new { error = "Internal server error", details = ex.Message });
        }
    }
}
