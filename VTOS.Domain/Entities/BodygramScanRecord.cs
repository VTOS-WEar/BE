using System.ComponentModel.DataAnnotations.Schema;
using VTOS.Domain.Common;

namespace VTOS.Domain.Entities;

public class BodygramScanRecord : AuditableEntity
{
    public Guid ChildId { get; set; }
    public string BodygramScanId { get; set; } = string.Empty;
    public string CustomScanId { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime ScannedAt { get; set; }
    public long CreatedAtUnix { get; set; }
    public int HeightCm { get; set; }
    public float WeightKg { get; set; }
    public string? AvatarUrl { get; set; }
    public string? AvatarFormat { get; set; }
    public string? AvatarType { get; set; }
    public string? RawInputJson { get; set; }
    public string? RawMeasurementsJson { get; set; }
    public double? WaistToHipRatio { get; set; }

    [ForeignKey(nameof(ChildId))]
    public ChildProfile Child { get; set; } = null!;

    public ICollection<BodygramMeasurementRecord> Measurements { get; set; } = new List<BodygramMeasurementRecord>();
}
