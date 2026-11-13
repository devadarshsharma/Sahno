namespace Sahno.Domain.Users;

/// <summary>
/// A Sahno account. <see cref="Id"/> is Sahno's own stable identifier;
/// <see cref="ExternalSubject"/> is the identity provider's subject for the
/// first sign-in that created it. An account can be opened by several
/// sign-ins — each a <see cref="UserIdentity"/>, this first one included —
/// joined when a new method arrives with a verified email the account already
/// has (D-131). Email and display name are otherwise profile hints only.
/// The subject also retains the sign-in method: Auth0 subjects are prefixed
/// with the connection ("google-oauth2|…", "apple|…", "email|…"), which is
/// enough to later show "Signed in with Google/Apple/Email" in account
/// settings without an additional column.
/// </summary>
public sealed class User
{
    public const int PhoneNumberMaxLength = 40;

    private User(
        Guid id,
        string externalSubject,
        string? email,
        string? displayName,
        bool displayNameSetByUser,
        string? phoneNumber,
        DateTimeOffset createdAtUtc)
    {
        Id = id;
        ExternalSubject = externalSubject;
        Email = email;
        DisplayName = displayName;
        DisplayNameSetByUser = displayNameSetByUser;
        PhoneNumber = phoneNumber;
        CreatedAtUtc = createdAtUtc;
    }

    public Guid Id { get; }

    public string ExternalSubject { get; }

    public string? Email { get; private set; }

    public string? DisplayName { get; private set; }

    /// <summary>
    /// Whether <see cref="DisplayName"/> was chosen by the person rather than
    /// taken from an identity-provider claim. A chosen name always outranks a
    /// later provider hint (D-046).
    /// </summary>
    public bool DisplayNameSetByUser { get; private set; }

    /// <summary>
    /// Optional and private by default (D-046): an ordinary Member of an
    /// organisation only sees it once this person chooses to share their
    /// contact details with that organisation (D-018).
    /// </summary>
    public string? PhoneNumber { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; }

    /// <summary>
    /// The display name that can actually be shown to people. The passwordless
    /// email connection sets the provider's "name" claim to the email address
    /// itself, which is not a name — and showing it would leak an address that
    /// D-018 keeps private from other Members by default. Such a hint is
    /// reported as absent so the person is asked for a real name instead.
    /// </summary>
    public string? PresentableDisplayName =>
        DisplayNameSetByUser || !EchoesEmail(DisplayName) ? DisplayName : null;

    /// <summary>
    /// Refreshes the optional profile hints from a newer login. Values are
    /// only ever improved — an absent claim never erases a stored hint, and a
    /// provider hint never overwrites a name the person chose themselves.
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

        if (DisplayNameSetByUser)
        {
            return changed;
        }

        var normalizedName = NormalizeOptional(displayName);
        // An emailed-code sign-in's "name" is the address itself; on an
        // account that also signs in with Google it must not replace the real
        // name Google gave (D-131).
        var onlyAnAddress = normalizedName is not null
            && (EchoesEmail(normalizedName) || normalizedName.Contains('@'));
        if (normalizedName is not null && normalizedName != DisplayName
            && !(onlyAnAddress && DisplayName is not null && !EchoesEmail(DisplayName)))
        {
            DisplayName = normalizedName;
            changed = true;
        }

        return changed;
    }

    /// <summary>
    /// Records the display name the person chose. From this point provider
    /// hints no longer touch it.
    /// </summary>

    /// <summary>
    /// Sets or clears this person's own phone number. Pass null or blank to
    /// remove it; sharing it with an organisation is a separate, per-
    /// organisation choice (D-018).
    /// </summary>
    public void SetPhoneNumber(string? phoneNumber)
    {
        var normalized = NormalizeOptional(phoneNumber);
        PhoneNumber = normalized is { Length: > PhoneNumberMaxLength }
            ? normalized[..PhoneNumberMaxLength]
            : normalized;
    }
    public void SetDisplayName(string displayName)
    {
        var normalized = NormalizeOptional(displayName);
        if (normalized is null)
        {
            throw new ArgumentException(
                "A display name is required.",
                nameof(displayName));
        }

        DisplayName = normalized;
        DisplayNameSetByUser = true;
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
            displayNameSetByUser: false,
            phoneNumber: null,
            DateTimeOffset.UtcNow);
    }

    private bool EchoesEmail(string? displayName)
    {
        return displayName is not null
            && Email is not null
            && string.Equals(displayName, Email, StringComparison.OrdinalIgnoreCase);
    }

    private static string? NormalizeOptional(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}
