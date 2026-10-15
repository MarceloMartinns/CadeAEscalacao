namespace CadeAEscalacao.Application.Common;

/// <summary>The resource does not exist. The API answers 404 (ADR-013).</summary>
public sealed class NotFoundException(string resource, object id)
    : Exception($"{resource} '{id}' was not found.");
