using Microsoft.EntityFrameworkCore;
using Sahno.Domain.Users;

namespace Sahno.Infrastructure.Persistence;

public sealed class SahnoDbContext(DbContextOptions<SahnoDbContext> options)
    : DbContext(options)
{
    public DbSet<User> Users => Set<User>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(
            typeof(SahnoDbContext).Assembly);
    }
}
