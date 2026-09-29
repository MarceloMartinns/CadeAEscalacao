using CadeAEscalacao.Domain.Players;

namespace CadeAEscalacao.Domain.Tests;

public class PositionTests
{
    [Test]
    public void MidfielderForward_PlaysBothPositions()
    {
        var positions = Position.Midfielder | Position.Forward;

        positions.HasFlag(Position.Midfielder).ShouldBeTrue();
        positions.HasFlag(Position.Forward).ShouldBeTrue();
        positions.HasFlag(Position.Goalkeeper).ShouldBeFalse();
    }
}
