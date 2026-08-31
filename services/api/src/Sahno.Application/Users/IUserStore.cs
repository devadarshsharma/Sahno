using Sahno.Domain.Users;

namespace Sahno.Application.Users;

/// <summary>
/// Focused persistence port for <see cref="User"/> records.
/// </summary>
public interface IUserStore
{
    Task<User?> FindByExternalSubjectAsync(
        string externalSubject,
        CancellationToken cancellationToken);

    /// <summary>
    /// Persists a new user. Returns <c>false</c> when another request created
    /// a user with the same external subject first (uniqueness is enforced by
    /// the database); the caller should re-read the existing record.
    /// </summary>
    Task<bool> AddAsync(User user, CancellationToken cancellationToken);
}
