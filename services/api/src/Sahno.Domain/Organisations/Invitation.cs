using System.Security.Cryptography;

namespace Sahno.Domain.Organisations;

public enum InvitationType
{
    /// <summary>Shareable link/code: multi-use until revoked or expired (D-077).</summary>
    Link = 1,

    /// <summary>
    /// Bound to an email address and single-use. Modelled now; issued only when
    /// email delivery arrives with the notifications slice.
    /// </summary>
    Email = 2,
}

/// <summary>
/// An invitation into an organisation (D-045). Invitations always join the
/// person as a Member; Admin access is a later Owner-only promotion.
/// </summary>
public sealed class Invitation
{
    /// <summary>
    /// Invite codes are read aloud and typed on phones, so they are short.
    /// 31^8 ≈ 8.5 × 10^11 codes (~39 bits): guessing one takes an account and
    /// is capped per account by the API's rate limit on code lookups.
    /// </summary>
    public const int TokenLength = 8;

    /// <summary>
    /// Upper-case letters and digits with the look-alikes removed (no I, L, O,
    /// 0, 1), so a code copied by eye cannot be misread.
    /// </summary>
    public const string TokenAlphabet = "ABCDEFGHJKMNPQRSTUVWXYZ23456789";

    private Invitation(
        Guid id,
        Guid organisationId,
        string token,
        InvitationType type,
        string? email,
        Guid createdByUserId,
        DateTimeOffset createdAtUtc,
        DateTimeOffset? expiresAtUtc,
        DateTimeOffset? revokedAtUtc,
        Guid? acceptedByUserId,
        DateTimeOffset? acceptedAtUtc)
    {
        Id = id;
        OrganisationId = organisationId;
        Token = token;
        Type = type;
        Email = email;
        CreatedByUserId = createdByUserId;
        CreatedAtUtc = createdAtUtc;
        ExpiresAtUtc = expiresAtUtc;
        RevokedAtUtc = revokedAtUtc;
        AcceptedByUserId = acceptedByUserId;
        AcceptedAtUtc = acceptedAtUtc;
    }

    public Guid Id { get; }

    public Guid OrganisationId { get; }

    /// <summary>The invite code: <see cref="TokenLength"/> characters of <see cref="TokenAlphabet"/>.</summary>
    public string Token { get; }

    public InvitationType Type { get; }

    public string? Email { get; }

    public Guid CreatedByUserId { get; }

    public DateTimeOffset CreatedAtUtc { get; }

    public DateTimeOffset? ExpiresAtUtc { get; }

    public DateTimeOffset? RevokedAtUtc { get; private set; }

    public Guid? AcceptedByUserId { get; private set; }

    public DateTimeOffset? AcceptedAtUtc { get; private set; }

    public bool IsUsable(DateTimeOffset nowUtc)
    {
        if (RevokedAtUtc is not null)
        {
            return false;
        }

        if (ExpiresAtUtc is not null && nowUtc >= ExpiresAtUtc)
        {
            return false;
        }

        // Email invitations are single-use; link invitations stay usable.
        if (Type == InvitationType.Email && AcceptedAtUtc is not null)
        {
            return false;
        }

        return true;
    }

    public void Revoke()
    {
        RevokedAtUtc ??= DateTimeOffset.UtcNow;
    }

    /// <summary>Records the single accepted use of an Email invitation.</summary>
    public void MarkAccepted(Guid userId)
    {
        if (Type != InvitationType.Email || AcceptedAtUtc is not null)
        {
            return;
        }

        AcceptedByUserId = userId;
        AcceptedAtUtc = DateTimeOffset.UtcNow;
    }

    public static Invitation CreateLink(
        Guid organisationId,
        Guid createdByUserId,
        DateTimeOffset? expiresAtUtc)
    {
        if (organisationId == Guid.Empty)
        {
            throw new ArgumentException(
                "An organisation is required.",
                nameof(organisationId));
        }

        return new Invitation(
            Guid.CreateVersion7(),
            organisationId,
            GenerateToken(),
            InvitationType.Link,
            email: null,
            createdByUserId,
            DateTimeOffset.UtcNow,
            expiresAtUtc,
            revokedAtUtc: null,
            acceptedByUserId: null,
            acceptedAtUtc: null);
    }

    /// <summary>
    /// The code as it is stored, from however it was typed: spaces and dashes
    /// dropped ("K7MP-9QAB", "k7mp 9qab") and, for a code of the current
    /// length, upper-cased. Anything longer is left alone, so a 26-character
    /// code issued before codes were shortened still matches exactly.
    /// </summary>
    public static string NormalizeToken(string typed)
    {
        var compact = new string(typed
            .Where(character => !char.IsWhiteSpace(character) && character != '-')
            .ToArray());

        return compact.Length == TokenLength
            ? compact.ToUpperInvariant()
            : compact;
    }

    private static string GenerateToken()
    {
        // GetString draws each character uniformly, whatever the alphabet's
        // size — no modulo bias from a 31-character alphabet.
        return RandomNumberGenerator.GetString(TokenAlphabet, TokenLength);
    }
}
