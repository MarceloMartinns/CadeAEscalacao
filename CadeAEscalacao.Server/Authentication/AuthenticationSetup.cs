using System.Threading.RateLimiting;
using CadeAEscalacao.Application.Abstractions;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace CadeAEscalacao.Server.Authentication;

public static class AuthenticationSetup
{
    /// <summary>Rate limiting policy of the <c>/api/auth</c> endpoints (ADR-014).</summary>
    public const string AuthRateLimitPolicy = "auth";

    /// <summary>Signed in and with a player (<c>player_id</c> claim).</summary>
    public const string RegisteredPlayerPolicy = "RegisteredPlayer";

    /// <summary>
    /// JWT validation (ADR-015), authorization and the rate limit of the login endpoints.
    /// The options are read when the first request arrives, so <c>dotnet ef</c> works without the key.
    /// </summary>
    public static IServiceCollection AddApiAuthentication(this IServiceCollection services)
    {
        services.TryAddSingleton(TimeProvider.System);

        services.AddOptions<JwtOptions>()
            .BindConfiguration(JwtOptions.SectionName)
            .ValidateDataAnnotations()
            .Validate(
                options => options.HasValidSigningKey(),
                $"Jwt:SigningKey must be a Base64 key with at least {JwtOptions.MinimumKeyBytes} bytes (ADR-015).")
            .ValidateOnStart();

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer();
        services.AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
            .Configure<IOptions<JwtOptions>>((bearer, jwt) =>
            {
                // Keep "sub", "email" and "player_id" as they are in the token.
                bearer.MapInboundClaims = false;
                bearer.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidIssuer = jwt.Value.Issuer,
                    ValidAudience = jwt.Value.Audience,
                    IssuerSigningKey = jwt.Value.CreateSigningKey(),
                    ValidAlgorithms = [SecurityAlgorithms.HmacSha256],
                    NameClaimType = AppClaims.Email,
                    ClockSkew = TimeSpan.FromSeconds(30),
                };
            });

        // Authenticated and with a player: every endpoint except /api/auth and /api/me (ADR-016).
        services.AddAuthorizationBuilder()
            .AddPolicy(RegisteredPlayerPolicy, policy => policy
                .RequireAuthenticatedUser()
                .RequireClaim(AppClaims.PlayerId));
        services.AddSingleton<IAuthorizationMiddlewareResultHandler, ProblemDetailsAuthorizationResultHandler>();

        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentUser, CurrentUser>();
        services.AddScoped<TokenService>();
        services.AddSingleton<RefreshTokenCookie>();

        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            options.AddPolicy(AuthRateLimitPolicy, context => RateLimitPartition.GetFixedWindowLimiter(
                context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                _ => new FixedWindowRateLimiterOptions { PermitLimit = 10, Window = TimeSpan.FromMinutes(1) }));
        });

        return services;
    }
}
