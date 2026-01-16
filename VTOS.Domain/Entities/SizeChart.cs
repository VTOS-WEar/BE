using VTOS.Domain.Common;

namespace VTOS.Domain.Entities;

public class SizeChart : AuditableEntity
{
    public string ChartName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Unit { get; set; } = "cm";

    // Navigation properties
    public ICollection<Outfit> Outfits { get; set; } = new List<Outfit>();
    public ICollection<SizeChartDetail> SizeChartDetails { get; set; } = new List<SizeChartDetail>();
}

