namespace CadeAEscalacao.Domain.Lineups;

public enum LineupStatus
{
    /// <summary>The coach's partner is not playing (or the coach has no pair): the president picks one.</summary>
    WaitingForPartner = 1,

    /// <summary>The coach is assigning the other players to the two teams.</summary>
    Drafting = 2,

    /// <summary>Teams assigned; the coach's partner picks a team.</summary>
    WaitingForPartnerChoice = 3,

    /// <summary>Partner and coach are on their teams. The lineup is final.</summary>
    Completed = 4,
}
