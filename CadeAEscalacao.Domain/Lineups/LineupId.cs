namespace CadeAEscalacao.Domain.Lineups;

public readonly record struct LineupId(Guid Value)
{
    public static LineupId New() => new(Guid.CreateVersion7());

    public override string ToString() => Value.ToString();
}
