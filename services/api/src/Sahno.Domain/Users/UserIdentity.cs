namespace Sahno.Domain.Users;

/// <summary>
/// One way of signing in to a Sahno account (D-131): the identity provider's
/// subject ("google-oauth2|…", "email|…", "apple|…") and the account it opens.
/// An account can have several — Google and an emailed code to the same
/// verified address both open the same account — and every subject opens
/// exactly one. <see cref="User.ExternalSubject"/> remains the first of them.
/// </summary>
public sealed class UserIdentity
{
    private UserIdentity(string subject, Guid userId, DateTimeOffset linkedAtUtc)
    {
        Subject = subject;
        UserId = userId;
        LinkedAtUtc = linkedAtUtc;
    }

    public string Subject { get; }

    public Guid UserId { get; }

    public DateTimeOffset LinkedAtUtc { get; }

    public static UserIdentity Create(string subject, Guid userId)
    {
        if (string.IsNullOrWhiteSpace(subject))
        {
            throw new ArgumentException("A subject is required.", nameof(subject));
        }

        return new UserIdentity(subject, userId, DateTimeOffset.UtcNow);
    }
}
