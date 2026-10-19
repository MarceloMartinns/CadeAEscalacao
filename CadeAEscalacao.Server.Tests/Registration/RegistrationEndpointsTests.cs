using System.Net;
using CadeAEscalacao.Application.Groups;
using CadeAEscalacao.Application.Players;
using CadeAEscalacao.Application.Seasons;
using CadeAEscalacao.Domain.Groups;
using CadeAEscalacao.Domain.Players;
using CadeAEscalacao.Domain.Seasons;

namespace CadeAEscalacao.Server.Tests.Registration;

/// <summary>Minimal registration: players, groups and seasons. Players are created with POST /api/me.</summary>
public sealed class RegistrationEndpointsTests : ApiTestBase
{
    private readonly TestGroup _team = new();

    [Test]
    public async Task GetPlayer_Exists_Returns200()
    {
        var player = Player.Create("Zé");
        Api.Players.GetByIdAsync(player.Id, Arg.Any<CancellationToken>()).Returns(Task.FromResult<Player?>(player));
        SignInAs(_team.Regular);

        var response = await Client.GetAsync($"/api/players/{player.Id}");

        (await ReadAsync<PlayerResponse>(response, HttpStatusCode.OK)).Name.ShouldBe("Zé");
    }

    [Test]
    public async Task GetPlayer_NotFound_Returns404()
    {
        SignInAs(_team.Regular);

        var response = await Client.GetAsync($"/api/players/{Guid.NewGuid()}");

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Test]
    public async Task CreateGroup_SignedInPlayer_Returns201WithThemAsPresidentAndRegular()
    {
        var presidentId = Guid.NewGuid();
        Api.Players.ExistsAsync(new PlayerId(presidentId), Arg.Any<CancellationToken>()).Returns(true);
        SignInAs(presidentId);

        var response = await PostAsync("/api/groups", new
        {
            name = "Society de terça",
            playersPerTeam = 7,
            benchPlayersPerTeam = 1,
            matchDay = "Tuesday",
        });

        var group = await ReadAsync<GroupResponse>(response, HttpStatusCode.Created);
        group.PresidentId.ShouldBe(presidentId);
        group.MatchDay.ShouldBe(DayOfWeek.Tuesday);
        group.Members.ShouldBe([new GroupMemberResponse(presidentId, PlayerType.Regular)]);
        group.CoachRotation.ShouldBe([presidentId]);
    }

    [Test]
    public async Task CreateGroup_PlayerOfTheTokenNotFound_Returns404()
    {
        SignInAs(Guid.NewGuid());

        var response = await PostAsync("/api/groups", new
        {
            name = "Futsal",
            playersPerTeam = 5,
            benchPlayersPerTeam = 1,
            matchDay = "Thursday",
        });

        await ReadProblemAsync(response, HttpStatusCode.NotFound);
        Api.Groups.DidNotReceive().Add(Arg.Any<Group>());
    }

    [Test]
    public async Task GetGroup_Member_Returns200()
    {
        Api.Groups.Returns(_team.Group);
        SignInAs(_team.Substitute1);

        var response = await Client.GetAsync($"/api/groups/{_team.Group.Id}");

        (await ReadAsync<GroupResponse>(response, HttpStatusCode.OK)).Id.ShouldBe(_team.Group.Id.Value);
    }

    [Test]
    public async Task GetGroup_NotAMember_Returns403()
    {
        Api.Groups.Returns(_team.Group);
        SignInAs(_team.Outsider);

        var response = await Client.GetAsync($"/api/groups/{_team.Group.Id}");

        var problem = await ReadProblemAsync(response, HttpStatusCode.Forbidden);
        problem.Detail.ShouldBe("Only members of the group can do this.");
    }

    [Test]
    public async Task AddMember_AlreadyMember_Returns422()
    {
        Api.Groups.Returns(_team.Group);
        Api.Players.ExistsAsync(_team.Regular, Arg.Any<CancellationToken>()).Returns(true);
        SignInAs(_team.President);

        var response = await PostAsync(
            $"/api/groups/{_team.Group.Id}/members", new { playerId = _team.Regular.Value, type = "Substitute" });

        var problem = await ReadProblemAsync(response, HttpStatusCode.UnprocessableEntity);
        problem.Detail.ShouldBe("The player is already a member of this group.");
    }

    [Test]
    public async Task AddMember_NotThePresident_Returns403()
    {
        Api.Groups.Returns(_team.Group);
        Api.Players.ExistsAsync(_team.Outsider, Arg.Any<CancellationToken>()).Returns(true);
        SignInAs(_team.Regular);

        var response = await PostAsync(
            $"/api/groups/{_team.Group.Id}/members", new { playerId = _team.Outsider.Value, type = "Substitute" });

        await ReadProblemAsync(response, HttpStatusCode.Forbidden);
        _team.Group.IsMember(_team.Outsider).ShouldBeFalse();
    }

    [Test]
    public async Task ReorderCoachRotation_EveryRegular_ReturnsNewOrder()
    {
        Api.Groups.Returns(_team.Group);
        SignInAs(_team.President);

        var response = await PutAsync(
            $"/api/groups/{_team.Group.Id}/coach-rotation",
            new { playerIds = new[] { _team.Regular.Value, _team.President.Value } });

        var group = await ReadAsync<GroupResponse>(response, HttpStatusCode.OK);
        group.CoachRotation.ShouldBe([_team.Regular.Value, _team.President.Value]);
    }

    [Test]
    public async Task CreateSeason_NoOverlap_Returns201EndingAfterSixMonths()
    {
        Api.Groups.Returns(_team.Group);
        SignInAs(_team.President);

        var response = await PostAsync(
            $"/api/groups/{_team.Group.Id}/seasons", new { name = "2026/2", start = new DateOnly(2026, 7, 1) });

        var season = await ReadAsync<SeasonResponse>(response, HttpStatusCode.Created);
        season.End.ShouldBe(new DateOnly(2026, 12, 31));
        Api.Seasons.Received(1).Add(Arg.Any<Season>());
    }

    [Test]
    public async Task CreateSeason_NotThePresident_Returns403()
    {
        Api.Groups.Returns(_team.Group);
        SignInAs(_team.Regular);

        var response = await PostAsync(
            $"/api/groups/{_team.Group.Id}/seasons", new { name = "2026/2", start = new DateOnly(2026, 7, 1) });

        await ReadProblemAsync(response, HttpStatusCode.Forbidden);
        Api.Seasons.DidNotReceive().Add(Arg.Any<Season>());
    }

    [Test]
    public async Task CreateSeason_OverlapsAnotherSeason_Returns409()
    {
        Api.Groups.Returns(_team.Group);
        Api.Seasons.OverlapsAsync(_team.Group.Id, Arg.Any<DateOnly>(), Arg.Any<DateOnly>(), Arg.Any<CancellationToken>())
            .Returns(true);
        SignInAs(_team.President);

        var response = await PostAsync(
            $"/api/groups/{_team.Group.Id}/seasons", new { name = "2026/2", start = new DateOnly(2026, 7, 1) });

        await ReadProblemAsync(response, HttpStatusCode.Conflict);
    }

    [Test]
    public async Task OpenApiDocument_Development_DescribesTheEndpoints()
    {
        var document = await Client.GetStringAsync("/openapi/v1.json");

        document.ShouldContain("/api/auth/email/verify");
        document.ShouldContain("/api/lists/{id}/entries/me");
        document.ShouldContain("/api/lineups/{id}/choice");
        document.ShouldContain("/api/seasons/{seasonId}/ranking");
    }
}
