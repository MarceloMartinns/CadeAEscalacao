using System.ComponentModel.DataAnnotations;
using CadeAEscalacao.Application.Matches;
using CadeAEscalacao.Domain.Lineups;
using CadeAEscalacao.Domain.Matches;
using Microsoft.AspNetCore.Http.HttpResults;

namespace CadeAEscalacao.Server.Endpoints.Matches;

public sealed record ScoreRequest([Range(0, 99)] int GoalsA, [Range(0, 99)] int GoalsB);

public static class MatchEndpoints
{
    public static IEndpointRouteBuilder MapMatchEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/lineups/{lineupId:guid}/match", RecordAsync)
            .WithTags("Matches")
            .WithSummary("Records the score of the match played with a completed lineup.")
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status422UnprocessableEntity);

        var matches = app.MapGroup("/matches").WithTags("Matches");

        matches.MapGet("/{id:guid}", GetAsync).WithSummary("Gets a match.");
        matches.MapPut("/{id:guid}/score", CorrectScoreAsync)
            .WithSummary("Fixes a score that was recorded wrong.")
            .ProducesProblem(StatusCodes.Status409Conflict);

        return app;
    }

    private static async Task<Created<MatchResponse>> RecordAsync(
        Guid lineupId, ScoreRequest request, RecordMatch recordMatch, CancellationToken cancellationToken)
    {
        var match = await recordMatch.HandleAsync(
            new LineupId(lineupId), request.GoalsA, request.GoalsB, cancellationToken);
        return TypedResults.Created($"/api/matches/{match.Id}", match);
    }

    private static async Task<Results<Ok<MatchResponse>, NotFound>> GetAsync(
        Guid id, GetMatch getMatch, CancellationToken cancellationToken) =>
        await getMatch.HandleAsync(new MatchId(id), cancellationToken) is { } match
            ? TypedResults.Ok(match)
            : TypedResults.NotFound();

    private static async Task<Ok<MatchResponse>> CorrectScoreAsync(
        Guid id, ScoreRequest request, CorrectScore correctScore, CancellationToken cancellationToken) =>
        TypedResults.Ok(await correctScore.HandleAsync(new MatchId(id), request.GoalsA, request.GoalsB, cancellationToken));
}
