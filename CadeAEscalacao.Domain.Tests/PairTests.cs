using CadeAEscalacao.Domain.Common;
using CadeAEscalacao.Domain.Groups;
using CadeAEscalacao.Domain.Players;

namespace CadeAEscalacao.Domain.Tests;

public class PairTests
{
    private readonly PlayerId _first = PlayerId.New();
    private readonly PlayerId _second = PlayerId.New();

    [Test]
    public void Constructor_SamePlayerTwice_Throws()
    {
        Should.Throw<DomainException>(() => new Pair(_first, _first));
    }

    [Test]
    public void PartnerOf_PlayerInPair_ReturnsTheOtherPlayer()
    {
        var pair = new Pair(_first, _second);

        pair.PartnerOf(_first).ShouldBe(_second);
        pair.PartnerOf(_second).ShouldBe(_first);
    }

    [Test]
    public void PartnerOf_PlayerOutsidePair_Throws()
    {
        var pair = new Pair(_first, _second);

        Should.Throw<ArgumentException>(() => pair.PartnerOf(PlayerId.New()));
    }

    [Test]
    public void Contains_ChecksBothPlayers()
    {
        var pair = new Pair(_first, _second);

        pair.Contains(_first).ShouldBeTrue();
        pair.Contains(_second).ShouldBeTrue();
        pair.Contains(PlayerId.New()).ShouldBeFalse();
    }
}
