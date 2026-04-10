namespace VTOS.Application.Common.Models.BodygramDTOs;

public class GenerateScanTokenRequest
{
    public string CustomScanId { get; set; } = string.Empty;
    public int Lifetime { get; set; } = 3600;
    public List<string> Scope { get; set; } = new() { "api.platform.bodygram.com/scans:create" };
}
