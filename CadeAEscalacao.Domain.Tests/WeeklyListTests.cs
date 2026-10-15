using CadeAEscalacao.Domain.Common;
using CadeAEscalacao.Domain.Groups;
using CadeAEscalacao.Domain.Players;
using CadeAEscalacao.Domain.WeeklyLists;

namespace CadeAEscalacao.Domain.Tests;

public class WeeklyListTests
{
    private const Position AnyPosition = Position.Midfielder;
    private static readonly DateOnly MatchDate = new(2026, 10, 6);

    private readonly Dictionary<PlayerId, int> _noAttendance = new();
    private PlayerId _president;
    private Group _group = null!;

    [SetUp]
    public void SetUp()
    {
        _president = PlayerId.New();

        // 2 players per team, no bench = 4 spots: keeps the scenarios small.
        _group = Group.Create("Society de terça", new MatchFormat(2, 0), DayOfWeek.Tuesday, _president);
    }

    [Test]
    public void Open_Group_AddsRegularsAsPendingWithReservedSpot()
    {
        var regular = AddMember(PlayerType.Regular);
        AddMember(PlayerType.Substitute);

        var list = WeeklyList.Open(_group, MatchDate);

        list.GroupId.ShouldBe(_group.Id);
        list.MatchDate.ShouldBe(MatchDate);
        list.Capacity.ShouldBe(4);
        list.Status.ShouldBe(WeeklyListStatus.Open);
        Ids(list.Entries).ShouldBe(new[] { _president, regular });
        list.Entries.ShouldAllBe(entry => entry.Status == EntryStatus.Pending && entry.HasReservedSpot);
    }

    [Test]
    public void SignUp_PendingRegular_ConfirmsWithPositions()
    {
        var list = WeeklyList.Open(_group, MatchDate);

        list.SignUp(_group, _president, Position.Defender | Position.Midfielder);

        var entry = list.Entries.ShouldHaveSingleItem();
        entry.Status.ShouldBe(EntryStatus.Confirmed);
        entry.Positions.ShouldBe(Position.Defender | Position.Midfielder);
        entry.HasReservedSpot.ShouldBeTrue();
    }

    [Test]
    public void SignUp_Substitute_AddsConfirmedEntryWithoutReservedSpot()
    {
        var substitute = AddMember(PlayerType.Substitute);
        var list = WeeklyList.Open(_group, MatchDate);

        list.SignUp(_group, substitute, Position.Forward);

        var entry = list.Entries.Single(e => e.PlayerId == substitute);
        entry.Type.ShouldBe(PlayerType.Substitute);
        entry.Status.ShouldBe(EntryStatus.Confirmed);
        entry.HasReservedSpot.ShouldBeFalse();
    }

    [Test]
    public void SignUp_RegularWhoJoinedAfterOpening_GetsReservedSpot()
    {
        var list = WeeklyList.Open(_group, MatchDate);
        var newRegular = AddMember(PlayerType.Regular);

        list.SignUp(_group, newRegular, AnyPosition);

        list.Entries.Single(entry => entry.PlayerId == newRegular).HasReservedSpot.ShouldBeTrue();
    }

    [Test]
    public void SignUp_NotMember_Throws()
    {
        var list = WeeklyList.Open(_group, MatchDate);

        Should.Throw<DomainException>(() => list.SignUp(_group, PlayerId.New(), AnyPosition));
    }

    [Test]
    public void SignUp_GroupOfAnotherList_Throws()
    {
        var list = WeeklyList.Open(_group, MatchDate);
        var otherGroup = Group.Create("Futsal", MatchFormat.Futsal, DayOfWeek.Thursday, PlayerId.New());

        Should.Throw<DomainException>(() => list.SignUp(otherGroup, otherGroup.PresidentId, AnyPosition));
    }

    [Test]
    public void SignUp_NoPosition_Throws()
    {
        var list = WeeklyList.Open(_group, MatchDate);

        Should.Throw<ArgumentException>(() => list.SignUp(_group, _president, Position.None));
    }

    [Test]
    public void SignUp_AlreadyConfirmed_Throws()
    {
        var list = WeeklyList.Open(_group, MatchDate);
        list.SignUp(_group, _president, AnyPosition);

        Should.Throw<DomainException>(() => list.SignUp(_group, _president, AnyPosition));
    }

    [Test]
    public void SignUp_ClosedList_Throws()
    {
        var substitute = AddMember(PlayerType.Substitute);
        var list = WeeklyList.Open(_group, MatchDate);
        list.Close();

        Should.Throw<DomainException>(() => list.SignUp(_group, substitute, AnyPosition));
    }

    [Test]
    public void Withdraw_NotOnList_Throws()
    {
        var list = WeeklyList.Open(_group, MatchDate);

        Should.Throw<DomainException>(() => list.Withdraw(PlayerId.New()));
    }

    [Test]
    public void Withdraw_AlreadyDeclined_Throws()
    {
        var list = WeeklyList.Open(_group, MatchDate);
        list.Withdraw(_president);

        Should.Throw<DomainException>(() => list.Withdraw(_president));
    }

    [Test]
    public void ChangePositions_Confirmed_UpdatesPositions()
    {
        var list = WeeklyList.Open(_group, MatchDate);
        list.SignUp(_group, _president, Position.Defender);

        list.ChangePositions(_president, Position.Goalkeeper);

        list.Entries.ShouldHaveSingleItem().Positions.ShouldBe(Position.Goalkeeper);
    }

    [Test]
    public void ChangePositions_Pending_Throws()
    {
        var list = WeeklyList.Open(_group, MatchDate);

        Should.Throw<DomainException>(() => list.ChangePositions(_president, Position.Goalkeeper));
    }

    [Test]
    public void AllocateSlots_PendingRegulars_KeepTheirSpots()
    {
        // 3 regulars (president included) reserve 3 of the 4 spots.
        var regular1 = AddMember(PlayerType.Regular);
        var regular2 = AddMember(PlayerType.Regular);
        var substitute1 = AddMember(PlayerType.Substitute);
        var substitute2 = AddMember(PlayerType.Substitute);
        var list = WeeklyList.Open(_group, MatchDate);
        list.SignUp(_group, _president, AnyPosition);
        list.SignUp(_group, substitute1, AnyPosition);
        list.SignUp(_group, substitute2, AnyPosition);

        var allocation = list.AllocateSlots(_noAttendance);

        Ids(allocation.Playing).ShouldBe(new[] { _president, substitute1 });
        Ids(allocation.Pending).ShouldBe(new[] { regular1, regular2 });
        Ids(allocation.Waiting).ShouldBe(new[] { substitute2 });
        allocation.IsFull.ShouldBeTrue();
    }

    [Test]
    public void AllocateSlots_Substitutes_MostAttendancesFirst()
    {
        // Only the president is regular: 3 spots for 4 substitutes.
        var s1 = AddMember(PlayerType.Substitute);
        var s2 = AddMember(PlayerType.Substitute);
        var s3 = AddMember(PlayerType.Substitute);
        var s4 = AddMember(PlayerType.Substitute);
        var list = WeeklyList.Open(_group, MatchDate);
        list.SignUp(_group, _president, AnyPosition);
        list.SignUp(_group, s1, AnyPosition);
        list.SignUp(_group, s2, AnyPosition);
        list.SignUp(_group, s3, AnyPosition);
        list.SignUp(_group, s4, AnyPosition);
        var attendance = new Dictionary<PlayerId, int> { [s4] = 5, [s2] = 3 };

        var allocation = list.AllocateSlots(attendance);

        Ids(allocation.Playing).ShouldBe(new[] { _president, s4, s2, s1 });
        Ids(allocation.Waiting).ShouldBe(new[] { s3 });
    }

    [Test]
    public void AllocateSlots_SameAttendance_EarlierSignUpFirst()
    {
        AddMember(PlayerType.Regular);
        AddMember(PlayerType.Regular);
        var early = AddMember(PlayerType.Substitute);
        var late = AddMember(PlayerType.Substitute);
        var list = WeeklyList.Open(_group, MatchDate);
        list.SignUp(_group, early, AnyPosition);
        list.SignUp(_group, late, AnyPosition);
        var attendance = new Dictionary<PlayerId, int> { [early] = 2, [late] = 2 };

        var allocation = list.AllocateSlots(attendance);

        Ids(allocation.Waiting).ShouldBe(new[] { late });
    }

    [Test]
    public void Withdraw_Regular_ReleasesSpotForWaitingSubstitute()
    {
        var regular1 = AddMember(PlayerType.Regular);
        AddMember(PlayerType.Regular);
        var substitute1 = AddMember(PlayerType.Substitute);
        var substitute2 = AddMember(PlayerType.Substitute);
        var list = WeeklyList.Open(_group, MatchDate);
        list.SignUp(_group, substitute1, AnyPosition);
        list.SignUp(_group, substitute2, AnyPosition);

        list.Withdraw(regular1);

        var allocation = list.AllocateSlots(_noAttendance);
        Ids(allocation.Playing).ShouldBe(new[] { substitute1, substitute2 });
        allocation.Waiting.ShouldBeEmpty();
    }

    [Test]
    public void SignUp_RegularReturnsAfterDecliningAndListIsFull_Waits()
    {
        var regular = AddMember(PlayerType.Regular);
        AddMember(PlayerType.Regular);
        var substitute1 = AddMember(PlayerType.Substitute);
        var substitute2 = AddMember(PlayerType.Substitute);
        var list = WeeklyList.Open(_group, MatchDate);
        list.Withdraw(regular);
        list.SignUp(_group, substitute1, AnyPosition);
        list.SignUp(_group, substitute2, AnyPosition);

        list.SignUp(_group, regular, AnyPosition);

        var allocation = list.AllocateSlots(_noAttendance);
        Ids(allocation.Waiting).ShouldBe(new[] { regular });
        list.Entries.Single(entry => entry.PlayerId == regular).HasReservedSpot.ShouldBeFalse();
    }

    [Test]
    public void SignUp_RegularReturnsAfterDecliningAndThereIsASpot_Plays()
    {
        var regular = AddMember(PlayerType.Regular);
        var list = WeeklyList.Open(_group, MatchDate);
        list.Withdraw(regular);

        list.SignUp(_group, regular, AnyPosition);

        Ids(list.AllocateSlots(_noAttendance).Playing).ShouldContain(regular);
    }

    [Test]
    public void Close_PendingRegulars_LoseSpotAndWaitingSubstitutesPlay()
    {
        var regular1 = AddMember(PlayerType.Regular);
        var regular2 = AddMember(PlayerType.Regular);
        var substitute1 = AddMember(PlayerType.Substitute);
        var substitute2 = AddMember(PlayerType.Substitute);
        var list = WeeklyList.Open(_group, MatchDate);
        list.SignUp(_group, _president, AnyPosition);
        list.SignUp(_group, substitute1, AnyPosition);
        list.SignUp(_group, substitute2, AnyPosition);

        list.Close();

        list.Status.ShouldBe(WeeklyListStatus.Closed);
        var allocation = list.AllocateSlots(_noAttendance);
        Ids(allocation.Playing).ShouldBe(new[] { _president, substitute1, substitute2 });
        allocation.Pending.ShouldBeEmpty();
        list.Entries.Where(entry => entry.PlayerId == regular1 || entry.PlayerId == regular2)
            .ShouldAllBe(entry => entry.Status == EntryStatus.Declined);
    }

    [Test]
    public void Close_ClosedList_Throws()
    {
        var list = WeeklyList.Open(_group, MatchDate);
        list.Close();

        Should.Throw<DomainException>(() => list.Close());
    }

    [Test]
    public void Cancel_OpenList_SetsCancelled()
    {
        var list = WeeklyList.Open(_group, MatchDate);

        list.Cancel();

        list.Status.ShouldBe(WeeklyListStatus.Cancelled);
    }

    [Test]
    public void Cancel_AlreadyCancelled_Throws()
    {
        var list = WeeklyList.Open(_group, MatchDate);
        list.Cancel();

        Should.Throw<DomainException>(() => list.Cancel());
    }

    private PlayerId AddMember(PlayerType type)
    {
        var player = PlayerId.New();
        _group.AddMember(player, type);
        return player;
    }

    private static PlayerId[] Ids(IEnumerable<WeeklyListEntry> entries) =>
        entries.Select(entry => entry.PlayerId).ToArray();
}
