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

    public string? Email { get; private set; }

    public string? DisplayName { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; }

    /// <summary>
    /// Refreshes the optional profile hints from a newer login. Values are
    /// only ever improved — an absent claim never erases a stored hint.
    /// </summary>
    public bool RefreshProfileHints(string? email, string? displayName)
    {
        var changed = false;

        var normalizedEmail = NormalizeOptional(email);
        if (normalizedEmail is not null && normalizedEmail != Email)
        {
            Email = normalizedEmail;
            changed = true;
        }

        var normalizedName = NormalizeOptional(displayName);
        if (normalizedName is not null && normalizedName != DisplayName)
        {
            DisplayName = normalizedName;
            changed = true;
        }

        return changed;
    }

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
