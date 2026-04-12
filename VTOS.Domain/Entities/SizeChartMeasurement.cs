using VTOS.Domain.Common;

namespace VTOS.Domain.Entities;

public class SizeChartMeasurement : AuditableEntity
{
    public Guid SizeChartDetailId { get; set; }
    public string FieldKey { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Unit { get; set; } = "cm";
    public decimal? MinCm { get; set; }
    public decimal? MaxCm { get; set; }

    public SizeChartDetail SizeChartDetail { get; set; } = null!;
}
