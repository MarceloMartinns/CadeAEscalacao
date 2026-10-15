namespace CadeAEscalacao.Domain.WeeklyLists;

public enum WeeklyListStatus
{
    /// <summary>Players can sign up and leave.</summary>
    Open = 1,

    /// <summary>The president closed the list; the coach can make the lineup.</summary>
    Closed = 2,

    /// <summary>The match will not happen (e.g. not enough players).</summary>
    Cancelled = 3,
}
