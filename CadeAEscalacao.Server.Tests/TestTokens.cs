using System.Security.Claims;
using CadeAEscalacao.Server.Authentication;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace CadeAEscalacao.Server.Tests;

/// <summary>
/// Access tokens made by the tests, signed with the same test key as the API (<see cref="ApiFactory"/>),
/// so the real JWT validation runs. Used to simulate signed-in players without the e-mail login.
/// </summary>
public static class TestTokens
{
    public const string Issuer = "cadeaescalacao-tests";

    public const string Audience = "cadeaescalacao-tests";

    /// <summary>32 bytes in Base64, only for the tests.</summary>
    public const string SigningKey = "dGVzdC1rZXktb25seS1mb3ItdGhlLXRlc3RzLTEyMzQ=";

    public static string Create(
        Guid? playerId = null,
        Guid? userId = null,
        DateTimeOffset? expires = null,
        string signingKey = SigningKey,
        string issuer = Issuer)
    {
        var expiresAt = expires ?? DateTimeOffset.UtcNow.AddMinutes(30);
        var claims = new List<Claim>
        {
            new(AppClaims.UserId, (userId ?? Guid.NewGuid()).ToString()),
            new(AppClaims.Email, "player@example.com"),
        };
        if (playerId is { } id)
        {
            claims.Add(new Claim(AppClaims.PlayerId, id.ToString()));
        }

        return new JsonWebTokenHandler().CreateToken(new SecurityTokenDescriptor
        {
            Issuer = issuer,
            Audience = Audience,
            Subject = new ClaimsIdentity(claims),
            IssuedAt = expiresAt.AddMinutes(-30).UtcDateTime,
            NotBefore = expiresAt.AddMinutes(-30).UtcDateTime,
            Expires = expiresAt.UtcDateTime,
            SigningCredentials = new SigningCredentials(
                new SymmetricSecurityKey(Convert.FromBase64String(signingKey)), SecurityAlgorithms.HmacSha256),
        });
    }
}
