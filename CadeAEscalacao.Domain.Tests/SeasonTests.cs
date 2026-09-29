using CadeAEscalacao.Domain.Seasons;

namespace CadeAEscalacao.Domain.Tests;

public class SeasonTests
{
    [Test]
    public void Create_WithoutDuration_Uses6Months()
    {
        var season = Season.Create("2026/2", new DateOnly(2026, 7, 1));

        season.End.ShouldBe(new DateOnly(2026, 12, 31));
    }

    [Test]
    public void Create_WithCustomDuration_CalculatesEnd()
    {
        var season = Season.Create("Short", new DateOnly(2026, 1, 1), durationInMonths: 3);

        season.End.ShouldBe(new DateOnly(2026, 3, 31));
    }

    [TestCase(2026, 7, 1, true)]
    [TestCase(2026, 12, 31, true)]
    [TestCase(2026, 6, 30, false)]
    [TestCase(2027, 1, 1, false)]
    public void Contains_ChecksInclusiveBoundaries(int year, int month, int day, bool expected)
    {
        var season = Season.Create("2026/2", new DateOnly(2026, 7, 1));

        season.Contains(new DateOnly(year, month, day)).ShouldBe(expected);
    }

    [Test]
    public void Create_ZeroDuration_Throws()
    {
        Should.Throw<ArgumentOutOfRangeException>(
            () => Season.Create("Invalid", new DateOnly(2026, 1, 1), durationInMonths: 0));
    }

    [TestCase("")]
    [TestCase("   ")]
    public void Create_EmptyName_Throws(string name)
    {
        Should.Throw<ArgumentException>(() => Season.Create(name, new DateOnly(2026, 1, 1)));
    }
}
