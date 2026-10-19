using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using CadeAEscalacao.Application.Common;
using CadeAEscalacao.Application.Players;
using CadeAEscalacao.Domain.Players;
using CadeAEscalacao.Infrastructure.Identity;
using CadeAEscalacao.Server.Authentication;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Identity;

namespace CadeAEscalacao.Server.Endpoints.Me;

public sealed record CreateProfileRequest([Required, StringLength(100)] string Name);

/// <summary>The account and the player of the signed-in person. <see cref="Player"/> is null until the profile is created.</summary>
public sealed record MeResponse(Guid UserId, string Email, PlayerResponse? Player);

/// <summary>The new player and an access token that already has the <c>player_id</c> claim.</summary>
public sealed record CreateProfileResponse(PlayerResponse Player, AccessTokenResponse Token);

/// <summary>Profile of the signed-in person (ADR-014): links the login account to a <see cref="Player"/>.</summary>
public static class MeEndpoints
{
    public static IEndpointRouteBuilder MapMeEndpoints(this IEndpointRouteBuilder app)
    {
        var me = app.MapGroup("/me")
            .WithTags("Me")
            .RequireAuthorization()
            .ProducesProblem(StatusCodes.Status401Unauthorized);

        me.MapGet("/", GetAsync).WithSummary("Gets the signed-in account and its player.");
        me.MapPost("/", CreateProfileAsync)
            .WithSummary("Creates the player of the signed-in account. Returns an access token with the player.")
            .ProducesProblem(StatusCodes.Status409Conflict);

        return app;
    }

    private static async Task<Results<Ok<MeResponse>, UnauthorizedHttpResult>> GetAsync(
        ClaimsPrincipal principal,
        UserManager<AppUser> users,
        GetPlayer getPlayer,
        CancellationToken cancellationToken)
    {
        if (await users.FindByIdAsync(principal.GetUserId().ToString()) is not { } user)
        {
            return TypedResults.Unauthorized();
        }

        var player = user.PlayerId is { } playerId
            ? await getPlayer.HandleAsync(new PlayerId(playerId), cancellationToken)
            : null;
        return TypedResults.Ok(new MeResponse(user.Id, user.Email!, player));
    }

    /// <summary>
    /// Two steps without a single transaction (ADR-014): the player is saved first, then linked to
    /// the account. If the link fails, a player without account and without group is left behind.
    /// </summary>
    private static async Task<Results<Created<CreateProfileResponse>, UnauthorizedHttpResult>> CreateProfileAsync(
        CreateProfileRequest request,
        ClaimsPrincipal principal,
        UserManager<AppUser> users,
        CreatePlayer createPlayer,
        TokenService tokens,
        CancellationToken cancellationToken)
    {
        if (await users.FindByIdAsync(principal.GetUserId().ToString()) is not { } user)
        {
            return TypedResults.Unauthorized();
        }

        if (user.PlayerId is not null)
        {
            throw new ConflictException("This account already has a player.");
        }

        var player = await createPlayer.HandleAsync(request.Name, cancellationToken);
        user.PlayerId = player.Id;
        var result = await users.UpdateAsync(user);
        if (!result.Succeeded)
        {
            throw new ConflictException("The account changed at the same time. Reload and try again.");
        }

        return TypedResults.Created("/api/me", new CreateProfileResponse(player, tokens.CreateAccessToken(user)));
    }
}
