using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Sahno.Application.Notifications;

namespace Sahno.Infrastructure.Notifications;

public sealed class PushOptions
{
    public const string SectionName = "Push";

    /// <summary>
    /// Off, and pushes are logged instead of sent. On by default: Expo's
    /// push endpoint needs no credentials to call, so a developer machine can
    /// push to a real phone that has registered against it.
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Optional Expo access token ("enhanced push security"). When set, Expo
    /// refuses pushes to this project's tokens from anyone without it.
    /// </summary>
    public string? ExpoAccessToken { get; set; }
}

/// <summary>
/// Development delivery: the push is written to the log and nowhere else.
/// </summary>
public sealed class LoggingPushSender(ILogger<LoggingPushSender> logger) : IPushSender
{
    public Task<PushSendResult> SendAsync(
        string pushToken,
        string title,
        string body,
        string? dataJson,
        CancellationToken cancellationToken)
    {
        logger.LogInformation(
            "Push (not sent, push disabled)\nTo: {Token}\nTitle: {Title}\n{Body}\n{Data}",
            pushToken,
            title,
            body,
            dataJson);

        return Task.FromResult(new PushSendResult(PushSendOutcome.Sent));
    }
}

/// <summary>
/// Expo's push service (TECHNICAL_ARCHITECTURE: Expo push as a delivery
/// adapter). Expo forwards to FCM on Android and APNs on iOS with the
/// credentials held on the EAS project, so this process holds none of them.
///
/// One message per call; the dispatcher owns retries. The ticket in the
/// response says whether Expo accepted it and, for a dead token, says so
/// immediately as <c>DeviceNotRegistered</c>. Receipts (what FCM/APNs then
/// said) are not polled yet; a token that dies later is found the next time
/// a push to it is ticketed.
/// </summary>
public sealed class ExpoPushSender(
    HttpClient httpClient,
    IOptions<PushOptions> options) : IPushSender
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    public async Task<PushSendResult> SendAsync(
        string pushToken,
        string title,
        string body,
        string? dataJson,
        CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, "push/send")
        {
            Content = JsonContent.Create(new ExpoPushMessage(
                pushToken,
                title,
                body,
                dataJson is null ? null : JsonSerializer.Deserialize<JsonElement>(dataJson),
                "default",
                "high",
                "default")),
        };

        if (!string.IsNullOrWhiteSpace(options.Value.ExpoAccessToken))
        {
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(
                "Bearer",
                options.Value.ExpoAccessToken);
        }

        using var response = await httpClient.SendAsync(request, cancellationToken);
        var text = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            return new PushSendResult(
                PushSendOutcome.Failed,
                $"Expo push returned {(int)response.StatusCode}: {Clip(text)}");
        }

        ExpoPushResponse? parsed;
        try
        {
            parsed = JsonSerializer.Deserialize<ExpoPushResponse>(text, Json);
        }
        catch (JsonException)
        {
            return new PushSendResult(PushSendOutcome.Failed, $"Unreadable Expo response: {Clip(text)}");
        }

        var ticket = parsed?.Data?.FirstOrDefault();
        if (ticket is null)
        {
            var error = parsed?.Errors?.FirstOrDefault();
            return new PushSendResult(
                PushSendOutcome.Failed,
                error is null ? $"No ticket in Expo response: {Clip(text)}" : $"{error.Code}: {error.Message}");
        }

        if (string.Equals(ticket.Status, "ok", StringComparison.OrdinalIgnoreCase))
        {
            return new PushSendResult(PushSendOutcome.Sent);
        }

        var reason = ticket.Details?.Error ?? ticket.Message ?? "error";
        return string.Equals(reason, "DeviceNotRegistered", StringComparison.OrdinalIgnoreCase)
            ? new PushSendResult(PushSendOutcome.DeviceNotRegistered, reason)
            : new PushSendResult(PushSendOutcome.Failed, $"{reason}: {ticket.Message}");
    }

    private static string Clip(string text) => text.Length > 300 ? text[..300] : text;

    private sealed record ExpoPushMessage(
        [property: JsonPropertyName("to")] string To,
        [property: JsonPropertyName("title")] string Title,
        [property: JsonPropertyName("body")] string Body,
        [property: JsonPropertyName("data")] JsonElement? Data,
        [property: JsonPropertyName("sound")] string Sound,
        [property: JsonPropertyName("priority")] string Priority,
        [property: JsonPropertyName("channelId")] string ChannelId);

    private sealed record ExpoPushResponse(
        [property: JsonPropertyName("data")] List<ExpoTicket>? Data,
        [property: JsonPropertyName("errors")] List<ExpoError>? Errors);

    private sealed record ExpoTicket(
        [property: JsonPropertyName("status")] string? Status,
        [property: JsonPropertyName("id")] string? Id,
        [property: JsonPropertyName("message")] string? Message,
        [property: JsonPropertyName("details")] ExpoTicketDetails? Details);

    private sealed record ExpoTicketDetails(
        [property: JsonPropertyName("error")] string? Error);

    private sealed record ExpoError(
        [property: JsonPropertyName("code")] string? Code,
        [property: JsonPropertyName("message")] string? Message);
}
