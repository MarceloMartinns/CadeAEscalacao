namespace CadeAEscalacao.Domain.Players;

/// <summary>Strongly typed id: a PlayerId cannot be passed where a GroupId is expected.</summary>
public readonly record struct PlayerId(Guid Value)
{
    public static PlayerId New() => new(Guid.CreateVersion7());

    public override string ToString() => Value.ToString();
}
