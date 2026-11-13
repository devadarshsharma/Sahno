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

    /// <summary>
    /// A signed-out phone asking not to be pushed to any more (D-133). No
    /// sign-in, so the address is the partition; the token itself is the
    /// proof, and is far too long to guess at this rate.
    /// </summary>
    public const string ForgetPushDevice = "forget-push-device";

    public const int ForgetPushDevicePerMinute = 60;

    /// <summary>
    /// A phone registering for notifications. The app does it once a sign-in
    /// and when the token rotates; builds before D-134 did it in a loop, many
    /// times a second, for as long as they were open. This turns those away
    /// before they reach the database.
    /// </summary>
    public const string RegisterPushDevice = "register-push-device";

    public const int RegisterPushDevicePerMinute = 6;

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

            options.AddPolicy(RegisterPushDevice, context =>
                RateLimitPartition.GetFixedWindowLimiter(
                    context.User.FindFirst("sub")?.Value
                        ?? context.Connection.RemoteIpAddress?.ToString()
                        ?? "unknown",
                    _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = RegisterPushDevicePerMinute,
                        Window = TimeSpan.FromMinutes(1),
                        QueueLimit = 0,
                    }));

            options.AddPolicy(ForgetPushDevice, context =>
                RateLimitPartition.GetFixedWindowLimiter(
                    context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                    _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = ForgetPushDevicePerMinute,
                        Window = TimeSpan.FromMinutes(1),
                        QueueLimit = 0,
                    }));
        });
    }
}
