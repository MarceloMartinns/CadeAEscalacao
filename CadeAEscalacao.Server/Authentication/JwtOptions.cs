using System.ComponentModel.DataAnnotations;
using Microsoft.IdentityModel.Tokens;

namespace CadeAEscalacao.Server.Authentication;

/// <summary>
/// Section <c>Jwt</c> of the configuration (ADR-015). The signing key is a secret:
/// User Secrets in development, environment variable in production. Never in appsettings.json.
/// </summary>
public sealed class JwtOptions
{
    public const string SectionName = "Jwt";

    /// <summary>The key must have at least 256 bits for HMAC-SHA256.</summary>
    public const int MinimumKeyBytes = 32;

    [Required]
    public string Issuer { get; set; } = string.Empty;

    [Required]
    public string Audience { get; set; } = string.Empty;

    /// <summary>Random key in Base64.</summary>
    [Required(ErrorMessage =
        "Jwt:SigningKey not found. In development, set it in the Server's User Secrets (ADR-015).")]
    public string SigningKey { get; set; } = string.Empty;

    public TimeSpan AccessTokenLifetime { get; set; } = TimeSpan.FromMinutes(30);

    public TimeSpan RefreshTokenLifetime { get; set; } = TimeSpan.FromDays(30);

    public SymmetricSecurityKey CreateSigningKey() => new(Convert.FromBase64String(SigningKey));

    internal bool HasValidSigningKey()
    {
        var bytes = new byte[SigningKey.Length];
        return Convert.TryFromBase64String(SigningKey, bytes, out var length) && length >= MinimumKeyBytes;
    }
}
