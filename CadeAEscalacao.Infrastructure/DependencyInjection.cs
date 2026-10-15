using CadeAEscalacao.Application.Abstractions;
using CadeAEscalacao.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CadeAEscalacao.Infrastructure;

public static class DependencyInjection
{
    /// <summary>
    /// Registers the database (Supabase Postgres via the session pooler, ADR-012) and the repositories.
    /// The connection string comes from <c>ConnectionStrings:Default</c> (User Secrets in development).
    /// </summary>
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Default")
            ?? throw new InvalidOperationException(
                "Connection string 'Default' not found. In development, set it in the Server's User Secrets (ADR-012).");

        services.AddDbContext<AppDbContext>(options => options
            .UseNpgsql(connectionString)
            .UseSnakeCaseNamingConvention());

        services.AddScoped<IUnitOfWork>(provider => provider.GetRequiredService<AppDbContext>());
        services.AddScoped<IPlayerRepository, PlayerRepository>();
        services.AddScoped<IGroupRepository, GroupRepository>();
        services.AddScoped<IWeeklyListRepository, WeeklyListRepository>();
        services.AddScoped<ILineupRepository, LineupRepository>();
        services.AddScoped<IMatchRepository, MatchRepository>();
        services.AddScoped<ISeasonRepository, SeasonRepository>();

        return services;
    }
}
