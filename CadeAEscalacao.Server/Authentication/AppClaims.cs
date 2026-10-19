using System.Security.Claims;

namespace CadeAEscalacao.Server.Authentication;

/// <summary>Claims of the access token (ADR-015). Kept with their JWT names (no inbound mapping).</summary>
public static class AppClaims
{
    public const string UserId = "sub";

    public const string Email = "email";

    /// <summary>Only present after the profile is created (<c>POST /api/me</c>).</summary>
    public const string PlayerId = "player_id";

    public static Guid GetUserId(this ClaimsPrincipal principal) =>
        Guid.Parse(principal.FindFirstValue(UserId)
            ?? throw new InvalidOperationException("The access token has no 'sub' claim."));
}
