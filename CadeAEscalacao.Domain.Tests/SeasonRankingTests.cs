using CadeAEscalacao.Domain.Groups;
using CadeAEscalacao.Domain.Lineups;
using CadeAEscalacao.Domain.Matches;
using CadeAEscalacao.Domain.Players;
using CadeAEscalacao.Domain.Seasons;
using CadeAEscalacao.Domain.WeeklyLists;

namespace CadeAEscalacao.Domain.Tests;

public class SeasonRankingTests
{
    private static readonly DateOnly Week1 = new(2026, 10, 6);
    private static readonly DateOnly Week2 = new(2026, 10, 13);

    private readonly Dictionary<PlayerId, int> _noAttendance = new();
    private Group _group = null!;
    private Season _season = null!;
    private PlayerId _president;
    private PlayerId _partner;
    private PlayerId _p1;
    private PlayerId _p2;

    [SetUp]
    public void SetUp()
    {
        _president = PlayerId.New();
        _group = Group.Create("Society de terça", new MatchFormat(2, 0), DayOfWeek.Tuesday, _president);
        _partner = AddRegular();
        _p1 = AddRegular();
        _p2 = AddRegular();
        _group.CreatePair(_president, _partner);
        _season = Season.Create(_group.Id, "2026/2", new DateOnly(2026, 7, 1));
    }

    [Test]
    public void Calculate_NoMatches_ReturnsEmpty()
    {
        SeasonRanking.Calculate(_season, []).ShouldBeEmpty();
    }

    [Test]
    public void Calculate_WinAndDraw_SumsPointsAndAttendances()
    {
        // Everybody plays: team A = p1 + partner, team B = p2 + president.
        var matches = new[]
        {
            Play(Week1, 2, 1),
            Play(Week2, 1, 1),
        };

        var ranking = SeasonRanking.Calculate(_season, matches);

        var p1 = ranking.Single(entry => entry.PlayerId == _p1);
        p1.Points.ShouldBe(4);
        p1.Attendances.ShouldBe(2);
        p1.Wins.ShouldBe(1);
        p1.Draws.ShouldBe(1);
        p1.Losses.ShouldBe(0);

        var p2 = ranking.Single(entry => entry.PlayerId == _p2);
        p2.Points.ShouldBe(1);
        p2.Losses.ShouldBe(1);
    }

    [Test]
    public void Calculate_SamePoints_MoreAttendancesFirstAndFullTiesSharePosition()
    {
        // Week 1 without p2: team A = p1 + partner beats team B = president.
        // Week 2 without p1: team A = p2 + partner loses to team B = president.
        var matches = new[]
        {
            Play(Week1, 1, 0, absent: _p2),
            Play(Week2, 0, 1, absent: _p1),
        };

        var ranking = SeasonRanking.Calculate(_season, matches);

        // partner and president: 3 points, 2 attendances, 1 win (tied) · p1: 3 points, 1 attendance · p2: 0.
        ranking.Select(entry => entry.Position).ShouldBe(new[] { 1, 1, 3, 4 });
        ranking.Take(2).Select(entry => entry.PlayerId).ShouldBe(new[] { _partner, _president }, ignoreOrder: true);
        ranking[2].PlayerId.ShouldBe(_p1);
        ranking[3].PlayerId.ShouldBe(_p2);
    }

    [Test]
    public void Calculate_MatchOutsideSeason_IsIgnored()
    {
        var matches = new[] { Play(new DateOnly(2027, 1, 5), 1, 0) };

        SeasonRanking.Calculate(_season, matches).ShouldBeEmpty();
    }

    [Test]
    public void Calculate_MatchOfAnotherGroup_IsIgnored()
    {
        var otherSeason = Season.Create(GroupId.New(), "2026/2", new DateOnly(2026, 7, 1));

        SeasonRanking.Calculate(otherSeason, [Play(Week1, 1, 0)]).ShouldBeEmpty();
    }

    [Test]
    public void Attendances_ReturnsMatchesPlayedPerPlayer()
    {
        var matches = new[]
        {
            Play(Week1, 1, 0),
            Play(Week2, 1, 0, absent: _p2),
        };

        var attendances = SeasonRanking.Attendances(_season, matches);

        attendances[_p1].ShouldBe(2);
        attendances[_p2].ShouldBe(1);
    }

    /// <summary>
    /// Plays a match in which the president coaches and the partner picks team A.
    /// The other players are split between the teams in order.
    /// </summary>
    private Match Play(DateOnly date, int goalsA, int goalsB, params PlayerId[] absent)
    {
        var players = new[] { _president, _partner, _p1, _p2 }.Except(absent).ToArray();

        var list = WeeklyList.Open(_group, date);
        foreach (var player in players)
        {
            list.SignUp(_group, player, Position.Midfielder);
        }

        list.Close();

        var lineup = Lineup.Start(_group, list, _noAttendance);
        var others = players.Where(player => player != _president && player != _partner).ToArray();
        var half = (others.Length + 1) / 2;
        lineup.AssignTeams(others.Take(half).ToArray(), others.Skip(half).ToArray());
        lineup.PartnerChooses(Team.A);

        return Match.Record(list, lineup, goalsA, goalsB);
    }

    private PlayerId AddRegular()
    {
        var player = PlayerId.New();
        _group.AddMember(player, PlayerType.Regular);
        return player;
    }
}
