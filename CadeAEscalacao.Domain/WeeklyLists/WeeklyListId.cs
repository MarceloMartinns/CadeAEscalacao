namespace CadeAEscalacao.Domain.WeeklyLists;

public readonly record struct WeeklyListId(Guid Value)
{
    public static WeeklyListId New() => new(Guid.CreateVersion7());

    public override string ToString() => Value.ToString();
}
