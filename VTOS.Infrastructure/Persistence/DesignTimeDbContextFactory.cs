using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace VTOS.Infrastructure.Persistence;

/// <summary>
/// Design-time factory for EF Core CLI (dotnet ef migrations add).
/// Uses PostgreSQL provider so migrations generate PostgreSQL-compatible types (uuid, varchar, timestamp).
/// This does NOT need a running PostgreSQL server — EF only uses the provider for type mapping.
/// </summary>
public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<VTOSDbContext>
{
    public VTOSDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<VTOSDbContext>();
        
        // Dummy connection string — EF doesn't connect during "migrations add"
        optionsBuilder.UseNpgsql(
            "Host=localhost;Database=dummy;Username=dummy;Password=dummy",
            b => b.MigrationsAssembly(typeof(VTOSDbContext).Assembly.FullName));

        return new VTOSDbContext(optionsBuilder.Options);
    }
}
