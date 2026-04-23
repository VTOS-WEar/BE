using VTOS.Domain.Common;

namespace VTOS.Domain.Entities;

public class ParentAddress : AuditableEntity
{
    public Guid ParentUserID { get; set; }
    public string Label { get; set; } = string.Empty;
    public string RecipientName { get; set; } = string.Empty;
    public string RecipientPhone { get; set; } = string.Empty;
    public string AddressLine { get; set; } = string.Empty;
    public bool IsDefault { get; set; }

    public User ParentUser { get; set; } = null!;
}
