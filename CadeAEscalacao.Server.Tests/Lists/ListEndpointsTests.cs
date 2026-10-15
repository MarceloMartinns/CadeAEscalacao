using System.Net;
using CadeAEscalacao.Application.Lists;
using CadeAEscalacao.Domain.Players;
using CadeAEscalacao.Domain.WeeklyLists;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using NSubstitute.ExceptionExtensions;

namespace CadeAEscalacao.Server.Tests.Lists;

public sealed class ListEndpointsTests : ApiTestBase
{
    private readonly TestGroup _team = new();

    [Test]
    public async Task OpenList_ValidRequest_Returns201WithRegularsPending()
    {
        Api.Groups.Returns(_team.Group);

        var response = await PostAsync($"/api/groups/{_team.Group.Id}/lists", new { matchDate = TestGroup.MatchDate });

        var list = await ReadAsync<WeeklyListResponse>(response, HttpStatusCode.Created);
        response.Headers.Location!.OriginalString.ShouldBe($"/api/lists/{list.Id}");
        list.Status.ShouldBe(WeeklyListStatus.Open);
        list.Capacity.ShouldBe(4);
        list.Pending.Select(entry => entry.PlayerId).ShouldBe([_team.President.Value, _team.Regular.Value], ignoreOrder: true);
        Api.Lists.Received(1).Add(Arg.Is<WeeklyList>(added => added.Id.Value == list.Id));
        await Api.UnitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task OpenList_GroupNotFound_Returns404()
    {
        var response = await PostAsync($"/api/groups/{Guid.NewGuid()}/lists", new { matchDate = TestGroup.MatchDate });

        var problem = await ReadProblemAsync(response, HttpStatusCode.NotFound);
        problem.Detail!.ShouldStartWith("Group");
    }

    [Test]
    public async Task OpenList_ListExistsForDate_Returns409()
    {
        Api.Groups.Returns(_team.Group);
        Api.Lists.ExistsAsync(_team.Group.Id, TestGroup.MatchDate, Arg.Any<CancellationToken>()).Returns(true);

        var response = await PostAsync($"/api/groups/{_team.Group.Id}/lists", new { matchDate = TestGroup.MatchDate });

        await ReadProblemAsync(response, HttpStatusCode.Conflict);
        await Api.UnitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task OpenList_MissingDate_Returns400WithValidationErrors()
    {
        var response = await PostAsync($"/api/groups/{_team.Group.Id}/lists", new { });

        var problem = await ReadAsync<HttpValidationProblemDetails>(response, HttpStatusCode.BadRequest);
        problem.Errors.Keys.ShouldContain(key => key.Contains("MatchDate", StringComparison.OrdinalIgnoreCase));
    }

    [Test]
    public async Task GetList_NotFound_Returns404()
    {
        var response = await Client.GetAsync($"/api/lists/{Guid.NewGuid()}");

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Test]
    public async Task GetList_MoreSubstitutesThanSpots_LastOneWaits()
    {
        var list = _team.OpenList();
        list.SignUp(_team.Group, _team.Substitute1, Position.Forward);
        list.SignUp(_team.Group, _team.Substitute2, Position.Forward);
        list.SignUp(_team.Group, _team.Substitute3, Position.Forward);
        Api.Lists.Returns(list);

        var response = await Client.GetAsync($"/api/lists/{list.Id}");

        // 4 spots: 2 held for the regulars (pending), 2 for the first substitutes.
        var body = await ReadAsync<WeeklyListResponse>(response, HttpStatusCode.OK);
        body.Playing.Select(entry => entry.PlayerId).ShouldBe([_team.Substitute1.Value, _team.Substitute2.Value]);
        body.Waiting.Single().PlayerId.ShouldBe(_team.Substitute3.Value);
        body.OpenSpots.ShouldBe(0);
    }

    [Test]
    public async Task SignUp_Substitute_ReturnsPlayingWithPositionsAndSaves()
    {
        var list = _team.OpenList();
        Api.Lists.Returns(list);
        Api.Groups.Returns(_team.Group);

        var response = await PostAsync(
            $"/api/lists/{list.Id}/entries",
            new { playerId = _team.Substitute1.Value, positions = new[] { "Midfielder", "Forward" } });

        var body = await ReadAsync<WeeklyListResponse>(response, HttpStatusCode.OK);
        var entry = body.Playing.Single();
        entry.PlayerId.ShouldBe(_team.Substitute1.Value);
        entry.Positions.ShouldBe([Position.Midfielder, Position.Forward]);
        await Api.UnitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task SignUp_NotAMember_Returns422()
    {
        var list = _team.OpenList();
        Api.Lists.Returns(list);
        Api.Groups.Returns(_team.Group);

        var response = await PostAsync(
            $"/api/lists/{list.Id}/entries",
            new { playerId = Guid.NewGuid(), positions = new[] { "Defender" } });

        var problem = await ReadProblemAsync(response, HttpStatusCode.UnprocessableEntity);
        problem.Detail.ShouldBe("The player is not a member of this group.");
        await Api.UnitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task SignUp_NoPositions_Returns400WithValidationErrors()
    {
        var response = await PostAsync(
            $"/api/lists/{Guid.NewGuid()}/entries",
            new { playerId = _team.Substitute1.Value, positions = Array.Empty<string>() });

        var problem = await ReadAsync<HttpValidationProblemDetails>(response, HttpStatusCode.BadRequest);
        problem.Errors.Keys.ShouldContain(key => key.Contains("Positions", StringComparison.OrdinalIgnoreCase));
    }

    [Test]
    public async Task SignUp_SomeoneElseChangedTheList_Returns409()
    {
        var list = _team.OpenList();
        Api.Lists.Returns(list);
        Api.Groups.Returns(_team.Group);
        Api.UnitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>()).ThrowsAsync(new DbUpdateConcurrencyException());

        var response = await PostAsync(
            $"/api/lists/{list.Id}/entries",
            new { playerId = _team.Substitute1.Value, positions = new[] { "Goalkeeper" } });

        var problem = await ReadProblemAsync(response, HttpStatusCode.Conflict);
        problem.Title.ShouldBe("Concurrent update");
    }

    [Test]
    public async Task Withdraw_Regular_MovesToDeclined()
    {
        var list = _team.OpenList();
        Api.Lists.Returns(list);

        var response = await Client.DeleteAsync($"/api/lists/{list.Id}/entries/{_team.Regular}");

        var body = await ReadAsync<WeeklyListResponse>(response, HttpStatusCode.OK);
        body.Declined.Single().PlayerId.ShouldBe(_team.Regular.Value);
        body.Pending.ShouldNotContain(entry => entry.PlayerId == _team.Regular.Value);
    }

    [Test]
    public async Task Close_RegularDidNotAnswer_ClosesAndRegularLosesSpot()
    {
        var list = _team.OpenList();
        list.SignUp(_team.Group, _team.President, Position.Defender);
        Api.Lists.Returns(list);

        var response = await PostAsync($"/api/lists/{list.Id}/close");

        var body = await ReadAsync<WeeklyListResponse>(response, HttpStatusCode.OK);
        body.Status.ShouldBe(WeeklyListStatus.Closed);
        body.Declined.Single().PlayerId.ShouldBe(_team.Regular.Value);
    }

    [Test]
    public async Task Cancel_AlreadyCancelled_Returns422()
    {
        var list = _team.OpenList();
        list.Cancel();
        Api.Lists.Returns(list);

        var response = await PostAsync($"/api/lists/{list.Id}/cancel");

        await ReadProblemAsync(response, HttpStatusCode.UnprocessableEntity);
    }
}
