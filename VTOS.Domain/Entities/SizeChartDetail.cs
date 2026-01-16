using VTOS.Domain.Common;

namespace VTOS.Domain.Entities;

public class SizeChartDetail : AuditableEntity
{
    public Guid SizeChartID { get; set; }
    public string SizeLabel { get; set; } = string.Empty;
    public decimal? ChestMin { get; set; }
    public decimal? ChestMax { get; set; }
    public decimal? WaistMin { get; set; }
    public decimal? WaistMax { get; set; }
    public decimal? HipMin { get; set; }
    public decimal? HipMax { get; set; }
    public decimal? HeightMin { get; set; }
    public decimal? HeightMax { get; set; }
    public string? OtherMeasurements { get; set; }

    // Navigation properties
    public SizeChart SizeChart { get; set; } = null!;
}

