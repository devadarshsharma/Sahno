using Microsoft.EntityFrameworkCore;
using Sahno.Application.Notifications;
using Sahno.Domain.Notifications;
using Sahno.Infrastructure.Persistence;

namespace Sahno.Infrastructure.Notifications;

public sealed class PushDeviceStore(SahnoDbContext dbContext) : IPushDeviceStore
{
    public Task<PushDevice?> FindByTokenAsync(string token, CancellationToken cancellationToken)
    {
        return dbContext.PushDevices.FirstOrDefaultAsync(
            device => device.Token == token,
            cancellationToken);
    }

    public async Task<IReadOnlyList<PushDevice>> ListActiveForUserAsync(
        Guid userId,
        CancellationToken cancellationToken)
    {
        return await dbContext.PushDevices
            .AsNoTracking()
            .Where(device => device.UserId == userId && device.DisabledAtUtc == null)
            .OrderByDescending(device => device.LastSeenAtUtc)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<PushDevice>> ListActiveForUsersAsync(
        IReadOnlyCollection<Guid> userIds,
        CancellationToken cancellationToken)
    {
        if (userIds.Count == 0)
        {
            return [];
        }

        return await dbContext.PushDevices
            .AsNoTracking()
            .Where(device => userIds.Contains(device.UserId) && device.DisabledAtUtc == null)
            .ToListAsync(cancellationToken);
    }

    public void Add(PushDevice device) => dbContext.PushDevices.Add(device);

    public Task RemoveAsync(Guid deviceId, CancellationToken cancellationToken)
    {
        return dbContext.PushDevices
            .Where(device => device.Id == deviceId)
            .ExecuteDeleteAsync(cancellationToken);
    }

    public Task SaveAsync(CancellationToken cancellationToken)
    {
        return dbContext.SaveChangesAsync(cancellationToken);
    }
}
