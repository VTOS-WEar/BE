using VTOS.Domain.Common;

namespace VTOS.Domain.Entities;

/// <summary>
/// One line item in a Contract — an outfit sample attached to the supplier agreement.
/// Legacy pricing and quantity fields are retained for backward compatibility only.
/// </summary>
public class ContractItem : BaseEntity
{
    public Guid ContractID { get; set; }
    public Guid OutfitID { get; set; }

    /// <summary>Negotiated price per unit for this outfit.</summary>
    public decimal PricePerUnit { get; set; }

    /// <summary>Minimum quantity Provider will produce.</summary>
    public int MinQuantity { get; set; }

    /// <summary>Maximum quantity Provider can produce.</summary>
    public int MaxQuantity { get; set; }

    // Navigation
    public Contract Contract { get; set; } = null!;
    public Outfit Outfit { get; set; } = null!;
}
