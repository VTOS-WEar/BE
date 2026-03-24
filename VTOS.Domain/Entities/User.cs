using VTOS.Domain.Common;

namespace VTOS.Domain.Entities;

/// <summary>
/// Represents a user in the system (authentication + shared profile).
/// Role-specific data lives in ParentProfile, SchoolManager, or ProviderManager.
/// </summary>
public class User : BaseEntity
{
    // Shared profile (all roles)
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string Avatar { get; set; } = string.Empty;

    // Auth & system
    public Guid RoleID { get; set; }
    public bool IsActive { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? LastLogin { get; set; }

    // Password Reset (stored as SHA-256 hash)
    public string? PasswordResetToken { get; set; }
    public DateTime? PasswordResetTokenExpiry { get; set; }

    // External Auth (Google OAuth)
    public string? GoogleId { get; set; }
    public string AuthProvider { get; set; } = "Local";

    // Two-Factor Authentication (TOTP)
    public bool IsTwoFactorEnabled { get; set; }
    public string? TwoFactorSecret { get; set; }
    public string? RecoveryCodes { get; set; }

    // Navigation properties
    public Role Role { get; set; } = null!;

    // Role-specific profiles (1:0..1)
    public ParentProfile? ParentProfile { get; set; }
    public SchoolManager? SchoolManager { get; set; }
    public ProviderManager? ProviderManager { get; set; }

    // Parent-specific collections (kept for backward compat, accessed via ParentProfile)
    public ICollection<ChildProfile> ChildProfiles { get; set; } = new List<ChildProfile>();
    public ICollection<TryOnHistory> TryOnHistories { get; set; } = new List<TryOnHistory>();
    public ICollection<OutfitRecommendation> OutfitRecommendations { get; set; } = new List<OutfitRecommendation>();
    public ICollection<Feedback> Feedbacks { get; set; } = new List<Feedback>();
    public ICollection<ParentBankAccount> BankAccounts { get; set; } = new List<ParentBankAccount>();
}
