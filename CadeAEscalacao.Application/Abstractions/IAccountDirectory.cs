using CadeAEscalacao.Domain.Players;

namespace CadeAEscalacao.Application.Abstractions;

/// <summary>
/// Looks up login accounts (ADR-014), which live outside the domain. Implemented in the
/// Infrastructure with ASP.NET Core Identity.
/// </summary>
public interface IAccountDirectory
{
    /// <summary>The player linked to the account with this e-mail, or null if there is no account or no profile yet.</summary>
    Task<PlayerId?> FindPlayerIdByEmailAsync(string email, CancellationToken cancellationToken = default);
}
