using CadeAEscalacao.Application.Abstractions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace CadeAEscalacao.Server.Tests;

/// <summary>
/// Runs the real API in memory (endpoints, validation, use cases and error handling)
/// with the repositories and the unit of work replaced by NSubstitute mocks: no database.
/// </summary>
public sealed class ApiFactory : WebApplicationFactory<Program>
{
    public IPlayerRepository Players { get; } = Substitute.For<IPlayerRepository>();

    public IGroupRepository Groups { get; } = Substitute.For<IGroupRepository>();

    public IWeeklyListRepository Lists { get; } = Substitute.For<IWeeklyListRepository>();

    public ILineupRepository Lineups { get; } = Substitute.For<ILineupRepository>();

    public IMatchRepository Matches { get; } = Substitute.For<IMatchRepository>();

    public ISeasonRepository Seasons { get; } = Substitute.For<ISeasonRepository>();

    public IUnitOfWork UnitOfWork { get; } = Substitute.For<IUnitOfWork>();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // AddInfrastructure requires a connection string; it is never used because every
        // service that touches the database is replaced below.
        builder.UseSetting("ConnectionStrings:Default", "Host=unused");

        builder.ConfigureTestServices(services =>
        {
            Replace(services, Players);
            Replace(services, Groups);
            Replace(services, Lists);
            Replace(services, Lineups);
            Replace(services, Matches);
            Replace(services, Seasons);
            Replace(services, UnitOfWork);
        });
    }

    private static void Replace<TService>(IServiceCollection services, TService mock)
        where TService : class
    {
        services.RemoveAll<TService>();
        services.AddSingleton(mock);
    }
}
