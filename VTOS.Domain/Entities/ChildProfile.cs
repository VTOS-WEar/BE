using VTOS.Domain.Common;
using VTOS.Domain.Enums;

namespace VTOS.Domain.Entities;

/// <summary>
/// Represents a child profile (student) in the system.
/// Maps to the Children table in the database.
/// </summary>
public class ChildProfile : BaseEntity
{
    public Guid? ParentUserID { get; set; }  // null = student imported, not yet linked to a parent
    public string FullName { get; set; } = string.Empty;
    public int Age { get; set; }
    public string Grade { get; set; } = string.Empty;
    public Gender Gender { get; set; }
    public Guid SchoolID { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime? DOB { get; set; }
    public string Avatar { get; set; } = string.Empty;
    public int HeightCm { get; set; }
    public float WeightKg { get; set; }

    // Navigation properties
    public User ParentUser { get; set; } = null!;
    public School School { get; set; } = null!;
    public ICollection<TryOnHistory> TryOnHistories { get; set; } = new List<TryOnHistory>();
    public ICollection<Order> Orders { get; set; } = new List<Order>();
    public ICollection<StudentDataImport> StudentDataImports { get; set; } = new List<StudentDataImport>();
}
