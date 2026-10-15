using CadeAEscalacao.Domain.Players;

namespace CadeAEscalacao.Domain.Groups;

/// <summary>A player's membership in a group, with their type in that group.</summary>
public sealed record GroupMember(PlayerId PlayerId, PlayerType Type)
{
    public bool IsRegular => Type == PlayerType.Regular;
}
