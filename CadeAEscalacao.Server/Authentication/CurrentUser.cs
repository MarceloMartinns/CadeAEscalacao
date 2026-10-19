using CadeAEscalacao.Application.Abstractions;
using CadeAEscalacao.Domain.Players;

namespace CadeAEscalacao.Server.Authentication;

/// <summary>
/// The player of the access token (ADR-017). Only used by endpoints with the
/// <see cref="AuthenticationSetup.RegisteredPlayerPolicy"/>, so the claim is always there.
/// </summary>
internal sealed class CurrentUser(IHttpContextAccessor httpContextAccessor) : ICurrentUser
{
    public PlayerId PlayerId =>
        new(Guid.Parse(httpContextAccessor.HttpContext?.User.FindFirst(AppClaims.PlayerId)?.Value
            ?? throw new InvalidOperationException("The signed-in user has no player.")));
}
