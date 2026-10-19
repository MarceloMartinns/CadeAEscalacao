using CadeAEscalacao.Domain.Players;

namespace CadeAEscalacao.Application.Abstractions;

/// <summary>
/// The signed-in person who is doing the action (ADR-017). Implemented by the API from the
/// access token; the endpoints that use it only accept people who already have a player.
/// </summary>
public interface ICurrentUser
{
    PlayerId PlayerId { get; }
}
