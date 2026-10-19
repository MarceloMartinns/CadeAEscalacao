using CadeAEscalacao.Application.Abstractions;
using CadeAEscalacao.Infrastructure.Identity;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace CadeAEscalacao.Server.Tests;

/// <summary>
/// Runs the real API in memory (endpoints, validation, authentication, use cases and error handling)
/// with the repositories and the unit of work replaced by NSubstitute mocks. The login tables
/// (ADR-014) use the EF Core in-memory provider and the e-mails stay in <see cref="Emails"/>: no database.
/// </summary>
public sealed class ApiFactory : WebApplicationFactory<Program>
{
    private readonly string _authDatabaseName = $"auth-{Guid.NewGuid()}";

    public IPlayerRepository Players { get; } = Substitute.For<IPlayerRepository>();

    public IGroupRepository Groups { get; } = Substitute.For<IGroupRepository>();

    public IWeeklyListRepository Lists { get; } = Substitute.For<IWeeklyListRepository>();

    public ILineupRepository Lineups { get; } = Substitute.For<ILineupRepository>();

    public IMatchRepository Matches { get; } = Substitute.For<IMatchRepository>();

    public ISeasonRepository Seasons { get; } = Substitute.For<ISeasonRepository>();

    public IUnitOfWork UnitOfWork { get; } = Substitute.For<IUnitOfWork>();

    public FakeEmailSender Emails { get; } = new();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // AddInfrastructure requires a connection string; it is never used because every
        // service that touches the database is replaced below.
        builder.UseSetting("ConnectionStrings:Default", "Host=unused");
        builder.UseSetting("Jwt:Issuer", TestTokens.Issuer);
        builder.UseSetting("Jwt:Audience", TestTokens.Audience);
        builder.UseSetting("Jwt:SigningKey", TestTokens.SigningKey);

        builder.ConfigureTestServices(services =>
        {
            Replace(services, Players);
            Replace(services, Groups);
            Replace(services, Lists);
            Replace(services, Lineups);
            Replace(services, Matches);
            Replace(services, Seasons);
            Replace(services, UnitOfWork);
            Replace<IEmailSender>(services, Emails);

            services.RemoveAll<DbContextOptions<AuthDbContext>>();
            services.RemoveAll<IDbContextOptionsConfiguration<AuthDbContext>>();
            services.AddDbContext<AuthDbContext>(options => options.UseInMemoryDatabase(_authDatabaseName));
        });
    }

    /// <summary>Runs code with the API's services, e.g. to read the login tables.</summary>
    public async Task<T> WithAuthDbAsync<T>(Func<AuthDbContext, Task<T>> action)
    {
        using var scope = Services.CreateScope();
        return await action(scope.ServiceProvider.GetRequiredService<AuthDbContext>());
    }

    private static void Replace<TService>(IServiceCollection services, TService mock)
        where TService : class
    {
        services.RemoveAll<TService>();
        services.AddSingleton(mock);
    }
}
