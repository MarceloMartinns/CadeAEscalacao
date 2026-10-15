namespace CadeAEscalacao.Application.Abstractions;

/// <summary>
/// Saves every change made through the repositories in one transaction.
/// Can throw a concurrency error when another request changed the same aggregate (ADR-012);
/// the API answers 409.
/// </summary>
public interface IUnitOfWork
{
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
