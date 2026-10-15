using CadeAEscalacao.Domain.Common;
using CadeAEscalacao.Domain.Groups;
using CadeAEscalacao.Domain.Players;

namespace CadeAEscalacao.Domain.Tests;

/// <summary>Coach rotation ("rodízio de professores") rules of <see cref="Group"/>.</summary>
public class GroupCoachRotationTests
{
    private PlayerId _president;
    private Group _group = null!;

    [SetUp]
    public void SetUp()
    {
        _president = PlayerId.New();
        _group = Group.Create("Society de terça", MatchFormat.SevenASide, DayOfWeek.Tuesday, _president);
    }

    [Test]
    public void Create_NewGroup_PresidentIsInRotation()
    {
        _group.CoachRotation.ShouldHaveSingleItem().ShouldBe(_president);
    }

    [Test]
    public void AddMember_Regular_GoesToEndOfRotation()
    {
        var regular = AddMember(PlayerType.Regular);

        _group.CoachRotation.ShouldBe(new[] { _president, regular });
    }

    [Test]
    public void AddMember_Substitute_IsNotInRotation()
    {
        AddMember(PlayerType.Substitute);

        _group.CoachRotation.ShouldHaveSingleItem().ShouldBe(_president);
    }

    [Test]
    public void ChangeMemberType_RegularToSubstitute_LeavesRotation()
    {
        var player = AddMember(PlayerType.Regular);

        _group.ChangeMemberType(player, PlayerType.Substitute);

        _group.CoachRotation.ShouldNotContain(player);
    }

    [Test]
    public void ChangeMemberType_SubstituteToRegular_GoesToEndOfRotation()
    {
        var player = AddMember(PlayerType.Substitute);
        var regular = AddMember(PlayerType.Regular);

        _group.ChangeMemberType(player, PlayerType.Regular);

        _group.CoachRotation.ShouldBe(new[] { _president, regular, player });
    }

    [Test]
    public void ChangeMemberType_RegularToRegular_DoesNotDuplicate()
    {
        var player = AddMember(PlayerType.Regular);

        _group.ChangeMemberType(player, PlayerType.Regular);

        _group.CoachRotation.ShouldBe(new[] { _president, player });
    }

    [Test]
    public void RemoveMember_Regular_LeavesRotation()
    {
        var player = AddMember(PlayerType.Regular);

        _group.RemoveMember(player);

        _group.CoachRotation.ShouldNotContain(player);
    }

    [Test]
    public void ReorderCoachRotation_AllRegulars_SetsOrder()
    {
        var a = AddMember(PlayerType.Regular);
        var b = AddMember(PlayerType.Regular);

        _group.ReorderCoachRotation([b, _president, a]);

        _group.CoachRotation.ShouldBe(new[] { b, _president, a });
    }

    [Test]
    public void ReorderCoachRotation_MissingRegular_Throws()
    {
        var a = AddMember(PlayerType.Regular);
        AddMember(PlayerType.Regular);

        Should.Throw<DomainException>(() => _group.ReorderCoachRotation([_president, a]));
    }

    [Test]
    public void ReorderCoachRotation_DuplicatePlayer_Throws()
    {
        var a = AddMember(PlayerType.Regular);

        Should.Throw<DomainException>(() => _group.ReorderCoachRotation([_president, a, a]));
    }

    [Test]
    public void ReorderCoachRotation_Substitute_Throws()
    {
        var a = AddMember(PlayerType.Regular);
        var substitute = AddMember(PlayerType.Substitute);

        Should.Throw<DomainException>(() => _group.ReorderCoachRotation([_president, a, substitute]));
    }

    [Test]
    public void FindCoach_FirstInRotationPresent_ReturnsFirst()
    {
        var a = AddMember(PlayerType.Regular);

        _group.FindCoach([a, _president]).ShouldBe(_president);
    }

    [Test]
    public void FindCoach_FirstInRotationAbsent_ReturnsNextPresent()
    {
        var a = AddMember(PlayerType.Regular);
        var b = AddMember(PlayerType.Regular);

        _group.FindCoach([b, a]).ShouldBe(a);
    }

    [Test]
    public void FindCoach_NobodyFromRotationPresent_ReturnsNull()
    {
        var substitute = AddMember(PlayerType.Substitute);

        _group.FindCoach([substitute]).HasValue.ShouldBeFalse();
    }

    [Test]
    public void CompleteCoachTurn_Coach_MovesToEnd()
    {
        var a = AddMember(PlayerType.Regular);
        var b = AddMember(PlayerType.Regular);

        _group.CompleteCoachTurn(_president);

        _group.CoachRotation.ShouldBe(new[] { a, b, _president });
    }

    [Test]
    public void CompleteCoachTurn_SkippedPlayer_KeepsPlaceAndCoachesNextTime()
    {
        var a = AddMember(PlayerType.Regular);
        var b = AddMember(PlayerType.Regular);

        // Week 1: the president (first in the queue) is absent, so "a" coaches.
        var week1Coach = _group.FindCoach([a, b]);
        _group.CompleteCoachTurn(week1Coach!.Value);

        // Week 2: the president is back and is still first in the queue.
        _group.CoachRotation.ShouldBe(new[] { _president, b, a });
        _group.FindCoach([_president, a, b]).ShouldBe(_president);
    }

    [Test]
    public void CompleteCoachTurn_NotInRotation_Throws()
    {
        var substitute = AddMember(PlayerType.Substitute);

        Should.Throw<DomainException>(() => _group.CompleteCoachTurn(substitute));
    }

    private PlayerId AddMember(PlayerType type)
    {
        var player = PlayerId.New();
        _group.AddMember(player, type);
        return player;
    }
}
