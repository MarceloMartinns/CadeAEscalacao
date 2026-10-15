using System.ComponentModel.DataAnnotations;
using CadeAEscalacao.Application.Lineups;
using CadeAEscalacao.Domain.Lineups;
using CadeAEscalacao.Domain.Players;
using CadeAEscalacao.Domain.WeeklyLists;
using Microsoft.AspNetCore.Http.HttpResults;

namespace CadeAEscalacao.Server.Endpoints.Lineups;

public sealed record SetPartnerRequest(Guid PlayerId);

public sealed record AssignTeamsRequest([Required] IReadOnlyList<Guid> TeamA, [Required] IReadOnlyList<Guid> TeamB);

public sealed record ChooseTeamRequest([EnumDataType(typeof(Team))] Team Team);

/// <summary>Lineup ("escalação"): the coach's flow described in <see cref="Lineup"/>.</summary>
public static class LineupEndpoints
{
    public static IEndpointRouteBuilder MapLineupEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/lists/{listId:guid}/lineup", StartAsync)
            .WithTags("Lineups")
            .WithSummary("Starts the lineup of a closed list and picks the coach of the week.")
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status422UnprocessableEntity);

        var lineups = app.MapGroup("/lineups")
            .WithTags("Lineups")
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status422UnprocessableEntity);

        lineups.MapGet("/{id:guid}", GetAsync).WithSummary("Gets a lineup.");
        lineups.MapPost("/{id:guid}/partner", SetPartnerAsync)
            .WithSummary("The president picks the coach's partner when their pair is not playing.");
        lineups.MapPut("/{id:guid}/teams", AssignTeamsAsync)
            .WithSummary("The coach splits the players, without themselves and their partner.");
        lineups.MapPost("/{id:guid}/choice", ChooseTeamAsync)
            .WithSummary("The partner picks a team; the coach goes to the other one and to the end of the rotation.");

        return app;
    }

    private static async Task<Created<LineupResponse>> StartAsync(
        Guid listId, StartLineup startLineup, CancellationToken cancellationToken)
    {
        var lineup = await startLineup.HandleAsync(new WeeklyListId(listId), cancellationToken);
        return TypedResults.Created($"/api/lineups/{lineup.Id}", lineup);
    }

    private static async Task<Results<Ok<LineupResponse>, NotFound>> GetAsync(
        Guid id, GetLineup getLineup, CancellationToken cancellationToken) =>
        await getLineup.HandleAsync(new LineupId(id), cancellationToken) is { } lineup
            ? TypedResults.Ok(lineup)
            : TypedResults.NotFound();

    private static async Task<Ok<LineupResponse>> SetPartnerAsync(
        Guid id, SetPartnerRequest request, SetPartnerOfTheDay setPartner, CancellationToken cancellationToken) =>
        TypedResults.Ok(await setPartner.HandleAsync(new LineupId(id), new PlayerId(request.PlayerId), cancellationToken));

    private static async Task<Ok<LineupResponse>> AssignTeamsAsync(
        Guid id, AssignTeamsRequest request, AssignTeams assignTeams, CancellationToken cancellationToken) =>
        TypedResults.Ok(await assignTeams.HandleAsync(
            new LineupId(id),
            request.TeamA.Select(playerId => new PlayerId(playerId)).ToList(),
            request.TeamB.Select(playerId => new PlayerId(playerId)).ToList(),
            cancellationToken));

    private static async Task<Ok<LineupResponse>> ChooseTeamAsync(
        Guid id, ChooseTeamRequest request, ChoosePartnerTeam chooseTeam, CancellationToken cancellationToken) =>
        TypedResults.Ok(await chooseTeam.HandleAsync(new LineupId(id), request.Team, cancellationToken));
}
