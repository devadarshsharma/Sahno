using Sahno.Application.Notifications;

namespace Sahno.Api.IntegrationTests;

/// <summary>
/// Records pushes instead of sending them, and can be told to answer
/// "DeviceNotRegistered" for a token so the dead-token path is testable.
/// </summary>
public sealed class RecordingPushSender : IPushSender
{
    private readonly List<(string Token, string Title, string Body, string? Data)> _sent = [];
    private readonly HashSet<string> _dead = [];

    public IReadOnlyList<(string Token, string Title, string Body, string? Data)> Sent
    {
        get
        {
            lock (_sent)
            {
                return _sent.ToList();
            }
        }
    }

    public void MarkDead(string token)
    {
        lock (_dead)
        {
            _dead.Add(token);
        }
    }

    public Task<PushSendResult> SendAsync(
        string pushToken,
        string title,
        string body,
        string? dataJson,
        CancellationToken cancellationToken)
    {
        lock (_dead)
        {
            if (_dead.Contains(pushToken))
            {
                return Task.FromResult(
                    new PushSendResult(PushSendOutcome.DeviceNotRegistered, "DeviceNotRegistered"));
            }
        }

        lock (_sent)
        {
            _sent.Add((pushToken, title, body, dataJson));
        }

        return Task.FromResult(new PushSendResult(PushSendOutcome.Sent));
    }
}
