using VTOS.Domain.Common;
using VTOS.Domain.Enums;

namespace VTOS.Domain.Entities;

/// <summary>
/// Represents a production batch / production order for a campaign.
/// Created when a school generates a production order (UC 3.9.8).
/// Sent to a provider (UC 3.9.9), confirmed (UC 3.9.10),
/// then processed (UC 3.9.17) or rejected (UC 3.9.18).
/// </summary>
public class ProductionBatch : BaseEntity
{
    public Guid CampaignID { get; set; }
    public Guid ProviderID { get; set; }
    public string BatchName { get; set; } = string.Empty;
    public int TotalQuantity { get; set; }
    public DateTime CreatedDate { get; set; }
    public ProductionBatchStatus Status { get; set; } = ProductionBatchStatus.Pending;
    public DateTime? DeliveryDeadline { get; set; }    // UC 3.9.16
    public string? RejectionReason { get; set; }       // UC 3.9.18
    public DateTime? ProcessedAt { get; set; }         // UC 3.9.17
    public bool IsDeleted { get; set; }

    // Phase 4 — Delivery tracking
    public int DeliveredQuantity { get; set; }         // Running total of delivered qty
    public DateTime? DeliveryConfirmedAt { get; set; } // When school confirmed all deliveries
    public string? DeliveryNote { get; set; }          // General delivery note

    // Navigation properties
    public Campaign Campaign { get; set; } = null!;
    public Provider Provider { get; set; } = null!;
    public ICollection<ProductionBatchItem> Items { get; set; } = new List<ProductionBatchItem>();
    public ICollection<SupportTicket> Complaints { get; set; } = new List<SupportTicket>();
    public ICollection<DeliveryRecord> DeliveryRecords { get; set; } = new List<DeliveryRecord>();
    public ICollection<DistributionRecord> DistributionRecords { get; set; } = new List<DistributionRecord>();
}
