namespace CadeAEscalacao.Domain.Matches;

public readonly record struct MatchId(Guid Value)
{
    public static MatchId New() => new(Guid.CreateVersion7());

    public override string ToString() => Value.ToString();
}
