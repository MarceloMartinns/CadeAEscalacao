using CadeAEscalacao.Application.Abstractions;
using CadeAEscalacao.Domain.Players;
using Microsoft.AspNetCore.Identity;

namespace CadeAEscalacao.Infrastructure.Identity;

/// <summary>Finds the player of a login account (ADR-014) by e-mail, for the president to add members (ADR-021).</summary>
internal sealed class AccountDirectory(UserManager<AppUser> users) : IAccountDirectory
{
    public async Task<PlayerId?> FindPlayerIdByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        var user = await users.FindByEmailAsync(email);
        return user?.PlayerId is { } playerId ? new PlayerId(playerId) : null;
    }
}
