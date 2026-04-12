using VTOS.Domain.Common;

namespace VTOS.Domain.Entities;

public class SizeChartDetail : AuditableEntity
{
    public Guid SizeChartID { get; set; }
    public string SizeLabel { get; set; } = string.Empty;

    // Navigation properties
    public SizeChart SizeChart { get; set; } = null!;
    public ICollection<SizeChartMeasurement> Measurements { get; set; } = new List<SizeChartMeasurement>();
}
