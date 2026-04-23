using VTOS.Domain.Common;
using VTOS.Domain.Enums;

namespace VTOS.Domain.Entities;

public class SemesterPublicationProvider : AuditableEntity
{
    public Guid SemesterPublicationID { get; set; }
    public Guid ProviderID { get; set; }
    public Guid? ContractID { get; set; }
    public SemPublicationProviderStatus Status { get; set; } = SemPublicationProviderStatus.Active;
    public DateTime? SuspendedAt { get; set; }
    public string? SuspendReason { get; set; }

    public SemesterPublication SemesterPublication { get; set; } = null!;
    public Provider Provider { get; set; } = null!;
    public Contract? Contract { get; set; }
    public ICollection<ProviderCatalogItem> ProviderCatalogItems { get; set; } = new List<ProviderCatalogItem>();
}
