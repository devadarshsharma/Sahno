using Microsoft.EntityFrameworkCore;
using Sahno.Domain.Engagements;
using Sahno.Domain.Organisations;
using Sahno.Domain.Users;

namespace Sahno.Infrastructure.Persistence;

public sealed class SahnoDbContext(DbContextOptions<SahnoDbContext> options)
    : DbContext(options)
{
    public DbSet<User> Users => Set<User>();

    public DbSet<Organisation> Organisations => Set<Organisation>();

    public DbSet<Membership> Memberships => Set<Membership>();

    public DbSet<Invitation> Invitations => Set<Invitation>();

    public DbSet<Engagement> Engagements => Set<Engagement>();

    public DbSet<EngagementActivity> EngagementActivities =>
        Set<EngagementActivity>();

    public DbSet<EngagementParticipant> EngagementParticipants =>
        Set<EngagementParticipant>();

    public DbSet<ReadinessWaiver> ReadinessWaivers => Set<ReadinessWaiver>();

    public DbSet<Responsibility> Responsibilities => Set<Responsibility>();

    public DbSet<Rehearsal> Rehearsals => Set<Rehearsal>();

    public DbSet<EngagementResource> EngagementResources =>
        Set<EngagementResource>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(
            typeof(SahnoDbContext).Assembly);
    }
}
