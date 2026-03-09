using VTOS.Domain.Common;

namespace VTOS.Domain.Entities;

public class School : AuditableEntity
{
    public string SchoolName { get; set; } = string.Empty;
    public string? LogoURL { get; set; }
    public string? ContactInfo { get; set; }
    public string? Level { get; set; }
    public Guid? CatalogID { get; set; }

    // Navigation properties
    public ICollection<ChildProfile> ChildProfiles { get; set; } = new List<ChildProfile>();
    public ICollection<Outfit> Outfits { get; set; } = new List<Outfit>();
    public ICollection<Campaign> Campaigns { get; set; } = new List<Campaign>();
    public ICollection<StudentDataImport> StudentDataImports { get; set; } = new List<StudentDataImport>();
    public SchoolWallet? Wallet { get; set; }
}

