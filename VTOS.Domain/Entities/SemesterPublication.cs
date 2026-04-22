using VTOS.Domain.Common;
using VTOS.Domain.Enums;

namespace VTOS.Domain.Entities;

public class SemesterPublication : AuditableEntity
{
    public Guid SchoolID { get; set; }
    public string Semester { get; set; } = string.Empty;
    public string AcademicYear { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public SemesterPublicationStatus Status { get; set; } = SemesterPublicationStatus.Draft;
    public string? Description { get; set; }
    public string? Rules { get; set; }

    public School School { get; set; } = null!;
    public ICollection<SemesterPublicationOutfit> Outfits { get; set; } = new List<SemesterPublicationOutfit>();
    public ICollection<SemesterPublicationProvider> Providers { get; set; } = new List<SemesterPublicationProvider>();
    public ICollection<Order> Orders { get; set; } = new List<Order>();
}
