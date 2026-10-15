using System.ComponentModel.DataAnnotations;
using CadeAEscalacao.Application.Seasons;
using CadeAEscalacao.Domain.Groups;
using CadeAEscalacao.Domain.Seasons;
using Microsoft.AspNetCore.Http.HttpResults;

namespace CadeAEscalacao.Server.Endpoints.Seasons;

public sealed record CreateSeasonRequest(
    [Required, StringLength(100)] string Name,
    [Required] DateOnly? Start,
    [Range(1, 24)] int DurationInMonths = Season.DefaultDurationInMonths);

public static class SeasonEndpoints
{
    public static IEndpointRouteBuilder MapSeasonEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/groups/{groupId:guid}/seasons", CreateAsync)
            .WithTags("Seasons")
            .WithSummary("Creates a season for the group (default: 6 months).")
            .ProducesProblem(StatusCodes.Status409Conflict);

        return app;
    }

    private static async Task<Created<SeasonResponse>> CreateAsync(
        Guid groupId, CreateSeasonRequest request, CreateSeason createSeason, CancellationToken cancellationToken)
    {
        var season = await createSeason.HandleAsync(
            new GroupId(groupId), request.Name, request.Start!.Value, request.DurationInMonths, cancellationToken);
        return TypedResults.Created($"/api/seasons/{season.Id}/ranking", season);
    }
}
