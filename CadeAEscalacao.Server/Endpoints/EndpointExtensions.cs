using CadeAEscalacao.Server.Authentication;
using CadeAEscalacao.Server.Endpoints.Auth;
using CadeAEscalacao.Server.Endpoints.Groups;
using CadeAEscalacao.Server.Endpoints.Lineups;
using CadeAEscalacao.Server.Endpoints.Lists;
using CadeAEscalacao.Server.Endpoints.Matches;
using CadeAEscalacao.Server.Endpoints.Me;
using CadeAEscalacao.Server.Endpoints.Players;
using CadeAEscalacao.Server.Endpoints.Rankings;
using CadeAEscalacao.Server.Endpoints.Seasons;

namespace CadeAEscalacao.Server.Endpoints;

public static class EndpointExtensions
{
    /// <summary>
    /// Maps every endpoint of the API. Each feature has its own folder with a
    /// <c>MapXxxEndpoints()</c> method (ADR-013), so Program.cs stays short.
    /// </summary>
    public static IEndpointRouteBuilder MapApiEndpoints(this IEndpointRouteBuilder app)
    {
        var api = app.MapGroup("/api")
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound);

        // Login and profile: anonymous or signed in without a player yet (ADR-014).
        api.MapAuthEndpoints();
        api.MapMeEndpoints();

        // Everything else needs a signed-in person with a player; the roles are checked
        // by the use cases (ADR-016).
        var registered = api.MapGroup(string.Empty)
            .RequireAuthorization(AuthenticationSetup.RegisteredPlayerPolicy)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden);

        registered.MapPlayerEndpoints();
        registered.MapGroupEndpoints();
        registered.MapSeasonEndpoints();
        registered.MapListEndpoints();
        registered.MapLineupEndpoints();
        registered.MapMatchEndpoints();
        registered.MapRankingEndpoints();

        return app;
    }
}
