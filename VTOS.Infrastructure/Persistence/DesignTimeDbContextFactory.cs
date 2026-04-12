using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace VTOS.Infrastructure.Persistence;

/// <summary>
/// Design-time factory for EF Core CLI (dotnet ef migrations add / database update).
/// Reads connection string from appsettings.json in the startup project directory.
/// Falls back to a dummy PostgreSQL connection for "migrations add" (no DB needed).
/// </summary>
public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<VTOSDbContext>
{
    public VTOSDbContext CreateDbContext(string[] args)
    {
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: true)
            .AddJsonFile("appsettings.Development.json", optional: true)
            .AddEnvironmentVariables()
            .Build();

        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? "Host=localhost;Database=dummy;Username=dummy;Password=dummy";

        var dbProvider = configuration.GetValue<string>("DatabaseProvider") ?? "PostgreSQL";

        var optionsBuilder = new DbContextOptionsBuilder<VTOSDbContext>();

        if (dbProvider.Equals("PostgreSQL", StringComparison.OrdinalIgnoreCase))
        {
            optionsBuilder.UseNpgsql(connectionString,
                b => b.MigrationsAssembly(typeof(VTOSDbContext).Assembly.FullName));
        }
        else
        {
            optionsBuilder.UseSqlServer(connectionString,
                b => b.MigrationsAssembly(typeof(VTOSDbContext).Assembly.FullName));
        }

        return new VTOSDbContext(optionsBuilder.Options);
    }
}
