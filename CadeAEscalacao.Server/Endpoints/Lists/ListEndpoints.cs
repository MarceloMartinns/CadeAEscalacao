using System.ComponentModel.DataAnnotations;
using CadeAEscalacao.Application.Common;
using CadeAEscalacao.Application.Lists;
using CadeAEscalacao.Domain.Groups;
using CadeAEscalacao.Domain.Players;
using CadeAEscalacao.Domain.WeeklyLists;
using Microsoft.AspNetCore.Http.HttpResults;

namespace CadeAEscalacao.Server.Endpoints.Lists;

public sealed record OpenListRequest([Required] DateOnly? MatchDate);

public sealed record SignUpRequest(Guid PlayerId, [Required, MinLength(1)] IReadOnlyList<Position> Positions);

public sealed record ChangePositionsRequest([Required, MinLength(1)] IReadOnlyList<Position> Positions);

/// <summary>
/// Weekly list ("lista semanal"). Until authentication (etapa 7), the player who acts
/// comes in the route or in the body.
/// </summary>
public static class ListEndpoints
{
    public static IEndpointRouteBuilder MapListEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/groups/{groupId:guid}/lists", OpenAsync)
            .WithTags("Lists")
            .WithSummary("Opens the list of a match date, with every regular pending.")
            .ProducesProblem(StatusCodes.Status409Conflict);

        var lists = app.MapGroup("/lists")
            .WithTags("Lists")
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status422UnprocessableEntity);

        lists.MapGet("/{id:guid}", GetAsync).WithSummary("Gets the list with who plays, who is pending and who is waiting.");
        lists.MapPost("/{id:guid}/entries", SignUpAsync).WithSummary("A member confirms presence.");
        lists.MapPut("/{id:guid}/entries/{playerId:guid}", ChangePositionsAsync)
            .WithSummary("A confirmed player changes their positions.");
        lists.MapDelete("/{id:guid}/entries/{playerId:guid}", WithdrawAsync)
            .WithSummary("The player declines or leaves the list.");
        lists.MapPost("/{id:guid}/close", CloseAsync).WithSummary("Closes the list. Pending regulars lose their spot.");
        lists.MapPost("/{id:guid}/cancel", CancelAsync).WithSummary("Cancels the match.");

        return app;
    }

    private static async Task<Created<WeeklyListResponse>> OpenAsync(
        Guid groupId, OpenListRequest request, OpenWeeklyList openList, CancellationToken cancellationToken)
    {
        var list = await openList.HandleAsync(new GroupId(groupId), request.MatchDate!.Value, cancellationToken);
        return TypedResults.Created($"/api/lists/{list.Id}", list);
    }

    private static async Task<Results<Ok<WeeklyListResponse>, NotFound>> GetAsync(
        Guid id, GetWeeklyList getList, CancellationToken cancellationToken) =>
        await getList.HandleAsync(new WeeklyListId(id), cancellationToken) is { } list
            ? TypedResults.Ok(list)
            : TypedResults.NotFound();

    private static async Task<Ok<WeeklyListResponse>> SignUpAsync(
        Guid id, SignUpRequest request, SignUpForList signUp, CancellationToken cancellationToken) =>
        TypedResults.Ok(await signUp.HandleAsync(
            new WeeklyListId(id), new PlayerId(request.PlayerId), Positions.Combine(request.Positions), cancellationToken));

    private static async Task<Ok<WeeklyListResponse>> ChangePositionsAsync(
        Guid id,
        Guid playerId,
        ChangePositionsRequest request,
        ChangeListPositions changePositions,
        CancellationToken cancellationToken) =>
        TypedResults.Ok(await changePositions.HandleAsync(
            new WeeklyListId(id), new PlayerId(playerId), Positions.Combine(request.Positions), cancellationToken));

    private static async Task<Ok<WeeklyListResponse>> WithdrawAsync(
        Guid id, Guid playerId, WithdrawFromList withdraw, CancellationToken cancellationToken) =>
        TypedResults.Ok(await withdraw.HandleAsync(new WeeklyListId(id), new PlayerId(playerId), cancellationToken));

    private static async Task<Ok<WeeklyListResponse>> CloseAsync(
        Guid id, CloseWeeklyList closeList, CancellationToken cancellationToken) =>
        TypedResults.Ok(await closeList.HandleAsync(new WeeklyListId(id), cancellationToken));

    private static async Task<Ok<WeeklyListResponse>> CancelAsync(
        Guid id, CancelWeeklyList cancelList, CancellationToken cancellationToken) =>
        TypedResults.Ok(await cancelList.HandleAsync(new WeeklyListId(id), cancellationToken));
}
