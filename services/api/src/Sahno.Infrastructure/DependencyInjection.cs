using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Sahno.Application.Users;
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

        return services;
    }
}
