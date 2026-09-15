namespace Sahno.Contracts.Notifications;

/// <summary>
/// The app registering the phone it is on (D-080). <c>Platform</c> is "ios"
/// or "android"; <c>DeviceName</c> is whatever the OS calls the phone, for
/// an operator reading the table.
/// </summary>
public sealed record RegisterPushDeviceRequest(
    string Token,
    string Platform,
    string? DeviceName);

public sealed record PushDeviceResponse(
    string Token,
    string Platform,
    string? DeviceName,
    DateTimeOffset LastSeenAtUtc);

/// <summary>An organiser writing to everybody in the organisation.</summary>
public sealed record AnnouncementRequest(string Title, string? Body);
