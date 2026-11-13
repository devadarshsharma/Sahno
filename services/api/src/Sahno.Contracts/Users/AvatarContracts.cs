namespace Sahno.Contracts.Users;

/// <summary>
/// Somebody's photo and its version (D-129). The app fetches the photo at
/// <c>/api/users/{UserId}/avatar?v={Version}</c>; a new version is a new address.
/// </summary>
public sealed record AvatarVersionResponse(Guid UserId, long Version);
