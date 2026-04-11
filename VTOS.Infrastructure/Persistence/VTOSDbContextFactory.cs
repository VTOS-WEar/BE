using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace VTOS.Infrastructure.Persistence;

/// <summary>
/// Design-time factory used by EF Core CLI (add-migration, update-database).
/// Bypasses the full app host to avoid ASPNETCORE_ENVIRONMENT / system env var
/// pollution that caused Npgsql to be selected even on a local MSSQL machine.
///
/// Provider resolution order (first match wins):
///   1. EF_MIGRATION_PROVIDER env var  (explicit override — set in PMC before add-migration)
///   2. "DatabaseProvider" key in appsettings.Development.json
///   3. "DatabaseProvider" key in appsettings.json
///   4. Default → "SqlServer"
///
/// Intentionally does NOT call AddEnvironmentVariables() to prevent system-level
/// DatabaseProvider=PostgreSQL from leaking in during local MSSQL development.
/// </summary>
public class VTOSDbContextFactory : IDesignTimeDbContextFactory<VTOSDbContext>
{
    public VTOSDbContext CreateDbContext(string[] args)
    {
        var apiProjectPath = ResolveApiProjectPath();

        // Load config from appsettings files ONLY — no system env var pollution
        var config = new ConfigurationBuilder()
            .SetBasePath(apiProjectPath)
            .AddJsonFile("appsettings.json", optional: true)
            .AddJsonFile("appsettings.Development.json", optional: true)
            .Build();

        // Allow explicit override via EF_MIGRATION_PROVIDER env var
        // e.g. set EF_MIGRATION_PROVIDER=PostgreSQL in PMC before add-migration
        var provider = Environment.GetEnvironmentVariable("EF_MIGRATION_PROVIDER")
            ?? config.GetValue<string>("DatabaseProvider")
            ?? "SqlServer";

        var connStr = config.GetConnectionString("DefaultConnection");

        // Fallback: hardcoded local MSSQL if appsettings not found
        if (string.IsNullOrWhiteSpace(connStr))
        {
            connStr = "Server=DESKTOP-P5MIN4R\\SQLEXPRESS;Database=VTOSDb;" +
                      "User Id=sa;Password=123;TrustServerCertificate=True;" +
                      "MultipleActiveResultSets=true";
            Console.WriteLine("[VTOSDbContextFactory] WARNING: appsettings not found — " +
                              "using hardcoded fallback MSSQL connection string.");
        }

        var optionsBuilder = new DbContextOptionsBuilder<VTOSDbContext>();

        if (provider.Equals("PostgreSQL", StringComparison.OrdinalIgnoreCase))
        {
            Console.WriteLine("[VTOSDbContextFactory] Using PostgreSQL provider.");
            optionsBuilder.UseNpgsql(
                connStr,
                b => b.MigrationsAssembly(typeof(VTOSDbContext).Assembly.FullName));
        }
        else
        {
            Console.WriteLine("[VTOSDbContextFactory] Using SqlServer provider.");
            optionsBuilder.UseSqlServer(
                connStr,
                b => b.MigrationsAssembly(typeof(VTOSDbContext).Assembly.FullName));
        }

        return new VTOSDbContext(optionsBuilder.Options);
    }

    /// <summary>
    /// Resolves the VTOS.API project directory reliably across different
    /// PMC working-directory conventions (project dir, solution root, bin/debug).
    /// </summary>
    private static string ResolveApiProjectPath()
    {
        // Strategy 1: relative to assembly location (bin/Debug/net8.0 → ../../../ → project root)
        var assemblyDir = Path.GetDirectoryName(typeof(VTOSDbContextFactory).Assembly.Location) ?? "";
        // bin/Debug/net8.0 → go up 3 levels → VTOS.Infrastructure root → ../VTOS.API
        var fromAssembly = Path.GetFullPath(Path.Combine(assemblyDir, "../../../", "../VTOS.API"));
        if (Directory.Exists(fromAssembly) && File.Exists(Path.Combine(fromAssembly, "appsettings.json")))
            return fromAssembly;

        // Strategy 2: relative to current working directory
        var candidates = new[]
        {
            Path.Combine(Directory.GetCurrentDirectory(), "../VTOS.API"),
            Path.Combine(Directory.GetCurrentDirectory(), "VTOS.API"),
            Path.Combine(Directory.GetCurrentDirectory(), "../../VTOS.API"),
        };

        foreach (var candidate in candidates)
        {
            var full = Path.GetFullPath(candidate);
            if (Directory.Exists(full) && File.Exists(Path.Combine(full, "appsettings.json")))
                return full;
        }

        // Strategy 3: last resort — return current directory and let it fail gracefully
        Console.WriteLine("[VTOSDbContextFactory] WARNING: Could not locate VTOS.API directory. " +
                          "Using current directory as fallback.");
        return Directory.GetCurrentDirectory();
    }
}
