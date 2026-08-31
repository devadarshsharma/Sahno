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
    public const int TokenLength = 26;

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

    /// <summary>URL-safe random token (~130 bits of entropy).</summary>
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

    private static string GenerateToken()
    {
        // Crockford-style alphabet without ambiguous characters; 26 chars of
        // a 32-character alphabet = 130 bits of entropy.
        const string alphabet = "abcdefghjkmnpqrstvwxyz23456789AB";
        Span<byte> bytes = stackalloc byte[TokenLength];
        RandomNumberGenerator.Fill(bytes);

        Span<char> chars = stackalloc char[TokenLength];
        for (var i = 0; i < TokenLength; i++)
        {
            chars[i] = alphabet[bytes[i] % alphabet.Length];
        }

        return new string(chars);
    }
}
