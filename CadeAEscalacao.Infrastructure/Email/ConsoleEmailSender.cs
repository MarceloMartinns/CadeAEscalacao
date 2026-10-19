using CadeAEscalacao.Application.Abstractions;
using Microsoft.Extensions.Logging;

namespace CadeAEscalacao.Infrastructure.Email;

/// <summary>
/// Writes the e-mail to the log instead of sending it, so the login code can be read in the
/// API console during development (ADR-014). Replaced by a real provider in etapa 10.
/// </summary>
internal sealed partial class ConsoleEmailSender(ILogger<ConsoleEmailSender> logger) : IEmailSender
{
    public Task SendAsync(string to, string subject, string body, CancellationToken cancellationToken = default)
    {
        LogEmail(to, subject, body);
        return Task.CompletedTask;
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "E-mail to {To} | {Subject}\n{Body}")]
    private partial void LogEmail(string to, string subject, string body);
}
