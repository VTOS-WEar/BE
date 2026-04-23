using VTOS.Domain.Common;
using VTOS.Domain.Enums;

namespace VTOS.Domain.Entities;

public class School : AuditableEntity
{
    public string SchoolName { get; set; } = string.Empty;
    public string? LogoURL { get; set; }
    public string? ContactInfo { get; set; }
    public string? Level { get; set; }
    public bool IsDeleted { get; set; }

    // ── Contract template fields (filled by school admin in profile) ─────────
    public string? Address { get; set; }
    public string? TaxCode { get; set; }
    public string? RepresentativeName { get; set; }
    public string? RepresentativeTitle { get; set; }
    public string? Phone { get; set; }
    public Guid? CatalogID { get; set; }
    
    // Status
    public SchoolStatus Status { get; set; } = SchoolStatus.Pending;
    
    // Verification fields
    public VerificationStatus VerificationStatus { get; set; } = VerificationStatus.Pending;
    public string? RejectionReason { get; set; }
    public string? VerificationDocumentUrl { get; set; }

    // Navigation properties
    public ICollection<ChildProfile> ChildProfiles { get; set; } = new List<ChildProfile>();
    public ICollection<Outfit> Outfits { get; set; } = new List<Outfit>();
    public ICollection<SemesterPublication> SemesterPublications { get; set; } = new List<SemesterPublication>();
    public ICollection<ClassGroup> ClassGroups { get; set; } = new List<ClassGroup>();
    public ICollection<StudentDataImport> StudentDataImports { get; set; } = new List<StudentDataImport>();
    public ICollection<Contract> Contracts { get; set; } = new List<Contract>();
    public Wallet? Wallet { get; set; }
    
}

