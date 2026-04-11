namespace VTOS.API.Controllers.Dtos;

public class CompleteScanRequest
{
    public Guid ChildId { get; set; }
    public string CustomScanId { get; set; } = string.Empty;
    public string BodygramScanId { get; set; } = string.Empty;
}
