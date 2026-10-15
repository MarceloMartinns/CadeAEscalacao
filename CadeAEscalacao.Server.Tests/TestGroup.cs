using CadeAEscalacao.Application.Abstractions;
using CadeAEscalacao.Domain.Groups;
using CadeAEscalacao.Domain.Lineups;
using CadeAEscalacao.Domain.Players;
using CadeAEscalacao.Domain.WeeklyLists;

namespace CadeAEscalacao.Server.Tests;

/// <summary>
/// A small group for the tests: 2 players per team and no bench (4 spots on the list),
/// two regulars in a pair (the president coaches first) and three substitutes.
/// </summary>
internal sealed class TestGroup
{
    public static readonly DateOnly MatchDate = new(2026, 10, 20);

    public TestGroup()
    {
        Group = Group.Create("Terça", new MatchFormat(2, 0), DayOfWeek.Tuesday, President);
        Group.AddMember(Regular, PlayerType.Regular);
        Group.AddMember(Substitute1, PlayerType.Substitute);
        Group.AddMember(Substitute2, PlayerType.Substitute);
        Group.AddMember(Substitute3, PlayerType.Substitute);
        Group.CreatePair(President, Regular);
    }

    public Group Group { get; }

    public PlayerId President { get; } = PlayerId.New();

    public PlayerId Regular { get; } = PlayerId.New();

    public PlayerId Substitute1 { get; } = PlayerId.New();

    public PlayerId Substitute2 { get; } = PlayerId.New();

    public PlayerId Substitute3 { get; } = PlayerId.New();

    public WeeklyList OpenList() => WeeklyList.Open(Group, MatchDate);

    /// <summary>Both regulars and two substitutes confirmed (the list is full), then closed.</summary>
    public WeeklyList ClosedList()
    {
        var list = OpenList();
        foreach (var playerId in new[] { President, Regular, Substitute1, Substitute2 })
        {
            list.SignUp(Group, playerId, Position.Midfielder);
        }

        list.Close();
        return list;
    }

    /// <summary>Coach: president. Partner: regular. The coach split the substitutes; the partner did not choose yet.</summary>
    public Lineup LineupWithTeams(WeeklyList closedList)
    {
        var lineup = Lineup.Start(Group, closedList, new Dictionary<PlayerId, int>());
        lineup.AssignTeams([Substitute1], [Substitute2]);
        return lineup;
    }

    /// <summary>Team A: substitute 1 and the regular (partner). Team B: substitute 2 and the president (coach).</summary>
    public Lineup CompletedLineup(WeeklyList closedList)
    {
        var lineup = LineupWithTeams(closedList);
        lineup.PartnerChooses(Team.A);
        return lineup;
    }
}

/// <summary>Short ways to make the mocked repositories return an aggregate.</summary>
internal static class RepositoryMocks
{
    public static void Returns(this IGroupRepository groups, Group group)
    {
        groups.GetByIdAsync(group.Id, Arg.Any<CancellationToken>()).Returns(Task.FromResult<Group?>(group));
        groups.ExistsAsync(group.Id, Arg.Any<CancellationToken>()).Returns(true);
    }

    public static void Returns(this IWeeklyListRepository lists, WeeklyList list) =>
        lists.GetByIdAsync(list.Id, Arg.Any<CancellationToken>()).Returns(Task.FromResult<WeeklyList?>(list));

    public static void Returns(this ILineupRepository lineups, Lineup lineup) =>
        lineups.GetByIdAsync(lineup.Id, Arg.Any<CancellationToken>()).Returns(Task.FromResult<Lineup?>(lineup));
}
