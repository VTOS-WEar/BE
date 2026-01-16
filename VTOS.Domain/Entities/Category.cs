using VTOS.Domain.Common;

namespace VTOS.Domain.Entities;

public class Category : AuditableEntity
{
    public string CategoryName { get; set; } = string.Empty;

    // Navigation properties
    public ICollection<OutfitCategory> OutfitCategories { get; set; } = new List<OutfitCategory>();
}

