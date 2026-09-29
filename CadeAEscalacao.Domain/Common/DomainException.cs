namespace CadeAEscalacao.Domain.Common;

/// <summary>
/// A business rule was broken (e.g. a player who is already in a pair).
/// Invalid arguments (empty name, negative number) still use the ArgumentException family.
/// </summary>
public class DomainException(string message) : Exception(message);
