using VTOS.Domain.Common;
using VTOS.Domain.Enums;

namespace VTOS.Domain.Entities;

/// <summary>
/// Represents a user in the system.
/// Maps to the User table in the database.
/// </summary>
public class User : BaseEntity
{
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public DateTime? DOB { get; set; }
    public Gender Gender { get; set; } = Gender.Other;
    public string Avatar { get; set; } = string.Empty;
    public Guid RoleID { get; set; }
    public Guid? SchoolID { get; set; }
    public Guid? ProviderID { get; set; }
    public bool IsActive { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? LastLogin { get; set; }
    
    // Password Reset (stored as SHA-256 hash)
    public string? PasswordResetToken { get; set; }
    public DateTime? PasswordResetTokenExpiry { get; set; }

    // Navigation properties
    public Role Role { get; set; } = null!;
    public School? School { get; set; }
    public Provider? Provider { get; set; }
    public ICollection<ChildProfile> ChildProfiles { get; set; } = new List<ChildProfile>();
    public ICollection<TryOnHistory> TryOnHistories { get; set; } = new List<TryOnHistory>();
    public ICollection<OutfitRecommendation> OutfitRecommendations { get; set; } = new List<OutfitRecommendation>();
    public ICollection<Feedback> Feedbacks { get; set; } = new List<Feedback>();
    public ICollection<ParentBankAccount> BankAccounts { get; set; } = new List<ParentBankAccount>();
}
