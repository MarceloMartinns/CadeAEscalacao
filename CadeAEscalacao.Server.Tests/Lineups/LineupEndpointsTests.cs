using System.Net;
using CadeAEscalacao.Application.Lineups;
using CadeAEscalacao.Domain.Lineups;

namespace CadeAEscalacao.Server.Tests.Lineups;

public sealed class LineupEndpointsTests : ApiTestBase
{
    private readonly TestGroup _team = new();

    [Test]
    public async Task StartLineup_ClosedList_Returns201WithCoachAndPartner()
    {
        var list = _team.ClosedList();
        Api.Lists.Returns(list);
        Api.Groups.Returns(_team.Group);

        var response = await PostAsync($"/api/lists/{list.Id}/lineup");

        var lineup = await ReadAsync<LineupResponse>(response, HttpStatusCode.Created);
        response.Headers.Location!.OriginalString.ShouldBe($"/api/lineups/{lineup.Id}");
        lineup.CoachId.ShouldBe(_team.President.Value);
        lineup.PartnerId.ShouldBe(_team.Regular.Value);
        lineup.Status.ShouldBe(LineupStatus.Drafting);
        lineup.Players.Count.ShouldBe(4);
        Api.Lineups.Received(1).Add(Arg.Any<Lineup>());
    }

    [Test]
    public async Task StartLineup_ListStillOpen_Returns422()
    {
        var list = _team.OpenList();
        Api.Lists.Returns(list);
        Api.Groups.Returns(_team.Group);

        var response = await PostAsync($"/api/lists/{list.Id}/lineup");

        var problem = await ReadProblemAsync(response, HttpStatusCode.UnprocessableEntity);
        problem.Detail.ShouldBe("Close the list before making the lineup.");
    }

    [Test]
    public async Task StartLineup_ListAlreadyHasLineup_Returns409()
    {
        var list = _team.ClosedList();
        Api.Lists.Returns(list);
        Api.Lineups.ExistsForListAsync(list.Id, Arg.Any<CancellationToken>()).Returns(true);

        var response = await PostAsync($"/api/lists/{list.Id}/lineup");

        await ReadProblemAsync(response, HttpStatusCode.Conflict);
    }

    [Test]
    public async Task AssignTeams_PlayerMissing_Returns422()
    {
        var lineup = Lineup.Start(_team.Group, _team.ClosedList(), new Dictionary<Domain.Players.PlayerId, int>());
        Api.Lineups.Returns(lineup);

        var response = await PutAsync(
            $"/api/lineups/{lineup.Id}/teams",
            new { teamA = new[] { _team.Substitute1.Value }, teamB = Array.Empty<Guid>() });

        await ReadProblemAsync(response, HttpStatusCode.UnprocessableEntity);
    }

    [Test]
    public async Task AssignTeams_EveryPlayerOnATeam_WaitsForPartnerChoice()
    {
        var lineup = Lineup.Start(_team.Group, _team.ClosedList(), new Dictionary<Domain.Players.PlayerId, int>());
        Api.Lineups.Returns(lineup);

        var response = await PutAsync(
            $"/api/lineups/{lineup.Id}/teams",
            new { teamA = new[] { _team.Substitute1.Value }, teamB = new[] { _team.Substitute2.Value } });

        var body = await ReadAsync<LineupResponse>(response, HttpStatusCode.OK);
        body.Status.ShouldBe(LineupStatus.WaitingForPartnerChoice);
        body.TeamA.ShouldBe([_team.Substitute1.Value]);
    }

    [Test]
    public async Task ChooseTeam_TeamsAssigned_CompletesLineupAndMovesCoachToEndOfRotation()
    {
        var lineup = _team.LineupWithTeams(_team.ClosedList());
        Api.Lineups.Returns(lineup);
        Api.Groups.Returns(_team.Group);

        var response = await PostAsync($"/api/lineups/{lineup.Id}/choice", new { team = "B" });

        var body = await ReadAsync<LineupResponse>(response, HttpStatusCode.OK);
        body.Status.ShouldBe(LineupStatus.Completed);
        body.TeamB.ShouldContain(_team.Regular.Value);
        body.TeamA.ShouldContain(_team.President.Value);
        _team.Group.CoachRotation.ShouldBe([_team.Regular, _team.President]);
        await Api.UnitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task ChooseTeam_UnknownTeam_Returns400()
    {
        var response = await PostAsync($"/api/lineups/{Guid.NewGuid()}/choice", new { team = "C" });

        await ReadProblemAsync(response, HttpStatusCode.BadRequest);
    }
}
