using System.ComponentModel.DataAnnotations;
using CadeAEscalacao.Application.Abstractions;
using CadeAEscalacao.Infrastructure.Identity;
using CadeAEscalacao.Server.Authentication;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Identity;

namespace CadeAEscalacao.Server.Endpoints.Auth;

public sealed record StartEmailLoginRequest([Required, EmailAddress, StringLength(256)] string Email);

public sealed record VerifyEmailLoginRequest(
    [Required, EmailAddress, StringLength(256)] string Email,
    [Required, RegularExpression("^[0-9]{6}$")] string Code);

public sealed record RefreshTokenRequest([Required] string RefreshToken);

/// <summary>Passwordless login with a code sent by e-mail (ADR-014) and the session tokens (ADR-015).</summary>
public static class AuthEndpoints
{
    /// <summary>Purpose of the e-mail code: a code made for something else does not work here.</summary>
    private const string LoginPurpose = "login";

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
                $"Seu código é {code}. Ele vale por poucos minutos. Se não foi você que pediu, ignore este e-mail.",
                cancellationToken);
        }

        return TypedResults.Accepted((string?)null);
    }

    private static async Task<Results<Ok<TokenResponse>, ProblemHttpResult>> VerifyAsync(
        VerifyEmailLoginRequest request,
        UserManager<AppUser> users,
        TokenService tokens,
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

        return TypedResults.Ok(await tokens.IssueAsync(user, cancellationToken));
    }

    private static async Task<Results<Ok<TokenResponse>, ProblemHttpResult>> RefreshAsync(
        RefreshTokenRequest request, TokenService tokens, CancellationToken cancellationToken) =>
        await tokens.RefreshAsync(request.RefreshToken, cancellationToken) is { } response
            ? TypedResults.Ok(response)
            : TypedResults.Problem(
                statusCode: StatusCodes.Status401Unauthorized,
                title: "Invalid refresh token",
                detail: "The refresh token is invalid, expired or revoked. Sign in again.");

    private static async Task<NoContent> LogoutAsync(
        RefreshTokenRequest request, TokenService tokens, CancellationToken cancellationToken)
    {
        await tokens.RevokeAsync(request.RefreshToken, cancellationToken);
        return TypedResults.NoContent();
    }

    // Same answer for an unknown e-mail, a wrong or expired code and a locked account,
    // so the response does not tell which e-mails have an account.
    private static ProblemHttpResult InvalidCode() =>
        TypedResults.Problem(
            statusCode: StatusCodes.Status400BadRequest,
            title: "Invalid code",
            detail: "The code is invalid or expired. Ask for a new code.");
}
