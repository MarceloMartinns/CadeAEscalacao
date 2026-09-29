namespace CadeAEscalacao.Domain.Players;

/// <summary>
/// Positions a player can play. It is a flags enum because a player
/// may list more than one position (e.g. "meio ataque" = Midfielder | Forward).
/// </summary>
[Flags]
public enum Position
{
    None = 0,
    Goalkeeper = 1,
    Defender = 2,
    Midfielder = 4,
    Forward = 8,
}
