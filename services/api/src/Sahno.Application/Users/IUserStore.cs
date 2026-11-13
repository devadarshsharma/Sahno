using Sahno.Domain.Users;

namespace Sahno.Application.Users;

/// <summary>
/// Focused persistence port for <see cref="User"/> records.
/// </summary>
public interface IUserStore
{
    /// <summary>The account this sign-in opens, if any (D-131).</summary>
    Task<User?> FindByExternalSubjectAsync(
        string externalSubject,
        CancellationToken cancellationToken);

    /// <summary>
    /// The oldest account holding this email address, compared without regard
    /// to case — the one a new sign-in with that verified address joins (D-131).
    /// </summary>
    Task<User?> FindByEmailAsync(string email, CancellationToken cancellationToken);

    /// <summary>
    /// Persists a new user, with its first sign-in. Returns <c>false</c> when
    /// another request claimed the same external subject first (uniqueness is
    /// enforced by the database); the caller should re-read the existing record.
    /// </summary>
    Task<bool> AddAsync(User user, CancellationToken cancellationToken);

    /// <summary>
    /// Adds a sign-in to an existing account. Returns <c>false</c> when another
    /// request claimed the subject first; the caller re-reads.
    /// </summary>
    Task<bool> LinkAsync(Guid userId, string externalSubject, CancellationToken cancellationToken);

    /// <summary>Persists changes to a tracked user (profile-hint refresh).</summary>
    Task SaveAsync(User user, CancellationToken cancellationToken);
}
