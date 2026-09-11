using System.Net.Http.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Sahno.Application.Notifications;

namespace Sahno.Infrastructure.Notifications;

public sealed class EmailOptions
{
    public const string SectionName = "Email";

    /// <summary>Resend API key. Absent in development, where email is logged instead.</summary>
    public string? ResendApiKey { get; set; }

    /// <summary>The From header, e.g. "Sahno &lt;hello@sahno.app&gt;".</summary>
    public string From { get; set; } = "Sahno <onboarding@resend.dev>";
}

/// <summary>
/// Development delivery: the email is written to the log and nowhere else.
/// Keeps the whole outbox path — staging, dispatch, retries — exercised on
/// every developer machine without anyone receiving a stray test email.
/// </summary>
public sealed class LoggingEmailSender(ILogger<LoggingEmailSender> logger) : IEmailSender
{
    public Task SendAsync(
        string toEmail,
        string subject,
        string textBody,
        CancellationToken cancellationToken)
    {
        logger.LogInformation(
            "Email (not sent, no provider configured)\nTo: {To}\nSubject: {Subject}\n\n{Body}",
            toEmail,
            subject,
            textBody);

        return Task.CompletedTask;
    }
}

/// <summary>
/// Resend (TECHNICAL_ARCHITECTURE: accepted notification architecture). One
/// POST per message; the dispatcher owns retries, so a failure here is thrown
/// rather than swallowed.
/// </summary>
public sealed class ResendEmailSender(
    HttpClient httpClient,
    IOptions<EmailOptions> options) : IEmailSender
{
    public async Task SendAsync(
        string toEmail,
        string subject,
        string textBody,
        CancellationToken cancellationToken)
    {
        var response = await httpClient.PostAsJsonAsync(
            "emails",
            new
            {
                from = options.Value.From,
                to = new[] { toEmail },
                subject,
                text = textBody,
            },
            cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var detail = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new HttpRequestException(
                $"Resend returned {(int)response.StatusCode}: {detail}");
        }
    }
}
