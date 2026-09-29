namespace CadeAEscalacao.Domain.Jogadores;

/// <summary>
/// Posições em que um jogador atua. É um enum de flags porque o jogador
/// pode informar mais de uma posição (ex.: "meio ataque" = Meio | Ataque).
/// </summary>
[Flags]
public enum Posicao
{
    Nenhuma = 0,
    Goleiro = 1,
    Zaga = 2,
    Meio = 4,
    Ataque = 8,
}
