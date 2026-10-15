using CadeAEscalacao.Application.Rankings;
using CadeAEscalacao.Domain.Seasons;
using Microsoft.AspNetCore.Http.HttpResults;

namespace CadeAEscalacao.Server.Endpoints.Rankings;

public static class RankingEndpoints
{
    public static IEndpointRouteBuilder MapRankingEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/seasons/{seasonId:guid}/ranking", GetAsync)
            .WithTags("Rankings")
            .WithSummary("Season ranking: points (win 3, draw 1), then attendances, then wins.");

        return app;
    }

    private static async Task<Results<Ok<SeasonRankingResponse>, NotFound>> GetAsync(
        Guid seasonId, GetSeasonRanking getRanking, CancellationToken cancellationToken) =>
        await getRanking.HandleAsync(new SeasonId(seasonId), cancellationToken) is { } ranking
            ? TypedResults.Ok(ranking)
            : TypedResults.NotFound();
}
