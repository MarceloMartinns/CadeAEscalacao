using Microsoft.AspNetCore.Identity;

namespace CadeAEscalacao.Infrastructure.Identity;

/// <summary>
/// Login account (ADR-014). There is no password: the person signs in with a code sent by e-mail.
/// The domain does not know the user; the link to the domain is <see cref="PlayerId"/>.
/// </summary>
public sealed class AppUser : IdentityUser<Guid>
{
    public AppUser()
    {
        // Same kind of ID as the aggregates (ADR-010): ordered by creation, good for indexes.
        Id = Guid.CreateVersion7();
    }

    /// <summary>The player of this account, or null until the profile is created (<c>POST /api/me</c>).</summary>
    public Guid? PlayerId { get; set; }
}
