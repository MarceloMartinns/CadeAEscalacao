using CadeAEscalacao.Application.Abstractions;
using CadeAEscalacao.Infrastructure.Email;
using CadeAEscalacao.Infrastructure.Identity;
using CadeAEscalacao.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
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

        // Login (ADR-014): Identity tables in their own context, same database.
        services.AddDbContext<AuthDbContext>(options => options
            .UseNpgsql(connectionString, npgsql => npgsql.MigrationsHistoryTable(AuthDbContext.MigrationsHistoryTable))
            .UseSnakeCaseNamingConvention());

        services.AddIdentityCore<AppUser>(options =>
            {
                // The user name is the e-mail, already validated by the API.
                options.User.AllowedUserNameCharacters = string.Empty;
                options.User.RequireUniqueEmail = true;
                options.SignIn.RequireConfirmedEmail = true;
                options.Lockout.AllowedForNewUsers = true;
                options.Lockout.MaxFailedAccessAttempts = 5;
                options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
            })
            .AddEntityFrameworkStores<AuthDbContext>()
            // Only the 6-digit e-mail code is used (ADR-014).
            .AddTokenProvider<EmailTokenProvider<AppUser>>(TokenOptions.DefaultEmailProvider);

        // Development only: writes the e-mail to the console. A real provider comes in etapa 10.
        services.AddSingleton<IEmailSender, ConsoleEmailSender>();

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
