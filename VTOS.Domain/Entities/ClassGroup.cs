using VTOS.Domain.Common;

namespace VTOS.Domain.Entities;

public class ClassGroup : BaseEntity
{
    public Guid SchoolID { get; set; }
    public string ClassName { get; set; } = string.Empty;
    public string Grade { get; set; } = string.Empty;
    public string AcademicYear { get; set; } = string.Empty;
    public Guid? HomeroomTeacherID { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public School School { get; set; } = null!;
    public User? HomeroomTeacher { get; set; }
    public ICollection<ChildProfile> Students { get; set; } = new List<ChildProfile>();
}
