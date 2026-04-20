using VTOS.Domain.Common;

namespace VTOS.Domain.Entities;

public class SemesterPublicationOutfit : AuditableEntity
{
    public Guid SemesterPublicationID { get; set; }
    public Guid OutfitID { get; set; }
    public string? Notes { get; set; }

    public SemesterPublication SemesterPublication { get; set; } = null!;
    public Outfit Outfit { get; set; } = null!;
}
