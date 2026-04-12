namespace VTOS.Application.Common.Models.BodygramDTOs;

public class GenerateScanTokenResponse
{
    public string Token { get; set; } = string.Empty;
    public long ExpiresAt { get; set; }
    public string CustomScanId { get; set; } = string.Empty;
    public string ScannerUrl { get; set; } = string.Empty;
}
