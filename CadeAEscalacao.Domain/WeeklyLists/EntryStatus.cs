namespace CadeAEscalacao.Domain.WeeklyLists;

public enum EntryStatus
{
    /// <summary>Regular who has not answered yet. The spot stays reserved.</summary>
    Pending = 1,

    /// <summary>Will play (or is waiting for a spot, if a substitute).</summary>
    Confirmed = 2,

    /// <summary>Declined or left the list.</summary>
    Declined = 3,
}
