namespace VTOS.Application.Common.Models.BodygramDTOs;

public class BodygramScanStatusResponse
{
    public string Status { get; set; } = string.Empty;
    public Guid ChildId { get; set; }
    public string? BodygramScanId { get; set; }
    public int? HeightCm { get; set; }
    public float? WeightKg { get; set; }
}
