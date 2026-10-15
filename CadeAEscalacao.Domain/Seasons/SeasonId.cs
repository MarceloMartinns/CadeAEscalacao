namespace CadeAEscalacao.Domain.Seasons;

public readonly record struct SeasonId(Guid Value)
{
    public static SeasonId New() => new(Guid.CreateVersion7());

    public override string ToString() => Value.ToString();
}
