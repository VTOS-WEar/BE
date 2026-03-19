using VTOS.Domain.Common;

namespace VTOS.Domain.Entities;

/// <summary>
/// Links a User (with Role = School) to the School they manage.
/// Replaces the nullable User.SchoolID FK.
/// </summary>
public class SchoolManager : BaseEntity
{
    public Guid UserID { get; set; }
    public Guid SchoolID { get; set; }

    // Navigation properties
    public User User { get; set; } = null!;
    public School School { get; set; } = null!;
}
