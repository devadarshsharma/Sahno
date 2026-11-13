using Microsoft.EntityFrameworkCore;
using Sahno.Application.Users;
using Sahno.Domain.Organisations;
using Sahno.Domain.Users;
using Sahno.Infrastructure.Persistence;

namespace Sahno.Infrastructure.Users;

public sealed class UserAvatarStore(SahnoDbContext dbContext) : IUserAvatarStore
{
    public Task<UserAvatar?> FindAsync(Guid userId, CancellationToken cancellationToken) =>
        dbContext.UserAvatars.SingleOrDefaultAsync(avatar => avatar.UserId == userId, cancellationToken);

    public async Task AddAsync(UserAvatar avatar, CancellationToken cancellationToken)
    {
        dbContext.UserAvatars.Add(avatar);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public Task SaveAsync(CancellationToken cancellationToken) => dbContext.SaveChangesAsync(cancellationToken);

    public Task DeleteAsync(Guid userId, CancellationToken cancellationToken) =>
        dbContext.UserAvatars.Where(avatar => avatar.UserId == userId).ExecuteDeleteAsync(cancellationToken);

    public async Task<IReadOnlyList<(Guid UserId, DateTimeOffset UpdatedAtUtc)>> ListForOrganisationAsync(
        Guid organisationId,
        CancellationToken cancellationToken)
    {
        // Only the version, never the bytes: this is asked on every screen.
        var rows = await dbContext.UserAvatars
            .AsNoTracking()
            .Where(avatar => dbContext.Set<Membership>().Any(membership =>
                membership.OrganisationId == organisationId && membership.UserId == avatar.UserId))
            .Select(avatar => new { avatar.UserId, avatar.UpdatedAtUtc })
            .ToListAsync(cancellationToken);
        return rows.Select(row => (row.UserId, row.UpdatedAtUtc)).ToList();
    }

    public Task<bool> ShareAnOrganisationAsync(Guid viewerUserId, Guid subjectUserId, CancellationToken cancellationToken) =>
        dbContext.Set<Membership>()
            .Where(viewer => viewer.UserId == viewerUserId)
            .AnyAsync(viewer => dbContext.Set<Membership>().Any(subject =>
                subject.OrganisationId == viewer.OrganisationId && subject.UserId == subjectUserId), cancellationToken);
}
