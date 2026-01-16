using VTOS.Domain.Common;

namespace VTOS.Domain.Entities;

public class StudentDataImport : AuditableEntity
{
    public Guid SchoolID { get; set; }
    public string StudentCode { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string? Class { get; set; }
    public string? ParentPhone { get; set; }
    public DateTime? DateOfBirth { get; set; }
    public bool IsRegistered { get; set; }
    public Guid? MatchedChildID { get; set; }

    // Navigation properties
    public School School { get; set; } = null!;
    public ChildProfile? MatchedChildProfile { get; set; }
}

