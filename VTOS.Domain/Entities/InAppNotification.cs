using VTOS.Domain.Common;

namespace VTOS.Domain.Entities;

/// <summary>
/// In-app notification for School/Provider users.
/// Created by system actions (contract, production order, delivery, etc.)
/// </summary>
public class InAppNotification : BaseEntity
{
    public Guid UserId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    
    /// <summary>
    /// ContractAction, ProductionAction, DeliveryAction, OrderAction, System
    /// </summary>
    public string Type { get; set; } = "System";
    
    public bool IsRead { get; set; } = false;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    /// <summary>
    /// Optional reference to related entity (Contract, ProductionBatch, Order, etc.)
    /// </summary>
    public Guid? RelatedEntityId { get; set; }
    public string? RelatedEntityType { get; set; }
    
    /// <summary>
    /// Optional link for frontend navigation (e.g. "/school/contracts/123")
    /// </summary>
    public string? ActionUrl { get; set; }

    // Navigation
    public virtual User User { get; set; } = null!;
}
