using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Sahno.Application.Engagements;
using Sahno.Application.Notifications;
using Sahno.Application.Organisations;
using Sahno.Application.Repertoire;
using Sahno.Application.Users;
using Sahno.Infrastructure.Engagements;
using Sahno.Infrastructure.Notifications;
using Sahno.Infrastructure.Organisations;
using Sahno.Infrastructure.Repertoire;
using Sahno.Infrastructure.Persistence;
using Sahno.Infrastructure.Users;

namespace Sahno.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        string connectionString)
    {
        // The interceptor broadcasts after each commit, so anything that saves
        // a row in an organisation is live without the service knowing.
        services.AddScoped<LiveUpdateInterceptor>();
        services.AddDbContext<SahnoDbContext>((provider, options) =>
            options
                .UseNpgsql(connectionString)
                .AddInterceptors(provider.GetRequiredService<LiveUpdateInterceptor>()));

        services.AddScoped<IUserStore, UserStore>();
        services.AddScoped<IOrganisationStore, OrganisationStore>();
        services.AddScoped<IMembershipStore, MembershipStore>();
        services.AddScoped<IInvitationStore, InvitationStore>();
        services.AddScoped<IEngagementStore, EngagementStore>();
        services.AddScoped<IEngagementParticipantStore, EngagementParticipantStore>();
        services.AddScoped<IReadinessStore, ReadinessStore>();
        services.AddScoped<IResponsibilityStore, ResponsibilityStore>();
        services.AddScoped<IRehearsalStore, RehearsalStore>();
        services.AddScoped<IEngagementResourceStore, EngagementResourceStore>();
        services.AddScoped<IDiscussionStore, DiscussionStore>();
        services.AddScoped<ICommercialStore, CommercialStore>();
        services.AddScoped<ICustomerStore, CustomerStore>();
        services.AddScoped<IPieceStore, PieceStore>();
        services.AddScoped<ISetListStore, SetListStore>();
        services.AddScoped<INotificationStore, NotificationStore>();
        services.AddScoped<IOutboxStore, OutboxStore>();
        services.AddScoped<IPushDeviceStore, PushDeviceStore>();

        // Email goes through Resend when a key is configured and to the log
        // otherwise, so the whole outbox path runs on every developer machine
        // without anyone receiving a stray test email.
        services.AddOptions<EmailOptions>().BindConfiguration(EmailOptions.SectionName);
        services.AddHttpClient<ResendEmailSender>(client =>
        {
            client.BaseAddress = new Uri("https://api.resend.com/");
        })
        .ConfigureHttpClient((provider, client) =>
        {
            var key = provider.GetRequiredService<IOptions<EmailOptions>>().Value.ResendApiKey;
            if (!string.IsNullOrWhiteSpace(key))
            {
                client.DefaultRequestHeaders.Authorization =
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", key);
            }
        });
        services.AddScoped<IEmailSender>(provider =>
        {
            var options = provider.GetRequiredService<IOptions<EmailOptions>>().Value;
            return string.IsNullOrWhiteSpace(options.ResendApiKey)
                ? provider.GetRequiredService<LoggingEmailSender>()
                : provider.GetRequiredService<ResendEmailSender>();
        });
        services.AddScoped<LoggingEmailSender>();

        // Push goes through Expo's push service unless switched off, in which
        // case it is logged like a keyless email. The Expo endpoint needs no
        // credentials of its own; the FCM and APNs keys live on the EAS project.
        services.AddOptions<PushOptions>().BindConfiguration(PushOptions.SectionName);
        services.AddHttpClient<ExpoPushSender>(client =>
        {
            client.BaseAddress = new Uri("https://exp.host/--/api/v2/");
            client.DefaultRequestHeaders.Accept.Add(
                new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));
        });
        services.AddScoped<IPushSender>(provider =>
        {
            var options = provider.GetRequiredService<IOptions<PushOptions>>().Value;
            return options.Enabled
                ? provider.GetRequiredService<ExpoPushSender>()
                : provider.GetRequiredService<LoggingPushSender>();
        });
        services.AddScoped<LoggingPushSender>();
        services.AddScoped<OutboxDispatcher>();
        services.AddHostedService<OutboxWorker>();

        return services;
    }
}
