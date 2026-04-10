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
    /// Generates a scan token for client-side scanning
    /// </summary>
    /// <param name="request">Request parameters for scan token</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Token and Expiry</returns>
    Task<GenerateScanTokenResponse> GenerateScanTokenAsync(GenerateScanTokenRequest request, CancellationToken cancellationToken = default);
}
