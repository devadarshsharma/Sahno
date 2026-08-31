namespace Sahno.Domain.Users;

/// <summary>
/// A Sahno account. <see cref="Id"/> is Sahno's own stable identifier;
/// <see cref="ExternalSubject"/> is an opaque reference to the canonical
/// identity asserted by the external identity provider. The identity provider
/// collapses linked login methods into one canonical subject, so a single
/// subject per user is sufficient (documented assumption; explicit account
/// linking and duplicate-user merging are a later slice). Email and display
/// name are optional profile hints — never identity keys.
/// The subject also retains the sign-in method: Auth0 subjects are prefixed
/// with the connection ("google-oauth2|…", "apple|…", "email|…"), which is
/// enough to later show "Signed in with Google/Apple/Email" in account
/// settings without an additional column.
/// </summary>
public sealed class User
{
    private User(
        Guid id,
        string externalSubject,
        string? email,
        string? displayName,
        DateTimeOffset createdAtUtc)
    {
        Id = id;
        ExternalSubject = externalSubject;
        Email = email;
        DisplayName = displayName;
        CreatedAtUtc = createdAtUtc;
    }

    public Guid Id { get; }

    public string ExternalSubject { get; }

    public string? Email { get; }

    public string? DisplayName { get; }

    public DateTimeOffset CreatedAtUtc { get; }

    public static User Create(
        string externalSubject,
        string? email,
        string? displayName)
    {
        if (string.IsNullOrWhiteSpace(externalSubject))
        {
            throw new ArgumentException(
                "An external subject is required.",
                nameof(externalSubject));
        }

        return new User(
            Guid.CreateVersion7(),
            externalSubject,
            NormalizeOptional(email),
            NormalizeOptional(displayName),
            DateTimeOffset.UtcNow);
    }

    private static string? NormalizeOptional(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}
