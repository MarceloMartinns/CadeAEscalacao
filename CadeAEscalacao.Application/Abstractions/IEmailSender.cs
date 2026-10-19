namespace CadeAEscalacao.Application.Abstractions;

/// <summary>
/// Sends e-mails, such as the login code (ADR-014). In development the e-mail is only written
/// to the API console; a real provider comes in etapa 10.
/// </summary>
public interface IEmailSender
{
    Task SendAsync(string to, string subject, string body, CancellationToken cancellationToken = default);
}
