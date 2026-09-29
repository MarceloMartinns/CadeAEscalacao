namespace CadeAEscalacao.Domain.Jogadores;

public enum TipoJogador
{
    /// <summary>Jogador fixo do grupo, com vaga garantida.</summary>
    Mensalista = 1,

    /// <summary>Jogador avulso, entra quando sobram vagas.</summary>
    Suplente = 2,
}
