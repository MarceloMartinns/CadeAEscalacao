using CadeAEscalacao.Domain.Common;
using CadeAEscalacao.Domain.Groups;
using CadeAEscalacao.Domain.Lineups;
using CadeAEscalacao.Domain.Matches;
using CadeAEscalacao.Domain.Players;
using CadeAEscalacao.Domain.WeeklyLists;

namespace CadeAEscalacao.Domain.Tests;

public class MatchTests
{
    private const Position AnyPosition = Position.Midfielder;
    private static readonly DateOnly MatchDate = new(2026, 10, 6);

    private readonly Dictionary<PlayerId, int> _noAttendance = new();
    private Group _group = null!;
    private PlayerId _president;
    private PlayerId _partner;
    private PlayerId _p1;
    private PlayerId _p2;
    private WeeklyList _list = null!;
    private Lineup _lineup = null!;

    [SetUp]
    public void SetUp()
    {
        // 2 per team. The president coaches and the partner picks team A:
        // team A = p1 + partner, team B = p2 + president.
        _president = PlayerId.New();
        _group = Group.Create("Society de terça", new MatchFormat(2, 0), DayOfWeek.Tuesday, _president);
        _partner = AddRegular();
        _p1 = AddRegular();
        _p2 = AddRegular();
        _group.CreatePair(_president, _partner);

        _list = WeeklyList.Open(_group, MatchDate);
        foreach (var player in new[] { _president, _partner, _p1, _p2 })
        {
            _list.SignUp(_group, player, AnyPosition);
        }

        _list.Close();
        _lineup = Lineup.Start(_group, _list, _noAttendance);
    }

    [Test]
    public void Record_CompletedLineup_CopiesTeamsDateAndScore()
    {
        CompleteLineup();

        var match = Match.Record(_list, _lineup, 3, 1);

        match.GroupId.ShouldBe(_group.Id);
        match.LineupId.ShouldBe(_lineup.Id);
        match.Date.ShouldBe(MatchDate);
        match.GoalsA.ShouldBe(3);
        match.GoalsB.ShouldBe(1);
        match.TeamA.ShouldBe(new[] { _p1, _partner });
        match.TeamB.ShouldBe(new[] { _p2, _president });
    }

    [Test]
    public void Record_LineupNotCompleted_Throws()
    {
        Should.Throw<DomainException>(() => Match.Record(_list, _lineup, 1, 0));
    }

    [Test]
    public void Record_LineupOfAnotherList_Throws()
    {
        CompleteLineup();
        var otherList = WeeklyList.Open(_group, MatchDate.AddDays(7));

        Should.Throw<DomainException>(() => Match.Record(otherList, _lineup, 1, 0));
    }

    [Test]
    public void Record_CancelledList_Throws()
    {
        CompleteLineup();
        _list.Cancel();

        Should.Throw<DomainException>(() => Match.Record(_list, _lineup, 1, 0));
    }

    [TestCase(-1, 0)]
    [TestCase(0, -1)]
    public void Record_NegativeGoals_Throws(int goalsA, int goalsB)
    {
        CompleteLineup();

        Should.Throw<ArgumentOutOfRangeException>(() => Match.Record(_list, _lineup, goalsA, goalsB));
    }

    [TestCase(3, 1, MatchResult.Win, MatchResult.Loss)]
    [TestCase(0, 2, MatchResult.Loss, MatchResult.Win)]
    [TestCase(2, 2, MatchResult.Draw, MatchResult.Draw)]
    public void ResultOf_Team_ComparesGoals(int goalsA, int goalsB, MatchResult expectedA, MatchResult expectedB)
    {
        CompleteLineup();

        var match = Match.Record(_list, _lineup, goalsA, goalsB);

        match.ResultOf(Team.A).ShouldBe(expectedA);
        match.ResultOf(Team.B).ShouldBe(expectedB);
    }

    [Test]
    public void ResultOf_Player_UsesTheirTeam()
    {
        CompleteLineup();

        var match = Match.Record(_list, _lineup, 3, 1);

        match.ResultOf(_partner).ShouldBe(MatchResult.Win);
        match.ResultOf(_president).ShouldBe(MatchResult.Loss);
    }

    [Test]
    public void ResultOf_PlayerWhoDidNotPlay_ReturnsNull()
    {
        CompleteLineup();

        var match = Match.Record(_list, _lineup, 3, 1);

        match.ResultOf(PlayerId.New()).HasValue.ShouldBeFalse();
    }

    [Test]
    public void CorrectScore_NewScore_ChangesResult()
    {
        CompleteLineup();
        var match = Match.Record(_list, _lineup, 1, 0);

        match.CorrectScore(0, 1);

        match.ResultOf(Team.A).ShouldBe(MatchResult.Loss);
    }

    [Test]
    public void CorrectScore_NegativeGoals_Throws()
    {
        CompleteLineup();
        var match = Match.Record(_list, _lineup, 1, 0);

        Should.Throw<ArgumentOutOfRangeException>(() => match.CorrectScore(-1, 0));
    }

    private void CompleteLineup()
    {
        _lineup.AssignTeams([_p1], [_p2]);
        _lineup.PartnerChooses(Team.A);
    }

    private PlayerId AddRegular()
    {
        var player = PlayerId.New();
        _group.AddMember(player, PlayerType.Regular);
        return player;
    }
}
