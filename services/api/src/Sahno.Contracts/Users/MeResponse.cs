namespace Sahno.Contracts.Users;

public sealed record MeResponse(
    Guid UserId,
    string? Email,
    string? DisplayName,
    DateTimeOffset CreatedAtUtc);
