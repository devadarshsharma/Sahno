using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging;
using Sahno.Application.Notifications;
using Sahno.Domain.Engagements;
using Sahno.Domain.Notifications;
using Sahno.Domain.Organisations;

namespace Sahno.Infrastructure.Persistence;

/// <summary>
/// Broadcasts "something changed" to an organisation's connected clients
/// AFTER a save commits.
///
/// Hooking the commit rather than the services is what makes this correct: a
/// client told to refetch before the transaction landed would refetch and see
/// nothing new, then never be told again. Hooking it here also means no
/// service has to remember to publish — anything that saves a row in an
/// organisation is live, including things nobody thought to wire.
///
/// A failed broadcast is logged and swallowed. The change is already saved;
/// the client will see it on its next focus, which is what it did before
/// SignalR existed.
/// </summary>
public sealed class LiveUpdateInterceptor(
    ILiveUpdates liveUpdates,
    ILogger<LiveUpdateInterceptor> logger) : SaveChangesInterceptor
{
    private readonly List<Guid> _organisations = [];
    private readonly List<Guid> _engagements = [];
    private readonly List<Notification> _notifications = [];

    public override InterceptionResult<int> SavingChanges(
        DbContextEventData eventData,
        InterceptionResult<int> result)
    {
        Collect(eventData.Context);
        return result;
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        Collect(eventData.Context);
        return ValueTask.FromResult(result);
    }

    public override async ValueTask<int> SavedChangesAsync(
        SaveChangesCompletedEventData eventData,
        int result,
        CancellationToken cancellationToken = default)
    {
        await PublishAsync(eventData.Context, cancellationToken);
        return result;
    }

    public override int SavedChanges(SaveChangesCompletedEventData eventData, int result)
    {
        PublishAsync(eventData.Context, CancellationToken.None).GetAwaiter().GetResult();
        return result;
    }

    /// <summary>
    /// Gathered before the save, while the tracker still knows what is new or
    /// modified. Deleted entries count too: an unassigned job removed is a
    /// readiness change somebody is looking at.
    /// </summary>
    private void Collect(DbContext? context)
    {
        _organisations.Clear();
        _engagements.Clear();
        _notifications.Clear();
        if (context is null)
        {
            return;
        }

        foreach (var entry in context.ChangeTracker.Entries())
        {
            if (entry.State is not (EntityState.Added or EntityState.Modified or EntityState.Deleted))
            {
                continue;
            }

            switch (entry.Entity)
            {
                case Notification notification:
                    _organisations.Add(notification.OrganisationId);
                    // A new row is also told to its recipient directly, payload
                    // and all, so a banner can go up without a refetch (D-080).
                    if (entry.State == EntityState.Added)
                    {
                        _notifications.Add(notification);
                    }

                    break;
                case Engagement engagement:
                    _organisations.Add(engagement.OrganisationId);
                    break;
                case Membership membership:
                    _organisations.Add(membership.OrganisationId);
                    break;
                case EngagementParticipant participant:
                    _engagements.Add(participant.EngagementId);
                    break;
                case EngagementActivity activity:
                    _engagements.Add(activity.EngagementId);
                    break;
                case ReadinessWaiver waiver:
                    _engagements.Add(waiver.EngagementId);
                    break;
                case Responsibility responsibility:
                    _engagements.Add(responsibility.EngagementId);
                    break;
                case Rehearsal rehearsal:
                    _engagements.Add(rehearsal.EngagementId);
                    break;
                case EngagementResource resource:
                    _engagements.Add(resource.EngagementId);
                    break;
                case DiscussionMessage message:
                    _engagements.Add(message.EngagementId);
                    break;
                case EngagementCustomer customer:
                    _engagements.Add(customer.EngagementId);
                    break;
                case EngagementFinance finance:
                    _engagements.Add(finance.EngagementId);
                    break;
                case PerformerPayment payment:
                    _engagements.Add(payment.EngagementId);
                    break;
            }
        }
    }

    private async Task PublishAsync(DbContext? context, CancellationToken cancellationToken)
    {
        if (context is null
            || (_organisations.Count == 0 && _engagements.Count == 0 && _notifications.Count == 0))
        {
            return;
        }

        foreach (var notification in _notifications)
        {
            try
            {
                await liveUpdates.NotificationCreatedAsync(
                    notification.RecipientUserId,
                    NotificationPayload.From(notification),
                    cancellationToken);
            }
            catch (Exception exception)
            {
                logger.LogWarning(
                    exception,
                    "Live notification {NotificationId} for user {UserId} was not sent",
                    notification.Id,
                    notification.RecipientUserId);
            }
        }

        var organisations = _organisations.ToHashSet();

        // Engagement-scoped rows know their engagement, not their organisation.
        // The engagement is almost always already tracked — the service loaded
        // it to authorise the change — so this is usually free.
        var unresolved = _engagements.Distinct().ToList();
        foreach (var engagement in context.ChangeTracker.Entries<Engagement>())
        {
            if (unresolved.Remove(engagement.Entity.Id))
            {
                organisations.Add(engagement.Entity.OrganisationId);
            }
        }

        if (unresolved.Count > 0)
        {
            var looked = await context.Set<Engagement>()
                .AsNoTracking()
                .Where(engagement => unresolved.Contains(engagement.Id))
                .Select(engagement => engagement.OrganisationId)
                .ToListAsync(cancellationToken);
            foreach (var organisationId in looked)
            {
                organisations.Add(organisationId);
            }
        }

        foreach (var organisationId in organisations)
        {
            try
            {
                await liveUpdates.OrganisationChangedAsync(organisationId, cancellationToken);
            }
            catch (Exception exception)
            {
                logger.LogWarning(
                    exception,
                    "Live update for organisation {OrganisationId} was not sent",
                    organisationId);
            }
        }
    }
}
