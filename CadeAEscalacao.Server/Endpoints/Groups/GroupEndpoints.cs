using System.ComponentModel.DataAnnotations;
using CadeAEscalacao.Application.Groups;
using CadeAEscalacao.Domain.Groups;
using CadeAEscalacao.Domain.Players;
using Microsoft.AspNetCore.Http.HttpResults;

namespace CadeAEscalacao.Server.Endpoints.Groups;

public sealed record CreateGroupRequest(
    [Required, StringLength(100)] string Name,
    [Range(1, 11)] int PlayersPerTeam,
    [Range(0, 11)] int BenchPlayersPerTeam,
    [Required] DayOfWeek? MatchDay);

public sealed record AddMemberRequest(Guid PlayerId, [EnumDataType(typeof(PlayerType))] PlayerType Type);

public sealed record CreatePairRequest(Guid FirstPlayerId, Guid SecondPlayerId);

public sealed record ReorderCoachRotationRequest([Required] IReadOnlyList<Guid> PlayerIds);

public static class GroupEndpoints
{
    public static IEndpointRouteBuilder MapGroupEndpoints(this IEndpointRouteBuilder app)
    {
        var groups = app.MapGroup("/groups")
            .WithTags("Groups")
            .ProducesProblem(StatusCodes.Status422UnprocessableEntity);

        groups.MapPost("/", CreateAsync).WithSummary("Creates a group. The signed-in player is the president and its first regular member.");
        groups.MapGet("/{id:guid}", GetAsync).WithSummary("Gets a group with its members, pairs and coach rotation.");
        groups.MapPost("/{id:guid}/members", AddMemberAsync).WithSummary("Adds a regular or substitute member.");
        groups.MapPost("/{id:guid}/pairs", CreatePairAsync).WithSummary("Creates a pair of members.");
        groups.MapPut("/{id:guid}/coach-rotation", ReorderCoachRotationAsync)
            .WithSummary("Sets the coach order. It must list every regular member exactly once.");

        return app;
    }

    private static async Task<Created<GroupResponse>> CreateAsync(
        CreateGroupRequest request, CreateGroup createGroup, CancellationToken cancellationToken)
    {
        var group = await createGroup.HandleAsync(
            request.Name,
            new MatchFormat(request.PlayersPerTeam, request.BenchPlayersPerTeam),
            request.MatchDay!.Value,
            cancellationToken);
        return TypedResults.Created($"/api/groups/{group.Id}", group);
    }

    private static async Task<Results<Ok<GroupResponse>, NotFound>> GetAsync(
        Guid id, GetGroup getGroup, CancellationToken cancellationToken) =>
        await getGroup.HandleAsync(new GroupId(id), cancellationToken) is { } group
            ? TypedResults.Ok(group)
            : TypedResults.NotFound();

    private static async Task<Ok<GroupResponse>> AddMemberAsync(
        Guid id, AddMemberRequest request, AddGroupMember addMember, CancellationToken cancellationToken) =>
        TypedResults.Ok(await addMember.HandleAsync(
            new GroupId(id), new PlayerId(request.PlayerId), request.Type, cancellationToken));

    private static async Task<Ok<GroupResponse>> CreatePairAsync(
        Guid id, CreatePairRequest request, CreatePair createPair, CancellationToken cancellationToken) =>
        TypedResults.Ok(await createPair.HandleAsync(
            new GroupId(id), new PlayerId(request.FirstPlayerId), new PlayerId(request.SecondPlayerId), cancellationToken));

    private static async Task<Ok<GroupResponse>> ReorderCoachRotationAsync(
        Guid id,
        ReorderCoachRotationRequest request,
        ReorderCoachRotation reorderCoachRotation,
        CancellationToken cancellationToken) =>
        TypedResults.Ok(await reorderCoachRotation.HandleAsync(
            new GroupId(id), request.PlayerIds.Select(playerId => new PlayerId(playerId)).ToList(), cancellationToken));
}
