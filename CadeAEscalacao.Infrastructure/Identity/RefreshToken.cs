namespace CadeAEscalacao.Infrastructure.Identity;

/// <summary>
/// Long-lived token used to get a new access token (ADR-015). Only the SHA-256 hash of the value
/// is stored. Each use replaces the token by a new one (rotation); a token used twice means it
/// leaked, and every token of the user is revoked.
/// </summary>
public sealed class RefreshToken
{
    public Guid Id { get; init; } = Guid.CreateVersion7();

    public Guid UserId { get; init; }

    /// <summary>SHA-256 of the token value, in Base64.</summary>
    public required string TokenHash { get; init; }

    public DateTimeOffset CreatedAt { get; init; }

    public DateTimeOffset ExpiresAt { get; init; }

    public DateTimeOffset? RevokedAt { get; set; }

    /// <summary>The token that replaced this one in a rotation, if any.</summary>
    public Guid? ReplacedById { get; set; }
}
