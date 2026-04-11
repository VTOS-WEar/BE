using System.ComponentModel.DataAnnotations.Schema;
using VTOS.Domain.Common;
using VTOS.Domain.Enums;

namespace VTOS.Domain.Entities;

public class BodygramScanLog : AuditableEntity
{
    public Guid ChildId { get; set; }
    public string CustomScanId { get; set; } = string.Empty;
    public string? BodygramScanId { get; set; }
    public BodygramScanStatus Status { get; set; } = BodygramScanStatus.Pending;

    [ForeignKey("ChildId")]
    public ChildProfile Child { get; set; } = null!;
}
