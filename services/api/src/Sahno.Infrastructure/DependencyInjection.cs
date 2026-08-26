using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Sahno.Infrastructure.Persistence;

namespace Sahno.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        string connectionString)
    {
        services.AddDbContext<SahnoDbContext>(options =>
            options.UseNpgsql(connectionString));

        return services;
    }
}
