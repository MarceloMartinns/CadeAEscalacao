using System.Collections.Concurrent;
using System.Text.RegularExpressions;
using CadeAEscalacao.Application.Abstractions;

namespace CadeAEscalacao.Server.Tests;

/// <summary>Keeps the e-mails in memory, so the tests can read the login code.</summary>
public sealed partial class FakeEmailSender : IEmailSender
{
    public ConcurrentQueue<(string To, string Subject, string Body)> Sent { get; } = new();

    public Task SendAsync(string to, string subject, string body, CancellationToken cancellationToken = default)
    {
        Sent.Enqueue((to, subject, body));
        return Task.CompletedTask;
    }

    /// <summary>The 6-digit code of the last e-mail sent to the address.</summary>
    public string LastCodeSentTo(string to) =>
        SixDigits().Match(Sent.Last(email => email.To == to).Body).Value;

    [GeneratedRegex("[0-9]{6}")]
    private static partial Regex SixDigits();
}
