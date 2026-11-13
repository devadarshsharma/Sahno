using Sahno.Domain.Users;

namespace Sahno.Application.Users;

public interface IUserAvatarStore
{
    Task<UserAvatar?> FindAsync(Guid userId, CancellationToken cancellationToken);

    Task AddAsync(UserAvatar avatar, CancellationToken cancellationToken);

    Task SaveAsync(CancellationToken cancellationToken);

    Task DeleteAsync(Guid userId, CancellationToken cancellationToken);

    /// <summary>When each member of the organisation last changed their photo; only those who have one.</summary>
    Task<IReadOnlyList<(Guid UserId, DateTimeOffset UpdatedAtUtc)>> ListForOrganisationAsync(
        Guid organisationId,
        CancellationToken cancellationToken);

    /// <summary>Whether the two people are in at least one organisation together.</summary>
    Task<bool> ShareAnOrganisationAsync(Guid viewerUserId, Guid subjectUserId, CancellationToken cancellationToken);
}

public enum AvatarResult
{
    Saved,
    NotAnImage,
    TooLarge,
}

/// <summary>
/// Profile photos (D-129). Your own to set and remove; seen by the people you
/// share an organisation with, and nobody else.
/// </summary>
public sealed class AvatarService(IUserAvatarStore avatars, TimeProvider clock)
{
    public async Task<(AvatarResult Result, DateTimeOffset? Version)> SetAsync(
        Guid userId,
        byte[] content,
        CancellationToken cancellationToken)
    {
        if (content.Length > UserAvatar.MaxBytes)
        {
            return (AvatarResult.TooLarge, null);
        }

        if (UserAvatar.SniffImageType(content) is null)
        {
            return (AvatarResult.NotAnImage, null);
        }

        var now = clock.GetUtcNow();
        var existing = await avatars.FindAsync(userId, cancellationToken);
        if (existing is null)
        {
            var avatar = UserAvatar.Create(userId, content, now);
            await avatars.AddAsync(avatar, cancellationToken);
            return (AvatarResult.Saved, avatar.UpdatedAtUtc);
        }

        existing.Replace(content, now);
        await avatars.SaveAsync(cancellationToken);
        return (AvatarResult.Saved, existing.UpdatedAtUtc);
    }

    public Task RemoveAsync(Guid userId, CancellationToken cancellationToken) =>
        avatars.DeleteAsync(userId, cancellationToken);

    /// <summary>The photo, if the viewer is that person or shares an organisation with them; otherwise nothing.</summary>
    public async Task<UserAvatar?> FindForViewerAsync(
        Guid viewerUserId,
        Guid subjectUserId,
        CancellationToken cancellationToken)
    {
        if (viewerUserId != subjectUserId
            && !await avatars.ShareAnOrganisationAsync(viewerUserId, subjectUserId, cancellationToken))
        {
            return null;
        }

        return await avatars.FindAsync(subjectUserId, cancellationToken);
    }

    public Task<IReadOnlyList<(Guid UserId, DateTimeOffset UpdatedAtUtc)>> ListForOrganisationAsync(
        Guid organisationId,
        CancellationToken cancellationToken) =>
        avatars.ListForOrganisationAsync(organisationId, cancellationToken);
}
