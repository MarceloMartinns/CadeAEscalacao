namespace CadeAEscalacao.Application.Common;

/// <summary>The signed-in person does not have the role this action needs (ADR-016). The API answers 403.</summary>
public sealed class ForbiddenException(string message) : Exception(message);
