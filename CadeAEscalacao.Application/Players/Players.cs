using CadeAEscalacao.Application.Abstractions;
using CadeAEscalacao.Domain.Players;

namespace CadeAEscalacao.Application.Players;

// Minimal player registration, so the API can be used before the screens (etapa 8).

public sealed record PlayerResponse(Guid Id, string Name)
{
    public static PlayerResponse From(Player player) => new(player.Id.Value, player.Name);
}

public sealed class CreatePlayer(IPlayerRepository players, IUnitOfWork unitOfWork)
{
    public async Task<PlayerResponse> HandleAsync(string name, CancellationToken cancellationToken = default)
    {
        var player = Player.Create(name);
        players.Add(player);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return PlayerResponse.From(player);
    }
}

public sealed class GetPlayer(IPlayerRepository players)
{
    /// <summary>The player, or null if not found.</summary>
    public async Task<PlayerResponse?> HandleAsync(PlayerId id, CancellationToken cancellationToken = default) =>
        await players.GetByIdAsync(id, cancellationToken) is { } player ? PlayerResponse.From(player) : null;
}
