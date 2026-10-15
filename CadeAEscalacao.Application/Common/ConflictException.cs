namespace CadeAEscalacao.Application.Common;

/// <summary>
/// The resource already exists (e.g. a second lineup for the same list). The API answers 409 (ADR-013).
/// </summary>
public sealed class ConflictException(string message) : Exception(message);
