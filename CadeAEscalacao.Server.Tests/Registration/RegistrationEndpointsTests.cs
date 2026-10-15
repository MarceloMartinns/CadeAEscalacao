using System.Net;
using CadeAEscalacao.Application.Groups;
using CadeAEscalacao.Application.Players;
using CadeAEscalacao.Application.Seasons;
using CadeAEscalacao.Domain.Groups;
using CadeAEscalacao.Domain.Players;
using CadeAEscalacao.Domain.Seasons;

namespace CadeAEscalacao.Server.Tests.Registration;

/// <summary>Minimal registration: players, groups and seasons.</summary>
public sealed class RegistrationEndpointsTests : ApiTestBase
{
    [Test]
    public async Task CreatePlayer_ValidName_Returns201WithTrimmedName()
    {
        var response = await PostAsync("/api/players", new { name = "  Zé  " });

        var player = await ReadAsync<PlayerResponse>(response, HttpStatusCode.Created);
        player.Name.ShouldBe("Zé");
        Api.Players.Received(1).Add(Arg.Is<Player>(added => added.Name == "Zé"));
    }

    [Test]
    public async Task CreatePlayer_EmptyName_Returns400()
    {
        var response = await PostAsync("/api/players", new { name = "" });

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        Api.Players.DidNotReceive().Add(Arg.Any<Player>());
    }

    [Test]
    public async Task GetPlayer_NotFound_Returns404()
    {
        var response = await Client.GetAsync($"/api/players/{Guid.NewGuid()}");

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Test]
    public async Task CreateGroup_PresidentExists_Returns201WithPresidentAsRegular()
    {
        var presidentId = Guid.NewGuid();
        Api.Players.ExistsAsync(new PlayerId(presidentId), Arg.Any<CancellationToken>()).Returns(true);

        var response = await PostAsync("/api/groups", new
        {
            name = "Society de terça",
            playersPerTeam = 7,
            benchPlayersPerTeam = 1,
            matchDay = "Tuesday",
            presidentId,
        });

        var group = await ReadAsync<GroupResponse>(response, HttpStatusCode.Created);
        group.MatchDay.ShouldBe(DayOfWeek.Tuesday);
        group.Members.ShouldBe([new GroupMemberResponse(presidentId, PlayerType.Regular)]);
        group.CoachRotation.ShouldBe([presidentId]);
    }

    [Test]
    public async Task CreateGroup_PresidentNotFound_Returns404()
    {
        var response = await PostAsync("/api/groups", new
        {
            name = "Futsal",
            playersPerTeam = 5,
            benchPlayersPerTeam = 1,
            matchDay = "Thursday",
            presidentId = Guid.NewGuid(),
        });

        await ReadProblemAsync(response, HttpStatusCode.NotFound);
        Api.Groups.DidNotReceive().Add(Arg.Any<Group>());
    }

    [Test]
    public async Task AddMember_AlreadyMember_Returns422()
    {
        var team = new TestGroup();
        Api.Groups.Returns(team.Group);
        Api.Players.ExistsAsync(team.Regular, Arg.Any<CancellationToken>()).Returns(true);

        var response = await PostAsync(
            $"/api/groups/{team.Group.Id}/members", new { playerId = team.Regular.Value, type = "Substitute" });

        var problem = await ReadProblemAsync(response, HttpStatusCode.UnprocessableEntity);
        problem.Detail.ShouldBe("The player is already a member of this group.");
    }

    [Test]
    public async Task ReorderCoachRotation_EveryRegular_ReturnsNewOrder()
    {
        var team = new TestGroup();
        Api.Groups.Returns(team.Group);

        var response = await PutAsync(
            $"/api/groups/{team.Group.Id}/coach-rotation",
            new { playerIds = new[] { team.Regular.Value, team.President.Value } });

        var group = await ReadAsync<GroupResponse>(response, HttpStatusCode.OK);
        group.CoachRotation.ShouldBe([team.Regular.Value, team.President.Value]);
    }

    [Test]
    public async Task CreateSeason_NoOverlap_Returns201EndingAfterSixMonths()
    {
        var team = new TestGroup();
        Api.Groups.Returns(team.Group);

        var response = await PostAsync(
            $"/api/groups/{team.Group.Id}/seasons", new { name = "2026/2", start = new DateOnly(2026, 7, 1) });

        var season = await ReadAsync<SeasonResponse>(response, HttpStatusCode.Created);
        season.End.ShouldBe(new DateOnly(2026, 12, 31));
        Api.Seasons.Received(1).Add(Arg.Any<Season>());
    }

    [Test]
    public async Task CreateSeason_OverlapsAnotherSeason_Returns409()
    {
        var team = new TestGroup();
        Api.Groups.Returns(team.Group);
        Api.Seasons.OverlapsAsync(team.Group.Id, Arg.Any<DateOnly>(), Arg.Any<DateOnly>(), Arg.Any<CancellationToken>())
            .Returns(true);

        var response = await PostAsync(
            $"/api/groups/{team.Group.Id}/seasons", new { name = "2026/2", start = new DateOnly(2026, 7, 1) });

        await ReadProblemAsync(response, HttpStatusCode.Conflict);
    }

    [Test]
    public async Task OpenApiDocument_Development_DescribesTheEndpoints()
    {
        var document = await Client.GetStringAsync("/openapi/v1.json");

        document.ShouldContain("/api/lists/{id}/entries");
        document.ShouldContain("/api/lineups/{id}/choice");
        document.ShouldContain("/api/seasons/{seasonId}/ranking");
    }
}
