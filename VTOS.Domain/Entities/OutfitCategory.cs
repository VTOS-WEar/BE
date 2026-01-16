namespace VTOS.Domain.Entities;

public class OutfitCategory
{
    public Guid OutfitID { get; set; }
    public Guid CategoryID { get; set; }

    // Navigation properties
    public Outfit Outfit { get; set; } = null!;
    public Category Category { get; set; } = null!;
}

