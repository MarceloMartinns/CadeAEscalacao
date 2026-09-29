using CadeAEscalacao.Domain.Matches;

namespace CadeAEscalacao.Domain.Tests;

public class ScoringTests
{
    [TestCase(MatchResult.Win, 3)]
    [TestCase(MatchResult.Draw, 1)]
    [TestCase(MatchResult.Loss, 0)]
    public void Calculate_ReturnsPointsForResult(MatchResult result, int expectedPoints)
    {
        Scoring.Calculate(result).ShouldBe(expectedPoints);
    }

    [Test]
    public void Calculate_InvalidResult_Throws()
    {
        Should.Throw<ArgumentOutOfRangeException>(() => Scoring.Calculate((MatchResult)99));
    }
}
