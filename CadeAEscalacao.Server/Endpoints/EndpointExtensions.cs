using CadeAEscalacao.Server.Endpoints.Groups;
using CadeAEscalacao.Server.Endpoints.Lineups;
using CadeAEscalacao.Server.Endpoints.Lists;
using CadeAEscalacao.Server.Endpoints.Matches;
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

        api.MapPlayerEndpoints();
        api.MapGroupEndpoints();
        api.MapSeasonEndpoints();
        api.MapListEndpoints();
        api.MapLineupEndpoints();
        api.MapMatchEndpoints();
        api.MapRankingEndpoints();

        return app;
    }
}
