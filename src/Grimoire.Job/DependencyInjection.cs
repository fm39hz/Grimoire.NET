using EntityFramework.Exceptions.PostgreSQL;
using Grimoire.Application;
using Grimoire.Infrastructure;
using Grimoire.Infrastructure.Configuration;
using Grimoire.Infrastructure.Persistence.Database;
using Grimoire.Job.Jobs;
using Hangfire;
using Hangfire.PostgreSql;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Grimoire.Job;

public static class DependencyInjection
{
    public static IServiceCollection AddJobInfrastructure(
        this IServiceCollection services, IConfiguration configuration)
    {
        // Register Application + Infrastructure services
        services.AddApplication();
        services.AddInfrastructure(configuration);

        // EF Core DbContext (required by repositories registered in AddInfrastructure)
        var connectionString = configuration.GetConnectionString("Postgre")
            ?? throw new InvalidOperationException("ConnectionStrings:Postgre is required");

        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseNpgsql(connectionString)
                .UseSnakeCaseNamingConvention()
                .UseExceptionProcessor()
                .ConfigureWarnings(w => w.Ignore(CoreEventId.AccidentalEntityType)));

        // Hangfire client — server runs only in API (single process)
        // Re-enable AddHangfireServer here for distributed deployment
        services.AddHangfire(config => config
            .UsePostgreSqlStorage(options => options
                .UseNpgsqlConnection(connectionString)));

        // Register jobs so they can be resolved when this project runs as worker
        services.AddTransient<ExportJob>();

        return services;
    }
}
