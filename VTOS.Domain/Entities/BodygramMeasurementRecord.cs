using System.ComponentModel.DataAnnotations.Schema;
using VTOS.Domain.Common;

namespace VTOS.Domain.Entities;

public class BodygramMeasurementRecord : BaseEntity
{
    public Guid ScanRecordId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Unit { get; set; } = string.Empty;
    public double Value { get; set; }

    [ForeignKey(nameof(ScanRecordId))]
    public BodygramScanRecord ScanRecord { get; set; } = null!;
}
