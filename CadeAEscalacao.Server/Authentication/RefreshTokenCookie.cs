using Microsoft.Extensions.Options;

namespace CadeAEscalacao.Server.Authentication;

/// <summary>
/// The refresh token in an HttpOnly cookie, for the browser (ADR-020). JavaScript cannot read it,
/// and the browser only sends it to <c>/api/auth</c>, from the same site.
/// </summary>
public sealed class RefreshTokenCookie(IOptions<JwtOptions> options, TimeProvider timeProvider)
{
    public const string Name = "cadeaescalacao_refresh";

    public const string Path = "/api/auth";

    public string? Read(HttpContext context) =>
        context.Request.Cookies.TryGetValue(Name, out var value) && !string.IsNullOrEmpty(value) ? value : null;

    public void Write(HttpContext context, string refreshToken) =>
        context.Response.Cookies.Append(Name, refreshToken, CreateOptions(
            timeProvider.GetUtcNow() + options.Value.RefreshTokenLifetime));

    public void Delete(HttpContext context) =>
        context.Response.Cookies.Delete(Name, CreateOptions(expires: null));

    private static CookieOptions CreateOptions(DateTimeOffset? expires) => new()
    {
        HttpOnly = true,
        Secure = true,
        SameSite = SameSiteMode.Strict,
        Path = Path,
        Expires = expires,
        IsEssential = true,
    };
}
