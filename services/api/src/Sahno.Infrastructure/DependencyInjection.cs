using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Sahno.Application.Engagements;
using Sahno.Application.Organisations;
using Sahno.Application.Users;
using Sahno.Infrastructure.Engagements;
using Sahno.Infrastructure.Organisations;
using Sahno.Infrastructure.Persistence;
using Sahno.Infrastructure.Users;

namespace Sahno.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        string connectionString)
    {
        services.AddDbContext<SahnoDbContext>(options =>
            options.UseNpgsql(connectionString));

        services.AddScoped<IUserStore, UserStore>();
        services.AddScoped<IOrganisationStore, OrganisationStore>();
        services.AddScoped<IMembershipStore, MembershipStore>();
        services.AddScoped<IInvitationStore, InvitationStore>();
        services.AddScoped<IEngagementStore, EngagementStore>();

        return services;
    }
}
