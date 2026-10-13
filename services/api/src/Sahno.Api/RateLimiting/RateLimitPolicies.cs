using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;

namespace Sahno.Api.RateLimiting;

public static class RateLimitPolicies
{
    /// <summary>
    /// Looking up or accepting an invite code. Codes are eight characters so
    /// people can type them, which makes guessing the thing to stop: each
    /// account gets <see cref="InviteCodeAttemptsPerMinute"/> attempts a
    /// minute — plenty for a typo or two, and about 43,000 a day, against
    /// roughly 8.5 × 10^11 possible codes.
    /// </summary>
    public const string InviteCode = "invite-code";

    public const int InviteCodeAttemptsPerMinute = 30;

    /// <summary>
    /// Venue suggestions (D-102). Each keystroke pause is a request, and each
    /// is billed by Google, so one account gets <see cref="PlaceSuggestionsPerMinute"/>
    /// a minute: far more than anyone typing, far less than a script.
    /// </summary>
    public const string PlaceSuggestions = "place-suggestions";

    public const int PlaceSuggestionsPerMinute = 60;

    public static IServiceCollection AddSahnoRateLimiting(this IServiceCollection services)
    {
        return services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

            options.AddPolicy(InviteCode, context =>
                RateLimitPartition.GetFixedWindowLimiter(
                    // Both endpoints require sign-in, so the account is the
                    // partition; the address is only a fallback.
                    context.User.FindFirst("sub")?.Value
                        ?? context.Connection.RemoteIpAddress?.ToString()
                        ?? "unknown",
                    _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = InviteCodeAttemptsPerMinute,
                        Window = TimeSpan.FromMinutes(1),
                        QueueLimit = 0,
                    }));

            options.AddPolicy(PlaceSuggestions, context =>
                RateLimitPartition.GetFixedWindowLimiter(
                    context.User.FindFirst("sub")?.Value
                        ?? context.Connection.RemoteIpAddress?.ToString()
                        ?? "unknown",
                    _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = PlaceSuggestionsPerMinute,
                        Window = TimeSpan.FromMinutes(1),
                        QueueLimit = 0,
                    }));
        });
    }
}
