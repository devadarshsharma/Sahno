using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Sahno.Infrastructure.Persistence;

/// <summary>
/// Design-time factory for the EF Core CLI (migrations). Uses the local
/// development connection string from docs/LOCAL_DEVELOPMENT.md unless
/// ConnectionStrings__Sahno is set. Never used at runtime.
/// </summary>
public sealed class SahnoDbContextFactory
    : IDesignTimeDbContextFactory<SahnoDbContext>
{
    public SahnoDbContext CreateDbContext(string[] args)
    {
        var connectionString =
            Environment.GetEnvironmentVariable("ConnectionStrings__Sahno")
            ?? "Host=localhost;Port=5434;Database=sahno;Username=sahno;Password=sahno_local_password";

        var options = new DbContextOptionsBuilder<SahnoDbContext>()
            .UseNpgsql(connectionString)
            .Options;

        return new SahnoDbContext(options);
    }
}
