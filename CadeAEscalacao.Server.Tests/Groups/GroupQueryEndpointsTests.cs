using System.Net;
using CadeAEscalacao.Application.Groups;
using CadeAEscalacao.Application.Lists;
using CadeAEscalacao.Application.Seasons;
using CadeAEscalacao.Domain.Groups;
using CadeAEscalacao.Domain.Lineups;
using CadeAEscalacao.Domain.Matches;
using CadeAEscalacao.Domain.Players;
using CadeAEscalacao.Domain.Seasons;
using CadeAEscalacao.Domain.WeeklyLists;
using CadeAEscalacao.Infrastructure.Identity;

namespace CadeAEscalacao.Server.Tests.Groups;

/// <summary>What the screens need to find their way (ADR-021): my groups, names, lists, seasons and members by e-mail.</summary>
public sealed class GroupQueryEndpointsTests : ApiTestBase
{
    private readonly TestGroup _team = new();

    [Test]
    public async Task ListMyGroups_Member_ReturnsTheirGroupsByName()
    {
        var futsal = Group.Create("Futsal", MatchFormat.Futsal, DayOfWeek.Thursday, _team.Regular);
        Api.Groups.ListByMemberAsync(_team.Regular, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyList<Group>>([_team.Group, futsal]));
        SignInAs(_team.Regular);

        var response = await Client.GetAsync("/api/groups");

        var groups = await ReadAsync<List<GroupSummaryResponse>>(response, HttpStatusCode.OK);
        groups.Select(group => group.Name).ShouldBe(["Futsal", "Terça"]);
        groups[0].PresidentId.ShouldBe(_team.Regular.Value);
    }

    [Test]
    public async Task GetGroup_Member_ReturnsTheNameOfEachMember()
    {
        Api.Groups.Returns(_team.Group);
        Api.Players.GetNamesAsync(Arg.Any<IReadOnlyCollection<PlayerId>>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyDictionary<PlayerId, string>>(
                new Dictionary<PlayerId, string> { [_team.President] = "Marcelo", [_team.Regular] = "João" }));
        SignInAs(_team.Substitute1);

        var response = await Client.GetAsync($"/api/groups/{_team.Group.Id}");

        var group = await ReadAsync<GroupResponse>(response, HttpStatusCode.OK);
        group.Members.First(member => member.PlayerId == _team.President.Value).Name.ShouldBe("Marcelo");
        group.Members.First(member => member.PlayerId == _team.Regular.Value).Name.ShouldBe("João");
    }

    [Test]
    public async Task ListGroupLists_Member_ReturnsNewestFirstWithLineupAndMatch()
    {
        var played = _team.ClosedList();
        var lineup = _team.CompletedLineup(played);
        var match = Match.Record(played, lineup, goalsA: 3, goalsB: 1);
        var next = WeeklyList.Open(_team.Group, TestGroup.MatchDate.AddDays(7));
        Api.Groups.Returns(_team.Group);
        Api.Lists.ListRecentByGroupAsync(_team.Group.Id, ListGroupLists.MaxLists, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyList<WeeklyList>>([played, next]));
        Api.Lineups.ListByWeeklyListsAsync(Arg.Any<IReadOnlyCollection<WeeklyListId>>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyList<Lineup>>([lineup]));
        Api.Matches.ListByLineupsAsync(Arg.Any<IReadOnlyCollection<LineupId>>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyList<Match>>([match]));
        SignInAs(_team.Substitute3);

        var response = await Client.GetAsync($"/api/groups/{_team.Group.Id}/lists");

        var lists = await ReadAsync<List<WeeklyListSummaryResponse>>(response, HttpStatusCode.OK);
        lists.ShouldBe([
            new WeeklyListSummaryResponse(next.Id.Value, next.MatchDate, WeeklyListStatus.Open, null, null, null),
            new WeeklyListSummaryResponse(
                played.Id.Value, played.MatchDate, WeeklyListStatus.Closed, lineup.Id.Value, LineupStatus.Completed, match.Id.Value),
        ]);
    }

    [Test]
    public async Task ListGroupLists_NotAMember_Returns403()
    {
        Api.Groups.Returns(_team.Group);
        SignInAs(_team.Outsider);

        var response = await Client.GetAsync($"/api/groups/{_team.Group.Id}/lists");

        await ReadProblemAsync(response, HttpStatusCode.Forbidden);
    }

    [Test]
    public async Task ListGroupLists_GroupNotFound_Returns404()
    {
        SignInAs(_team.President);

        var response = await Client.GetAsync($"/api/groups/{Guid.NewGuid()}/lists");

        await ReadProblemAsync(response, HttpStatusCode.NotFound);
    }

    [Test]
    public async Task ListGroupSeasons_Member_ReturnsNewestFirst()
    {
        var first = Season.Create(_team.Group.Id, "2026/1", new DateOnly(2026, 1, 1));
        var second = Season.Create(_team.Group.Id, "2026/2", new DateOnly(2026, 7, 1));
        Api.Groups.Returns(_team.Group);
        Api.Seasons.ListByGroupAsync(_team.Group.Id, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyList<Season>>([first, second]));
        SignInAs(_team.Regular);

        var response = await Client.GetAsync($"/api/groups/{_team.Group.Id}/seasons");

        var seasons = await ReadAsync<List<SeasonResponse>>(response, HttpStatusCode.OK);
        seasons.Select(season => season.Name).ShouldBe(["2026/2", "2026/1"]);
    }

    [Test]
    public async Task ListGroupSeasons_NotAMember_Returns403()
    {
        Api.Groups.Returns(_team.Group);
        SignInAs(_team.Outsider);

        var response = await Client.GetAsync($"/api/groups/{_team.Group.Id}/seasons");

        await ReadProblemAsync(response, HttpStatusCode.Forbidden);
    }

    [Test]
    public async Task AddMemberByEmail_AccountWithProfile_AddsThePlayer()
    {
        await CreateAccountAsync("ze@example.com", _team.Outsider);
        Api.Groups.Returns(_team.Group);
        SignInAs(_team.President);

        var response = await PostAsync(
            $"/api/groups/{_team.Group.Id}/members/by-email", new { email = "ZE@example.com", type = "Substitute" });

        var group = await ReadAsync<GroupResponse>(response, HttpStatusCode.OK);
        group.Members.ShouldContain(member => member.PlayerId == _team.Outsider.Value && member.Type == PlayerType.Substitute);
        await Api.UnitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task AddMemberByEmail_AccountWithoutProfile_Returns404()
    {
        await CreateAccountAsync("semperfil@example.com", playerId: null);
        Api.Groups.Returns(_team.Group);
        SignInAs(_team.President);

        var response = await PostAsync(
            $"/api/groups/{_team.Group.Id}/members/by-email", new { email = "semperfil@example.com", type = "Regular" });

        await ReadProblemAsync(response, HttpStatusCode.NotFound);
    }

    [Test]
    public async Task AddMemberByEmail_NoAccount_Returns404()
    {
        Api.Groups.Returns(_team.Group);
        SignInAs(_team.President);

        var response = await PostAsync(
            $"/api/groups/{_team.Group.Id}/members/by-email", new { email = "ninguem@example.com", type = "Regular" });

        await ReadProblemAsync(response, HttpStatusCode.NotFound);
    }

    [Test]
    public async Task AddMemberByEmail_NotThePresident_Returns403EvenForUnknownEmail()
    {
        Api.Groups.Returns(_team.Group);
        SignInAs(_team.Regular);

        var response = await PostAsync(
            $"/api/groups/{_team.Group.Id}/members/by-email", new { email = "ninguem@example.com", type = "Regular" });

        // Not 404: only the president learns whether an e-mail has an account.
        await ReadProblemAsync(response, HttpStatusCode.Forbidden);
    }

    [Test]
    public async Task AddMemberByEmail_InvalidEmail_Returns400()
    {
        Api.Groups.Returns(_team.Group);
        SignInAs(_team.President);

        var response = await PostAsync(
            $"/api/groups/{_team.Group.Id}/members/by-email", new { email = "sem arroba", type = "Regular" });

        await ReadProblemAsync(response, HttpStatusCode.BadRequest);
    }

    private Task<int> CreateAccountAsync(string email, PlayerId? playerId) =>
        Api.WithAuthDbAsync(db =>
        {
            db.Users.Add(new AppUser
            {
                UserName = email,
                NormalizedUserName = email.ToUpperInvariant(),
                Email = email,
                NormalizedEmail = email.ToUpperInvariant(),
                EmailConfirmed = true,
                PlayerId = playerId?.Value,
            });
            return db.SaveChangesAsync();
        });
}
