using CadeAEscalacao.Domain.Common;
using CadeAEscalacao.Domain.Groups;
using CadeAEscalacao.Domain.Players;

namespace CadeAEscalacao.Domain.Tests;

public class GroupTests
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
    public void Create_ValidData_AddsPresidentAsRegularMember()
    {
        _group.Name.ShouldBe("Society de terça");
        _group.Format.ShouldBe(MatchFormat.SevenASide);
        _group.MatchDay.ShouldBe(DayOfWeek.Tuesday);
        _group.PresidentId.ShouldBe(_president);
        _group.Members.ShouldHaveSingleItem().ShouldBe(new GroupMember(_president, PlayerType.Regular));
    }

    [TestCase("")]
    [TestCase("   ")]
    public void Create_EmptyName_Throws(string name)
    {
        Should.Throw<ArgumentException>(
            () => Group.Create(name, MatchFormat.Futsal, DayOfWeek.Thursday, PlayerId.New()));
    }

    [Test]
    public void AddMember_NewPlayer_AddsWithType()
    {
        var player = PlayerId.New();

        _group.AddMember(player, PlayerType.Substitute);

        _group.GetMember(player).Type.ShouldBe(PlayerType.Substitute);
    }

    [Test]
    public void AddMember_AlreadyMember_Throws()
    {
        var player = AddMember();

        Should.Throw<DomainException>(() => _group.AddMember(player, PlayerType.Regular));
    }

    [Test]
    public void GetMember_NotMember_Throws()
    {
        Should.Throw<DomainException>(() => _group.GetMember(PlayerId.New()));
    }

    [Test]
    public void ChangeMemberType_Member_UpdatesType()
    {
        var player = AddMember(PlayerType.Substitute);

        _group.ChangeMemberType(player, PlayerType.Regular);

        _group.GetMember(player).IsRegular.ShouldBeTrue();
    }

    [Test]
    public void ChangeMemberType_PresidentToSubstitute_Throws()
    {
        Should.Throw<DomainException>(() => _group.ChangeMemberType(_president, PlayerType.Substitute));
    }

    [Test]
    public void RemoveMember_PlayerInPair_RemovesMemberAndDissolvesPair()
    {
        var player = AddMember();
        var partner = AddMember();
        _group.CreatePair(player, partner);

        _group.RemoveMember(player);

        _group.IsMember(player).ShouldBeFalse();
        _group.Pairs.ShouldBeEmpty();
        _group.PartnerOf(partner).HasValue.ShouldBeFalse();
    }

    [Test]
    public void RemoveMember_President_Throws()
    {
        Should.Throw<DomainException>(() => _group.RemoveMember(_president));
    }

    [Test]
    public void RemoveMember_NotMember_Throws()
    {
        Should.Throw<DomainException>(() => _group.RemoveMember(PlayerId.New()));
    }

    [Test]
    public void TransferPresidency_ToRegular_ChangesPresident()
    {
        var newPresident = AddMember();

        _group.TransferPresidency(newPresident);

        _group.PresidentId.ShouldBe(newPresident);
    }

    [Test]
    public void TransferPresidency_ToSubstitute_Throws()
    {
        var substitute = AddMember(PlayerType.Substitute);

        Should.Throw<DomainException>(() => _group.TransferPresidency(substitute));
    }

    [Test]
    public void CreatePair_TwoMembersWithoutPair_CreatesPair()
    {
        var first = AddMember();
        var second = AddMember();

        _group.CreatePair(first, second);

        _group.PartnerOf(first).ShouldBe(second);
        _group.PartnerOf(second).ShouldBe(first);
    }

    [Test]
    public void CreatePair_PlayerAlreadyInPair_Throws()
    {
        var first = AddMember();
        var second = AddMember();
        var third = AddMember();
        _group.CreatePair(first, second);

        Should.Throw<DomainException>(() => _group.CreatePair(third, second));
    }

    [Test]
    public void CreatePair_NotMember_Throws()
    {
        var member = AddMember();

        Should.Throw<DomainException>(() => _group.CreatePair(member, PlayerId.New()));
    }

    [Test]
    public void CreatePair_SamePlayerTwice_Throws()
    {
        var member = AddMember();

        Should.Throw<DomainException>(() => _group.CreatePair(member, member));
    }

    [Test]
    public void DissolvePair_PlayerInPair_RemovesPair()
    {
        var first = AddMember();
        var second = AddMember();
        _group.CreatePair(first, second);

        _group.DissolvePair(second);

        _group.Pairs.ShouldBeEmpty();
    }

    [Test]
    public void DissolvePair_PlayerWithoutPair_Throws()
    {
        var member = AddMember();

        Should.Throw<DomainException>(() => _group.DissolvePair(member));
    }

    [Test]
    public void ChangeFormat_Futsal_UpdatesFormat()
    {
        _group.ChangeFormat(MatchFormat.Futsal);

        _group.Format.TotalSpots.ShouldBe(12);
    }

    private PlayerId AddMember(PlayerType type = PlayerType.Regular)
    {
        var player = PlayerId.New();
        _group.AddMember(player, type);
        return player;
    }
}
