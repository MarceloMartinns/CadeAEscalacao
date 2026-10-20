using System.ComponentModel.DataAnnotations;
using CadeAEscalacao.Application.Abstractions;
using CadeAEscalacao.Infrastructure.Identity;
using CadeAEscalacao.Server.Authentication;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Identity;

namespace CadeAEscalacao.Server.Endpoints.Auth;

public sealed record StartEmailLoginRequest([Required, EmailAddress, StringLength(256)] string Email);

/// <param name="UseCookie">The browser sends true: the refresh token goes to an HttpOnly cookie, not to the body (ADR-020).</param>
public sealed record VerifyEmailLoginRequest(
    [Required, EmailAddress, StringLength(256)] string Email,
    [Required, RegularExpression("^[0-9]{6}$")] string Code,
    bool UseCookie = false);

/// <param name="RefreshToken">Read from the cookie when empty (ADR-020).</param>
/// <param name="UseCookie">The browser sends true: the new refresh token goes to the cookie, not to the body.</param>
public sealed record RefreshTokenRequest(string? RefreshToken = null, bool UseCookie = false);

/// <summary>Passwordless login with a code sent by e-mail (ADR-014) and the session tokens (ADR-015).</summary>
public static class AuthEndpoints
{
    /// <summary>Purpose of the e-mail code: a code made for something else does not work here.</summary>
    private const string LoginPurpose = "login";

    /// <summary>Address of the app, used in the login link (ADR-022). Without it, the e-mail has only the code.</summary>
    public const string PublicUrlSetting = "App:PublicUrl";

    public static IEndpointRouteBuilder MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        var auth = app.MapGroup("/auth")
            .WithTags("Auth")
            .AllowAnonymous()
            .RequireRateLimiting(AuthenticationSetup.AuthRateLimitPolicy)
            .ProducesProblem(StatusCodes.Status429TooManyRequests);

        auth.MapPost("/email/start", StartAsync)
            .WithSummary("Sends a 6-digit login code to the e-mail. Always 202, whether the e-mail exists or not.");
        auth.MapPost("/email/verify", VerifyAsync)
            .WithSummary("Checks the code and starts a session. Creates the account on the first login.");
        auth.MapPost("/refresh", RefreshAsync)
            .WithSummary("Replaces the refresh token by a new one and returns a new access token.")
            .ProducesProblem(StatusCodes.Status401Unauthorized);
        auth.MapPost("/logout", LogoutAsync).WithSummary("Revokes the refresh token.");

        return app;
    }

    private static async Task<Accepted> StartAsync(
        StartEmailLoginRequest request,
        UserManager<AppUser> users,
        IEmailSender emailSender,
        IConfiguration configuration,
        CancellationToken cancellationToken)
    {
        var email = request.Email.Trim();
        var user = await users.FindByEmailAsync(email);
        if (user is null)
        {
            user = new AppUser { UserName = email, Email = email };
            if (!(await users.CreateAsync(user)).Succeeded)
            {
                // Created at the same time by another request: use that one.
                user = await users.FindByEmailAsync(email);
            }
        }

        if (user is not null && !await users.IsLockedOutAsync(user))
        {
            var code = await users.GenerateUserTokenAsync(user, TokenOptions.DefaultEmailProvider, LoginPurpose);
            await emailSender.SendAsync(
                user.Email!,
                "Seu código de acesso ao Cadê a escalação?",
                LoginEmailBody(code, user.Email!, configuration[PublicUrlSetting]),
                cancellationToken);
        }

        return TypedResults.Accepted((string?)null);
    }

    /// <summary>
    /// The code first, then a link that fills it in. The code goes in the fragment (#), which the
    /// browser does not send to any server, so it stays out of the logs (ADR-022).
    /// </summary>
    private static string LoginEmailBody(string code, string email, string? publicUrl)
    {
        var body = $"Seu código é {code}. Ele vale por poucos minutos. Se não foi você que pediu, ignore este e-mail.";
        if (string.IsNullOrWhiteSpace(publicUrl))
        {
            return body;
        }

        var link = $"{publicUrl.TrimEnd('/')}/login#email={Uri.EscapeDataString(email)}&code={code}";
        return $"{body}\n\nOu toque no link para entrar: {link}";
    }

    private static async Task<Results<Ok<TokenResponse>, ProblemHttpResult>> VerifyAsync(
        VerifyEmailLoginRequest request,
        UserManager<AppUser> users,
        TokenService tokens,
        RefreshTokenCookie cookie,
        HttpContext context,
        CancellationToken cancellationToken)
    {
        var user = await users.FindByEmailAsync(request.Email.Trim());
        if (user is null || await users.IsLockedOutAsync(user))
        {
            return InvalidCode();
        }

        if (!await users.VerifyUserTokenAsync(user, TokenOptions.DefaultEmailProvider, LoginPurpose, request.Code))
        {
            // After 5 wrong codes the account is locked for a while (ADR-014).
            await users.AccessFailedAsync(user);
            return InvalidCode();
        }

        await users.ResetAccessFailedCountAsync(user);
        user.EmailConfirmed = true;

        // A new security stamp invalidates the code, so it cannot be used twice. Also saves EmailConfirmed.
        await users.UpdateSecurityStampAsync(user);

        var response = await tokens.IssueAsync(user, cancellationToken);
        return TypedResults.Ok(request.UseCookie ? MoveToCookie(response, cookie, context) : response);
    }

    private static async Task<Results<Ok<TokenResponse>, ProblemHttpResult>> RefreshAsync(
        RefreshTokenRequest request,
        TokenService tokens,
        RefreshTokenCookie cookie,
        HttpContext context,
        CancellationToken cancellationToken)
    {
        var refreshToken = string.IsNullOrEmpty(request.RefreshToken) ? cookie.Read(context) : request.RefreshToken;
        if (refreshToken is not null && await tokens.RefreshAsync(refreshToken, cancellationToken) is { } response)
        {
            return TypedResults.Ok(request.UseCookie ? MoveToCookie(response, cookie, context) : response);
        }

        cookie.Delete(context);
        return TypedResults.Problem(
            statusCode: StatusCodes.Status401Unauthorized,
            title: "Invalid refresh token",
            detail: "The refresh token is invalid, expired or revoked. Sign in again.");
    }

    private static async Task<NoContent> LogoutAsync(
        RefreshTokenRequest request,
        TokenService tokens,
        RefreshTokenCookie cookie,
        HttpContext context,
        CancellationToken cancellationToken)
    {
        var refreshToken = string.IsNullOrEmpty(request.RefreshToken) ? cookie.Read(context) : request.RefreshToken;
        if (refreshToken is not null)
        {
            await tokens.RevokeAsync(refreshToken, cancellationToken);
        }

        cookie.Delete(context);
        return TypedResults.NoContent();
    }

    /// <summary>The browser keeps the refresh token in the HttpOnly cookie only, never in JavaScript (ADR-020).</summary>
    private static TokenResponse MoveToCookie(TokenResponse response, RefreshTokenCookie cookie, HttpContext context)
    {
        cookie.Write(context, response.RefreshToken!);
        return response with { RefreshToken = null };
    }

    // Same answer for an unknown e-mail, a wrong or expired code and a locked account,
    // so the response does not tell which e-mails have an account.
    private static ProblemHttpResult InvalidCode() =>
        TypedResults.Problem(
            statusCode: StatusCodes.Status400BadRequest,
            title: "Invalid code",
            detail: "The code is invalid or expired. Ask for a new code.");
}
