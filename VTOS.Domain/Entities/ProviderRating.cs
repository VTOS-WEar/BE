using VTOS.Domain.Common;

namespace VTOS.Domain.Entities;

public class ProviderRating : AuditableEntity
{
    public Guid ProviderID { get; set; }
    public Guid OrderID { get; set; }
    public Guid ParentUserID { get; set; }
    public int Rating { get; set; }
    public string? Comment { get; set; }

    public Provider Provider { get; set; } = null!;
    public Order Order { get; set; } = null!;
    public User ParentUser { get; set; } = null!;
}
