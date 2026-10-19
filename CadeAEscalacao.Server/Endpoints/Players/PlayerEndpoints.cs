using CadeAEscalacao.Application.Players;
using CadeAEscalacao.Domain.Players;
using Microsoft.AspNetCore.Http.HttpResults;

namespace CadeAEscalacao.Server.Endpoints.Players;

/// <summary>Players. Each person creates their own player with <c>POST /api/me</c> (ADR-017).</summary>
public static class PlayerEndpoints
{
    public static IEndpointRouteBuilder MapPlayerEndpoints(this IEndpointRouteBuilder app)
    {
        var players = app.MapGroup("/players").WithTags("Players");

        players.MapGet("/{id:guid}", GetAsync).WithSummary("Gets a player.");

        return app;
    }

    private static async Task<Results<Ok<PlayerResponse>, NotFound>> GetAsync(
        Guid id, GetPlayer getPlayer, CancellationToken cancellationToken) =>
        await getPlayer.HandleAsync(new PlayerId(id), cancellationToken) is { } player
            ? TypedResults.Ok(player)
            : TypedResults.NotFound();
}
