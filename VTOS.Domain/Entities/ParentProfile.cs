using VTOS.Domain.Common;
using VTOS.Domain.Enums;

namespace VTOS.Domain.Entities;

/// <summary>
/// Parent-specific profile data (1:1 with User where Role = Parent).
/// Stores DOB, Gender that are only relevant for parent users.
/// </summary>
public class ParentProfile : BaseEntity
{
    public Guid UserID { get; set; }
    public DateTime? DOB { get; set; }
    public Gender Gender { get; set; } = Gender.Other;

    // Navigation properties
    public User User { get; set; } = null!;
}
