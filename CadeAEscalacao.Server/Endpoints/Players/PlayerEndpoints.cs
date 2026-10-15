using System.ComponentModel.DataAnnotations;
using CadeAEscalacao.Application.Players;
using CadeAEscalacao.Domain.Players;
using Microsoft.AspNetCore.Http.HttpResults;

namespace CadeAEscalacao.Server.Endpoints.Players;

public sealed record CreatePlayerRequest([Required, StringLength(100)] string Name);

public static class PlayerEndpoints
{
    public static IEndpointRouteBuilder MapPlayerEndpoints(this IEndpointRouteBuilder app)
    {
        var players = app.MapGroup("/players").WithTags("Players");

        players.MapPost("/", CreateAsync).WithSummary("Registers a player.");
        players.MapGet("/{id:guid}", GetAsync).WithSummary("Gets a player.");

        return app;
    }

    private static async Task<Created<PlayerResponse>> CreateAsync(
        CreatePlayerRequest request, CreatePlayer createPlayer, CancellationToken cancellationToken)
    {
        var player = await createPlayer.HandleAsync(request.Name, cancellationToken);
        return TypedResults.Created($"/api/players/{player.Id}", player);
    }

    private static async Task<Results<Ok<PlayerResponse>, NotFound>> GetAsync(
        Guid id, GetPlayer getPlayer, CancellationToken cancellationToken) =>
        await getPlayer.HandleAsync(new PlayerId(id), cancellationToken) is { } player
            ? TypedResults.Ok(player)
            : TypedResults.NotFound();
}
