namespace CadeAEscalacao.Domain.Players;

public enum PlayerType
{
    /// <summary>Monthly member ("mensalista"): has a guaranteed spot.</summary>
    Regular = 1,

    /// <summary>Drop-in player ("suplente"): plays when there are open spots.</summary>
    Substitute = 2,
}
