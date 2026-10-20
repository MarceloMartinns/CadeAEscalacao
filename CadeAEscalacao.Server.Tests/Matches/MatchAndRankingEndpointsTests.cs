using System.Net;
using CadeAEscalacao.Application.Matches;
using CadeAEscalacao.Application.Rankings;
using CadeAEscalacao.Domain.Matches;
using CadeAEscalacao.Domain.Players;
using CadeAEscalacao.Domain.Seasons;
using Microsoft.AspNetCore.Http;

namespace CadeAEscalacao.Server.Tests.Matches;

public sealed class MatchAndRankingEndpointsTests : ApiTestBase
{
    private readonly TestGroup _team = new();

    [Test]
    public async Task RecordMatch_CompletedLineup_Returns201WithTeamsAndScore()
    {
        var list = _team.ClosedList();
        var lineup = _team.CompletedLineup(list);
        Api.Lists.Returns(list);
        Api.Lineups.Returns(lineup);
        Api.Groups.Returns(_team.Group);
        SignInAs(_team.President);

        var response = await PostAsync($"/api/lineups/{lineup.Id}/match", new { goalsA = 3, goalsB = 1 });

        var match = await ReadAsync<MatchResponse>(response, HttpStatusCode.Created);
        match.Date.ShouldBe(TestGroup.MatchDate);
        match.GoalsA.ShouldBe(3);
        match.TeamA.ShouldBe(lineup.TeamA.Select(playerId => playerId.Value));
        Api.Matches.Received(1).Add(Arg.Any<Match>());
    }

    [Test]
    public async Task RecordMatch_PresidentWhoIsNotTheCoach_Returns201()
    {
        // The regular coaches this week; the president is their partner.
        _team.Group.ReorderCoachRotation([_team.Regular, _team.President]);
        var list = _team.ClosedList();
        var lineup = _team.CompletedLineup(list);
        Api.Lists.Returns(list);
        Api.Lineups.Returns(lineup);
        Api.Groups.Returns(_team.Group);
        SignInAs(_team.President);

        var response = await PostAsync($"/api/lineups/{lineup.Id}/match", new { goalsA = 1, goalsB = 1 });

        lineup.CoachId.ShouldBe(_team.Regular);
        response.StatusCode.ShouldBe(HttpStatusCode.Created);
    }

    [Test]
    public async Task RecordMatch_NeitherCoachNorPresident_Returns403()
    {
        var list = _team.ClosedList();
        var lineup = _team.CompletedLineup(list);
        Api.Lists.Returns(list);
        Api.Lineups.Returns(lineup);
        Api.Groups.Returns(_team.Group);
        SignInAs(_team.Substitute1);

        var response = await PostAsync($"/api/lineups/{lineup.Id}/match", new { goalsA = 3, goalsB = 1 });

        await ReadProblemAsync(response, HttpStatusCode.Forbidden);
        Api.Matches.DidNotReceive().Add(Arg.Any<Match>());
    }

    [Test]
    public async Task RecordMatch_AlreadyRecorded_Returns409()
    {
        var lineup = _team.CompletedLineup(_team.ClosedList());
        Api.Lineups.Returns(lineup);
        Api.Groups.Returns(_team.Group);
        Api.Matches.ExistsForLineupAsync(lineup.Id, Arg.Any<CancellationToken>()).Returns(true);
        SignInAs(_team.President);

        var response = await PostAsync($"/api/lineups/{lineup.Id}/match", new { goalsA = 0, goalsB = 0 });

        await ReadProblemAsync(response, HttpStatusCode.Conflict);
    }

    [Test]
    public async Task RecordMatch_LineupNotCompleted_Returns422()
    {
        var list = _team.ClosedList();
        var lineup = _team.LineupWithTeams(list);
        Api.Lists.Returns(list);
        Api.Lineups.Returns(lineup);
        Api.Groups.Returns(_team.Group);
        SignInAs(_team.President);

        var response = await PostAsync($"/api/lineups/{lineup.Id}/match", new { goalsA = 2, goalsB = 2 });

        await ReadProblemAsync(response, HttpStatusCode.UnprocessableEntity);
    }

    [Test]
    public async Task CorrectScore_NegativeGoals_Returns400WithValidationErrors()
    {
        SignInAs(_team.President);

        var response = await PutAsync($"/api/matches/{Guid.NewGuid()}/score", new { goalsA = -1, goalsB = 0 });

        var problem = await ReadAsync<HttpValidationProblemDetails>(response, HttpStatusCode.BadRequest);
        problem.Errors.Keys.ShouldContain(key => key.Contains("GoalsA", StringComparison.OrdinalIgnoreCase));
    }

    [Test]
    public async Task CorrectScore_MatchNotFound_Returns404()
    {
        SignInAs(_team.President);

        var response = await PutAsync($"/api/matches/{Guid.NewGuid()}/score", new { goalsA = 1, goalsB = 0 });

        await ReadProblemAsync(response, HttpStatusCode.NotFound);
    }

    [Test]
    public async Task CorrectScore_NotThePresident_Returns403()
    {
        var list = _team.ClosedList();
        var match = Match.Record(list, _team.CompletedLineup(list), goalsA: 3, goalsB: 1);
        Api.Matches.GetByIdAsync(match.Id, Arg.Any<CancellationToken>()).Returns(Task.FromResult<Match?>(match));
        Api.Groups.Returns(_team.Group);
        SignInAs(_team.Regular);

        var response = await PutAsync($"/api/matches/{match.Id}/score", new { goalsA = 0, goalsB = 1 });

        await ReadProblemAsync(response, HttpStatusCode.Forbidden);
        match.GoalsA.ShouldBe(3);
    }

    [Test]
    public async Task GetRanking_OneMatch_WinnersShareFirstPlace()
    {
        var season = SeasonWithOneMatch();
        SignInAs(_team.Substitute3);

        var response = await Client.GetAsync($"/api/seasons/{season.Id}/ranking");

        // Team A (substitute 1 + regular) won 3 x 1.
        var ranking = await ReadAsync<SeasonRankingResponse>(response, HttpStatusCode.OK);
        ranking.Entries.Count.ShouldBe(4);
        ranking.Entries.Take(2).Select(entry => entry.PlayerId)
            .ShouldBe([_team.Substitute1.Value, _team.Regular.Value], ignoreOrder: true);
        ranking.Entries.Take(2).ShouldAllBe(entry => entry.Position == 1 && entry.Points == 3);
        ranking.Entries.Skip(2).ShouldAllBe(entry => entry.Position == 3 && entry.Points == 0);
    }

    [Test]
    public async Task GetRanking_BringsTheGroupAndTheNameOfEachPlayer()
    {
        var season = SeasonWithOneMatch();
        Api.Players.GetNamesAsync(Arg.Any<IReadOnlyCollection<PlayerId>>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyDictionary<PlayerId, string>>(
                new Dictionary<PlayerId, string> { [_team.Substitute1] = "Pedro", [_team.Regular] = "João" }));
        SignInAs(_team.Substitute3);

        var response = await Client.GetAsync($"/api/seasons/{season.Id}/ranking");

        // The names come with the ranking: someone who left the group is still in it (ADR-021).
        var ranking = await ReadAsync<SeasonRankingResponse>(response, HttpStatusCode.OK);
        ranking.GroupId.ShouldBe(_team.Group.Id.Value);
        ranking.Entries.Single(entry => entry.PlayerId == _team.Substitute1.Value).PlayerName.ShouldBe("Pedro");
        ranking.Entries.Single(entry => entry.PlayerId == _team.Regular.Value).PlayerName.ShouldBe("João");
    }

    [Test]
    public async Task GetRanking_NotAMember_Returns403()
    {
        var season = SeasonWithOneMatch();
        SignInAs(_team.Outsider);

        var response = await Client.GetAsync($"/api/seasons/{season.Id}/ranking");

        await ReadProblemAsync(response, HttpStatusCode.Forbidden);
    }

    [Test]
    public async Task GetRanking_SeasonNotFound_Returns404()
    {
        SignInAs(_team.President);

        var response = await Client.GetAsync($"/api/seasons/{Guid.NewGuid()}/ranking");

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    private Season SeasonWithOneMatch()
    {
        var list = _team.ClosedList();
        var match = Match.Record(list, _team.CompletedLineup(list), goalsA: 3, goalsB: 1);
        var season = Season.Create(_team.Group.Id, "2026/2", new DateOnly(2026, 7, 1));
        Api.Seasons.GetByIdAsync(season.Id, Arg.Any<CancellationToken>()).Returns(Task.FromResult<Season?>(season));
        Api.Matches.ListByGroupAsync(_team.Group.Id, season.Start, season.End, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyList<Match>>([match]));
        Api.Groups.Returns(_team.Group);
        return season;
    }
}
