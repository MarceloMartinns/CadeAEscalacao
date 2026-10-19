using CadeAEscalacao.Domain.Groups;
using CadeAEscalacao.Domain.Lineups;
using CadeAEscalacao.Domain.Players;

namespace CadeAEscalacao.Application.Common;

/// <summary>
/// Role checks of the use cases (ADR-016). The roles come from the group and from the lineup,
/// not from the account: the same person may be president in one group and only a player in another.
/// </summary>
internal static class Roles
{
    public static void EnsureMember(Group group, PlayerId playerId)
    {
        if (!group.IsMember(playerId))
        {
            throw new ForbiddenException("Only members of the group can do this.");
        }
    }

    public static void EnsurePresident(Group group, PlayerId playerId)
    {
        if (group.PresidentId != playerId)
        {
            throw new ForbiddenException("Only the president of the group can do this.");
        }
    }

    public static void EnsureCoach(Lineup lineup, PlayerId playerId)
    {
        if (lineup.CoachId != playerId)
        {
            throw new ForbiddenException("Only the coach of the week can do this.");
        }
    }

    public static void EnsurePartner(Lineup lineup, PlayerId playerId)
    {
        if (lineup.PartnerId != playerId)
        {
            throw new ForbiddenException("Only the coach's partner can do this.");
        }
    }

    public static void EnsureCoachOrPresident(Lineup lineup, Group group, PlayerId playerId)
    {
        if (lineup.CoachId != playerId && group.PresidentId != playerId)
        {
            throw new ForbiddenException("Only the coach of the week or the president of the group can do this.");
        }
    }
}
