using System.Security.Cryptography;
using CadeAEscalacao.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace CadeAEscalacao.Server.Authentication;

/// <summary>Tokens returned by the login: <c>expiresIn</c> is the access token lifetime in seconds.</summary>
public sealed record TokenResponse(string AccessToken, int ExpiresIn, string RefreshToken);

/// <summary>A new access token, without a new refresh token (e.g. after the profile is created).</summary>
public sealed record AccessTokenResponse(string AccessToken, int ExpiresIn);

/// <summary>
/// Issues the API's own tokens (ADR-015): a short JWT signed with HMAC-SHA256 and an opaque
/// refresh token, stored only as a hash and replaced on every use.
/// </summary>
public sealed class TokenService(AuthDbContext db, IOptions<JwtOptions> options, TimeProvider timeProvider)
{
    private readonly JwtOptions _options = options.Value;

    /// <summary>Starts a session: access token and a new refresh token.</summary>
    public async Task<TokenResponse> IssueAsync(AppUser user, CancellationToken cancellationToken = default)
    {
        var (refreshToken, value) = NewRefreshToken(user.Id);
        db.RefreshTokens.Add(refreshToken);
        await db.SaveChangesAsync(cancellationToken);

        var access = CreateAccessToken(user);
        return new TokenResponse(access.AccessToken, access.ExpiresIn, value);
    }

    public AccessTokenResponse CreateAccessToken(AppUser user)
    {
        var now = timeProvider.GetUtcNow();
        var claims = new Dictionary<string, object>
        {
            [AppClaims.UserId] = user.Id.ToString(),
            [AppClaims.Email] = user.Email!,
            [JwtRegisteredClaimNames.Jti] = Guid.NewGuid().ToString(),
        };
        if (user.PlayerId is { } playerId)
        {
            claims[AppClaims.PlayerId] = playerId.ToString();
        }

        var descriptor = new SecurityTokenDescriptor
        {
            Issuer = _options.Issuer,
            Audience = _options.Audience,
            IssuedAt = now.UtcDateTime,
            NotBefore = now.UtcDateTime,
            Expires = (now + _options.AccessTokenLifetime).UtcDateTime,
            Claims = claims,
            SigningCredentials = new SigningCredentials(_options.CreateSigningKey(), SecurityAlgorithms.HmacSha256),
        };

        return new AccessTokenResponse(
            new JsonWebTokenHandler().CreateToken(descriptor),
            (int)_options.AccessTokenLifetime.TotalSeconds);
    }

    /// <summary>
    /// Replaces a valid refresh token by a new one. Returns null if the token is unknown, expired
    /// or revoked. A token that was already replaced is being used a second time, so it leaked:
    /// every token of the user is revoked.
    /// </summary>
    public async Task<TokenResponse?> RefreshAsync(string value, CancellationToken cancellationToken = default)
    {
        var current = await FindAsync(value, cancellationToken);
        if (current is null)
        {
            return null;
        }

        var now = timeProvider.GetUtcNow();
        if (current.RevokedAt is not null)
        {
            if (current.ReplacedById is not null)
            {
                await RevokeAllAsync(current.UserId, now, cancellationToken);
            }

            return null;
        }

        if (current.ExpiresAt <= now)
        {
            return null;
        }

        var user = await db.Users.SingleAsync(appUser => appUser.Id == current.UserId, cancellationToken);
        var (next, nextValue) = NewRefreshToken(user.Id);
        current.RevokedAt = now;
        current.ReplacedById = next.Id;
        db.RefreshTokens.Add(next);
        await db.SaveChangesAsync(cancellationToken);

        var access = CreateAccessToken(user);
        return new TokenResponse(access.AccessToken, access.ExpiresIn, nextValue);
    }

    /// <summary>Logout: the refresh token stops working. Unknown or revoked tokens are ignored.</summary>
    public async Task RevokeAsync(string value, CancellationToken cancellationToken = default)
    {
        if (await FindAsync(value, cancellationToken) is { RevokedAt: null } token)
        {
            token.RevokedAt = timeProvider.GetUtcNow();
            await db.SaveChangesAsync(cancellationToken);
        }
    }

    private Task<RefreshToken?> FindAsync(string value, CancellationToken cancellationToken)
    {
        var hash = Hash(value);
        return db.RefreshTokens.SingleOrDefaultAsync(token => token.TokenHash == hash, cancellationToken);
    }

    private async Task RevokeAllAsync(Guid userId, DateTimeOffset now, CancellationToken cancellationToken)
    {
        var active = await db.RefreshTokens
            .Where(token => token.UserId == userId && token.RevokedAt == null)
            .ToListAsync(cancellationToken);
        foreach (var token in active)
        {
            token.RevokedAt = now;
        }

        await db.SaveChangesAsync(cancellationToken);
    }

    private (RefreshToken Token, string Value) NewRefreshToken(Guid userId)
    {
        var value = Base64UrlEncoder.Encode(RandomNumberGenerator.GetBytes(32));
        var now = timeProvider.GetUtcNow();
        var token = new RefreshToken
        {
            UserId = userId,
            TokenHash = Hash(value),
            CreatedAt = now,
            ExpiresAt = now + _options.RefreshTokenLifetime,
        };
        return (token, value);
    }

    private static string Hash(string value) =>
        Convert.ToBase64String(SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(value)));
}
