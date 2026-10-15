using CadeAEscalacao.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CadeAEscalacao.Infrastructure;

public static class DependencyInjection
{
    /// <summary>
    /// Registers the database (Supabase Postgres via the session pooler, ADR-012).
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

        return services;
    }
}
