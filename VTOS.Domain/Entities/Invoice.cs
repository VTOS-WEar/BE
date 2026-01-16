using VTOS.Domain.Common;

namespace VTOS.Domain.Entities;

public class Invoice : AuditableEntity
{
    public Guid OrderID { get; set; }
    public DateTime IssueDate { get; set; }
    public string? InvoiceDataURL { get; set; }

    // Navigation properties
    public Order Order { get; set; } = null!;
}

