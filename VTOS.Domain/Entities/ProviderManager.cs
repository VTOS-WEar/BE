using VTOS.Domain.Common;

namespace VTOS.Domain.Entities;

/// <summary>
/// Links a User (with Role = Provider) to the Provider they manage.
/// Replaces the nullable User.ProviderID FK.
/// </summary>
public class ProviderManager : BaseEntity
{
    public Guid UserID { get; set; }
    public Guid ProviderID { get; set; }

    // Navigation properties
    public User User { get; set; } = null!;
    public Provider Provider { get; set; } = null!;
}
