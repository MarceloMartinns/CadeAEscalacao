using CadeAEscalacao.Domain.Common;
using CadeAEscalacao.Domain.Groups;
using CadeAEscalacao.Domain.Lineups;
using CadeAEscalacao.Domain.Players;
using CadeAEscalacao.Domain.WeeklyLists;

namespace CadeAEscalacao.Domain.Tests;

public class LineupTests
{
    private const Position AnyPosition = Position.Midfielder;
    private static readonly DateOnly MatchDate = new(2026, 10, 6);

    private readonly Dictionary<PlayerId, int> _noAttendance = new();
    private Group _group = null!;
    private PlayerId _president;
    private PlayerId _partner;
    private PlayerId _p1;
    private PlayerId _p2;
    private PlayerId _p3;
    private PlayerId _p4;

    [SetUp]
    public void SetUp()
    {
        // 3 players per team, no bench = 6 spots.
        // Rotation: president, partner, p1, p2, p3, p4. The president and the partner are a pair.
        _president = PlayerId.New();
        _group = Group.Create("Society de terça", new MatchFormat(3, 0), DayOfWeek.Tuesday, _president);
        _partner = AddRegular();
        _p1 = AddRegular();
        _p2 = AddRegular();
        _p3 = AddRegular();
        _p4 = AddRegular();
        _group.CreatePair(_president, _partner);
    }

    [Test]
    public void Start_CoachAndPartnerPlaying_StartsDrafting()
    {
        var lineup = StartWith(_president, _partner, _p1, _p2, _p3, _p4);

        lineup.GroupId.ShouldBe(_group.Id);
        lineup.CoachId.ShouldBe(_president);
        lineup.PartnerId.ShouldBe(_partner);
        lineup.Status.ShouldBe(LineupStatus.Drafting);
        lineup.Players.Count.ShouldBe(6);
    }

    [Test]
    public void Start_FirstInRotationAbsent_NextPlayingCoaches()
    {
        // The president is first in the rotation but did not come: the partner coaches.
        // The partner's pair (the president) is absent, so the president must pick a partner of the day.
        var lineup = StartWith(_partner, _p1, _p2, _p3, _p4);

        lineup.CoachId.ShouldBe(_partner);
        lineup.PartnerId.HasValue.ShouldBeFalse();
        lineup.Status.ShouldBe(LineupStatus.WaitingForPartner);
    }

    [Test]
    public void Start_CoachWithoutPair_WaitsForPartner()
    {
        _group.DissolvePair(_president);

        var lineup = StartWith(_president, _partner, _p1, _p2);

        lineup.Status.ShouldBe(LineupStatus.WaitingForPartner);
    }

    [Test]
    public void Start_ListNotClosed_Throws()
    {
        var list = WeeklyList.Open(_group, MatchDate);
        list.SignUp(_group, _president, AnyPosition);
        list.SignUp(_group, _partner, AnyPosition);

        Should.Throw<DomainException>(() => Lineup.Start(_group, list, _noAttendance));
    }

    [Test]
    public void Start_ListOfAnotherGroup_Throws()
    {
        var list = ClosedListWith(_president, _partner);
        var otherGroup = Group.Create("Futsal", MatchFormat.Futsal, DayOfWeek.Thursday, PlayerId.New());

        Should.Throw<DomainException>(() => Lineup.Start(otherGroup, list, _noAttendance));
    }

    [Test]
    public void Start_NobodyFromRotationPlaying_Throws()
    {
        var substitute1 = AddSubstitute();
        var substitute2 = AddSubstitute();
        var list = ClosedListWith(substitute1, substitute2);

        Should.Throw<DomainException>(() => Lineup.Start(_group, list, _noAttendance));
    }

    [Test]
    public void Start_OnlyOnePlayer_Throws()
    {
        var list = ClosedListWith(_president);

        Should.Throw<DomainException>(() => Lineup.Start(_group, list, _noAttendance));
    }

    [Test]
    public void SetPartnerOfTheDay_PlayerOnLineup_StartsDrafting()
    {
        var lineup = StartWith(_partner, _p1, _p2, _p3, _p4);

        lineup.SetPartnerOfTheDay(_p1);

        lineup.PartnerId.ShouldBe(_p1);
        lineup.Status.ShouldBe(LineupStatus.Drafting);
    }

    [Test]
    public void SetPartnerOfTheDay_Coach_Throws()
    {
        var lineup = StartWith(_partner, _p1, _p2, _p3, _p4);

        Should.Throw<DomainException>(() => lineup.SetPartnerOfTheDay(_partner));
    }

    [Test]
    public void SetPartnerOfTheDay_PlayerNotPlaying_Throws()
    {
        var lineup = StartWith(_partner, _p1, _p2, _p3);

        Should.Throw<DomainException>(() => lineup.SetPartnerOfTheDay(_p4));
    }

    [Test]
    public void SetPartnerOfTheDay_PairIsPlaying_Throws()
    {
        var lineup = StartWith(_president, _partner, _p1, _p2);

        Should.Throw<DomainException>(() => lineup.SetPartnerOfTheDay(_p1));
    }

    [Test]
    public void AssignTeams_EverybodyElse_WaitsForPartnerChoice()
    {
        var lineup = StartWith(_president, _partner, _p1, _p2, _p3, _p4);

        lineup.AssignTeams([_p1, _p2], [_p3, _p4]);

        lineup.TeamA.ShouldBe(new[] { _p1, _p2 });
        lineup.TeamB.ShouldBe(new[] { _p3, _p4 });
        lineup.Status.ShouldBe(LineupStatus.WaitingForPartnerChoice);
    }

    [Test]
    public void AssignTeams_AgainBeforePartnerChooses_ReplacesTeams()
    {
        var lineup = StartWith(_president, _partner, _p1, _p2, _p3, _p4);
        lineup.AssignTeams([_p1, _p2], [_p3, _p4]);

        lineup.AssignTeams([_p1, _p3], [_p2, _p4]);

        lineup.TeamA.ShouldBe(new[] { _p1, _p3 });
        lineup.TeamB.ShouldBe(new[] { _p2, _p4 });
    }

    [Test]
    public void AssignTeams_OddNumberOfPlayers_AllowsOneMore()
    {
        // Playing with fewer players: 3 to split besides the coach and the partner.
        var lineup = StartWith(_president, _partner, _p1, _p2, _p3);

        lineup.AssignTeams([_p1, _p2], [_p3]);

        lineup.Status.ShouldBe(LineupStatus.WaitingForPartnerChoice);
    }

    [Test]
    public void AssignTeams_IncludesCoach_Throws()
    {
        var lineup = StartWith(_president, _partner, _p1, _p2, _p3, _p4);

        Should.Throw<DomainException>(() => lineup.AssignTeams([_p1, _p2, _president], [_p3, _p4]));
    }

    [Test]
    public void AssignTeams_IncludesPartner_Throws()
    {
        var lineup = StartWith(_president, _partner, _p1, _p2, _p3, _p4);

        Should.Throw<DomainException>(() => lineup.AssignTeams([_p1, _p2], [_p3, _p4, _partner]));
    }

    [Test]
    public void AssignTeams_MissingPlayer_Throws()
    {
        var lineup = StartWith(_president, _partner, _p1, _p2, _p3, _p4);

        Should.Throw<DomainException>(() => lineup.AssignTeams([_p1, _p2], [_p3]));
    }

    [Test]
    public void AssignTeams_PlayerOnBothTeams_Throws()
    {
        var lineup = StartWith(_president, _partner, _p1, _p2, _p3, _p4);

        Should.Throw<DomainException>(() => lineup.AssignTeams([_p1, _p2, _p3], [_p3, _p4]));
    }

    [Test]
    public void AssignTeams_Unbalanced_Throws()
    {
        var lineup = StartWith(_president, _partner, _p1, _p2, _p3, _p4);

        Should.Throw<DomainException>(() => lineup.AssignTeams([_p1, _p2, _p3], [_p4]));
    }

    [Test]
    public void AssignTeams_WaitingForPartner_Throws()
    {
        var lineup = StartWith(_partner, _p1, _p2, _p3, _p4);

        Should.Throw<DomainException>(() => lineup.AssignTeams([_p1, _p2], [_p3, _p4]));
    }

    [Test]
    public void PartnerChooses_TeamB_PartnerJoinsBAndCoachJoinsA()
    {
        var lineup = StartWith(_president, _partner, _p1, _p2, _p3, _p4);
        lineup.AssignTeams([_p1, _p2], [_p3, _p4]);

        lineup.PartnerChooses(Team.B);

        lineup.TeamOf(_partner).ShouldBe(Team.B);
        lineup.TeamOf(_president).ShouldBe(Team.A);
        lineup.TeamA.Count.ShouldBe(3);
        lineup.TeamB.Count.ShouldBe(3);
        lineup.Status.ShouldBe(LineupStatus.Completed);
    }

    [Test]
    public void PartnerChooses_BeforeTeamsAssigned_Throws()
    {
        var lineup = StartWith(_president, _partner, _p1, _p2, _p3, _p4);

        Should.Throw<DomainException>(() => lineup.PartnerChooses(Team.A));
    }

    [Test]
    public void PartnerChooses_AfterCompleted_Throws()
    {
        var lineup = StartWith(_president, _partner, _p1, _p2, _p3, _p4);
        lineup.AssignTeams([_p1, _p2], [_p3, _p4]);
        lineup.PartnerChooses(Team.A);

        Should.Throw<DomainException>(() => lineup.PartnerChooses(Team.B));
    }

    [Test]
    public void TeamOf_PlayerNotOnTeamYet_ReturnsNull()
    {
        var lineup = StartWith(_president, _partner, _p1, _p2, _p3, _p4);

        lineup.TeamOf(_p1).HasValue.ShouldBeFalse();
    }

    [Test]
    public void FullFlow_PartnerOfTheDayAndCoachTurn_CoachGoesToEndOfRotation()
    {
        // Week without the president: the partner coaches and p1 is the partner of the day.
        var lineup = StartWith(_partner, _p1, _p2, _p3, _p4);
        lineup.SetPartnerOfTheDay(_p1);
        lineup.AssignTeams([_p2], [_p3, _p4]);
        lineup.PartnerChooses(Team.A);

        _group.CompleteCoachTurn(lineup.CoachId);

        lineup.TeamA.ShouldBe(new[] { _p2, _p1 });
        lineup.TeamB.ShouldBe(new[] { _p3, _p4, _partner });
        _group.CoachRotation.ShouldBe(new[] { _president, _p1, _p2, _p3, _p4, _partner });
    }

    private PlayerId AddRegular()
    {
        var player = PlayerId.New();
        _group.AddMember(player, PlayerType.Regular);
        return player;
    }

    private PlayerId AddSubstitute()
    {
        var player = PlayerId.New();
        _group.AddMember(player, PlayerType.Substitute);
        return player;
    }

    private WeeklyList ClosedListWith(params PlayerId[] players)
    {
        var list = WeeklyList.Open(_group, MatchDate);
        foreach (var player in players)
        {
            list.SignUp(_group, player, AnyPosition);
        }

        list.Close();
        return list;
    }

    private Lineup StartWith(params PlayerId[] players) =>
        Lineup.Start(_group, ClosedListWith(players), _noAttendance);
}
