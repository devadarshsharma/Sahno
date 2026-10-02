namespace Sahno.Api.IntegrationTests;

/// <summary>
/// The time as tests want it to be. Unset, it is the real time, so nothing
/// that does not care is affected; a reminder test sets it to "8:05 tomorrow
/// in Sydney" and resets it when done.
/// </summary>
public sealed class TestClock : TimeProvider
{
    public DateTimeOffset? Now { get; set; }

    public override DateTimeOffset GetUtcNow() => (Now ?? DateTimeOffset.UtcNow).ToUniversalTime();
}
