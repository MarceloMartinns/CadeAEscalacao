using CadeAEscalacao.Domain.Common;
using CadeAEscalacao.Domain.Groups;
using CadeAEscalacao.Domain.Players;
using CadeAEscalacao.Domain.WeeklyLists;

namespace CadeAEscalacao.Domain.Lineups;

/// <summary>
/// The two teams of a match ("escalação"). Aggregate root.
/// </summary>
/// <remarks>
/// Flow:
/// <list type="number">
/// <item><see cref="Start"/>: after the list closes. The coach is the first player in the
/// group's rotation who is playing (<see cref="Group.FindCoach"/>).</item>
/// <item><see cref="SetPartnerOfTheDay"/>: only when the coach's partner is not playing
/// (or the coach has no pair). The president picks one of the players.</item>
/// <item><see cref="AssignTeams"/>: the coach splits everybody else, without themselves and their partner.</item>
/// <item><see cref="PartnerChooses"/>: the partner picks a team and the coach goes to the other one.
/// Then the application calls <see cref="Group.CompleteCoachTurn"/> (the coach goes to the end of the rotation).</item>
/// </list>
/// Playing with fewer players than the capacity is allowed: the president decides between
/// that and cancelling the list.
/// </remarks>
public sealed class Lineup
{
    // Not readonly: EF Core sets them when loading (stored as uuid[] columns, ADR-012).
    private List<PlayerId> _players = [];
    private List<PlayerId> _teamA = [];
    private List<PlayerId> _teamB = [];

    private Lineup(LineupId id, GroupId groupId, WeeklyListId weeklyListId, PlayerId coachId, PlayerId? partnerId)
    {
        Id = id;
        GroupId = groupId;
        WeeklyListId = weeklyListId;
        CoachId = coachId;
        PartnerId = partnerId;
        Status = partnerId is null ? LineupStatus.WaitingForPartner : LineupStatus.Drafting;
    }

    public LineupId Id { get; }

    public GroupId GroupId { get; }

    public WeeklyListId WeeklyListId { get; }

    public PlayerId CoachId { get; }

    /// <summary>The coach's pair, or the partner of the day picked by the president.</summary>
    public PlayerId? PartnerId { get; private set; }

    public LineupStatus Status { get; private set; }

    /// <summary>Everybody who plays this match (coach and partner included).</summary>
    public IReadOnlyList<PlayerId> Players => _players;

    public IReadOnlyList<PlayerId> TeamA => _teamA;

    public IReadOnlyList<PlayerId> TeamB => _teamB;

    /// <summary>
    /// Starts the lineup with the players who got a spot on the closed list.
    /// Season attendance is needed to know which substitutes got a spot (see <see cref="WeeklyList.AllocateSlots"/>).
    /// </summary>
    public static Lineup Start(Group group, WeeklyList list, IReadOnlyDictionary<PlayerId, int> seasonAttendance)
    {
        ArgumentNullException.ThrowIfNull(group);
        ArgumentNullException.ThrowIfNull(list);
        ArgumentNullException.ThrowIfNull(seasonAttendance);

        if (list.GroupId != group.Id)
        {
            throw new DomainException("This list belongs to another group.");
        }

        if (list.Status != WeeklyListStatus.Closed)
        {
            throw new DomainException("Close the list before making the lineup.");
        }

        var players = list.AllocateSlots(seasonAttendance).Playing.Select(entry => entry.PlayerId).ToList();

        if (players.Count < 2)
        {
            throw new DomainException("At least two players are needed for a lineup.");
        }

        var coachId = group.FindCoach(players)
            ?? throw new DomainException("Nobody from the coach rotation is playing.");

        // The pair only counts if the partner is playing; otherwise the president picks one.
        var partnerId = group.PartnerOf(coachId);
        if (partnerId is { } partner && !players.Contains(partner))
        {
            partnerId = null;
        }

        var lineup = new Lineup(LineupId.New(), group.Id, list.Id, coachId, partnerId);
        lineup._players.AddRange(players);
        return lineup;
    }

    /// <summary>
    /// The coach's partner is not playing (or the coach has no pair):
    /// the president picks one of the players as the partner for this match.
    /// </summary>
    public void SetPartnerOfTheDay(PlayerId partnerId)
    {
        if (Status != LineupStatus.WaitingForPartner)
        {
            throw new DomainException("The coach already has a partner for this match.");
        }

        if (partnerId == CoachId)
        {
            throw new DomainException("The coach cannot be their own partner.");
        }

        EnsurePlays(partnerId);

        PartnerId = partnerId;
        Status = LineupStatus.Drafting;
    }

    /// <summary>
    /// The coach splits every player except themselves and their partner.
    /// Teams must have the same size (one more at most, when the number is odd).
    /// The coach can redo it until the partner chooses.
    /// </summary>
    public void AssignTeams(IReadOnlyCollection<PlayerId> teamA, IReadOnlyCollection<PlayerId> teamB)
    {
        ArgumentNullException.ThrowIfNull(teamA);
        ArgumentNullException.ThrowIfNull(teamB);

        if (Status is not (LineupStatus.Drafting or LineupStatus.WaitingForPartnerChoice))
        {
            throw new DomainException("The teams cannot be assigned now.");
        }

        var toAssign = _players.Where(player => player != CoachId && player != PartnerId).ToHashSet();
        var assigned = teamA.Concat(teamB).ToList();

        // Same size + same elements = nobody twice, nobody missing, coach and partner left out.
        if (assigned.Count != toAssign.Count || !toAssign.SetEquals(assigned))
        {
            throw new DomainException(
                "Every player except the coach and their partner must be on exactly one team.");
        }

        if (Math.Abs(teamA.Count - teamB.Count) > 1)
        {
            throw new DomainException("The teams must have the same number of players (one more at most).");
        }

        _teamA.Clear();
        _teamA.AddRange(teamA);
        _teamB.Clear();
        _teamB.AddRange(teamB);
        Status = LineupStatus.WaitingForPartnerChoice;
    }

    /// <summary>
    /// The coach's partner picks a team and the coach goes to the other one. The lineup is final.
    /// </summary>
    public void PartnerChooses(Team team)
    {
        if (!Enum.IsDefined(team))
        {
            throw new ArgumentOutOfRangeException(nameof(team), team, "Invalid team.");
        }

        if (Status != LineupStatus.WaitingForPartnerChoice)
        {
            throw new DomainException("The coach must assign the teams first.");
        }

        var (partnerTeam, coachTeam) = team == Team.A ? (_teamA, _teamB) : (_teamB, _teamA);
        partnerTeam.Add(PartnerId!.Value);
        coachTeam.Add(CoachId);
        Status = LineupStatus.Completed;
    }

    /// <summary>The player's team, or null if the player is not on a team (yet).</summary>
    public Team? TeamOf(PlayerId playerId)
    {
        if (_teamA.Contains(playerId))
        {
            return Team.A;
        }

        if (_teamB.Contains(playerId))
        {
            return Team.B;
        }

        return null;
    }

    private void EnsurePlays(PlayerId playerId)
    {
        if (!_players.Contains(playerId))
        {
            throw new DomainException("The player is not playing this match.");
        }
    }
}
