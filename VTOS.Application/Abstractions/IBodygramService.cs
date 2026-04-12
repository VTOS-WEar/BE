using VTOS.Application.Common.Models.BodygramDTOs;
namespace VTOS.Application.Abstractions;

/// <summary>
/// Service for interacting with Bodygram API for 3D body scanning and avatar generation
/// </summary>
public interface IBodygramService
{
    /// <summary>
    /// Creates a new body scan in Bodygram with photo data
    /// </summary>
    /// <param name="request">Scan creation request with photos and user measurements</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Response with scan data including 3D avatar and measurements</returns>
    Task<BodygramScanResponse> CreateScanAsync(CreateScanRequest request, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Retrieves list of all scans for the organization
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of scans</returns>
    Task<ScanListResponse> GetScansAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Retrieves a specific scan by ID
    /// </summary>
    /// <param name="scanId">ID of the scan to retrieve</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Scan data including 3D avatar and measurements</returns>
    Task<BodygramScanResponse> GetScanAsync(string scanId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Generates a scan token for a specific child after validating parent permissions
    /// </summary>
    Task<GenerateScanTokenResponse> GenerateScanTokenForChildAsync(Guid childId, Guid parentId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Completes the scan session, logs the results and updates the child profile
    /// </summary>
    Task CompleteScanAsync(Guid childId, Guid parentId, string customScanId, string bodygramScanId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves scan session status for a specific child owned by the current parent
    /// </summary>
    Task<BodygramScanStatusResponse> GetScanStatusAsync(string customScanId, Guid parentId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves saved Bodygram scan history for a child owned by the current parent with pagination and filtering.
    /// </summary>
    Task<PaginatedBodygramScanHistoryResponse> GetChildScanHistoryAsync(
        Guid childId, 
        Guid parentId, 
        int pageNumber = 1, 
        int pageSize = 3, 
        DateTime? startDate = null, 
        DateTime? endDate = null, 
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves a saved Bodygram scan detail owned by the current parent.
    /// </summary>
    Task<BodygramScanDetailResponse> GetScanDetailAsync(Guid scanRecordId, Guid parentId, CancellationToken cancellationToken = default);
}
