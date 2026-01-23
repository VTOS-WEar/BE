using Microsoft.EntityFrameworkCore;
using VTOS.Domain.Entities;

namespace VTOS.Application.Abstractions;

/// <summary>
/// Abstraction for the database context.
/// Defined in Application layer to support dependency inversion.
/// </summary>
public interface IApplicationDbContext
{
    DbSet<User> Users { get; }
    DbSet<Role> Roles { get; }
    DbSet<EmailVerification> EmailVerifications { get; }
    DbSet<ChildProfile> ChildProfiles { get; }

    DbSet<T> Set<T>() where T : class;
    
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
