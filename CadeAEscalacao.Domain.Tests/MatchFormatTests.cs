using CadeAEscalacao.Domain.Groups;

namespace CadeAEscalacao.Domain.Tests;

public class MatchFormatTests
{
    [Test]
    public void SevenASide_Has7PlayersAnd1BenchPlayerPerTeam()
    {
        var format = MatchFormat.SevenASide;

        format.PlayersPerTeam.ShouldBe(7);
        format.BenchPlayersPerTeam.ShouldBe(1);
        format.TotalSpots.ShouldBe(16);
    }

    [Test]
    public void Futsal_Has5PlayersAnd1BenchPlayerPerTeam()
    {
        var format = MatchFormat.Futsal;

        format.PlayersPerTeam.ShouldBe(5);
        format.BenchPlayersPerTeam.ShouldBe(1);
        format.TotalSpots.ShouldBe(12);
    }

    [TestCase(0, 1)]
    [TestCase(5, -1)]
    public void Constructor_InvalidValues_Throws(int players, int benchPlayers)
    {
        Should.Throw<ArgumentOutOfRangeException>(() => new MatchFormat(players, benchPlayers));
    }
}
