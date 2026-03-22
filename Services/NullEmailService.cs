using BibleReader.Api.Interfaces;

namespace BibleReader.Api.Services;

/// <summary>Usado quando SendGrid:ApiKey está vazio (desenvolvimento).</summary>
public class NullEmailService : IEmailService
{
    private readonly ILogger<NullEmailService> _logger;

    public NullEmailService(ILogger<NullEmailService> logger)
    {
        _logger = logger;
    }

    public Task SendEmailAsync(string to, string subject, string htmlContent, CancellationToken ct = default)
    {
        _logger.LogWarning("E-mail não enviado (SendGrid desativado). Para: {To}, Assunto: {Subject}", to, subject);
        return Task.CompletedTask;
    }
}
