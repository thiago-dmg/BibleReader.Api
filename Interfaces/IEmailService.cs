namespace BibleReader.Api.Interfaces;

public interface IEmailService
{
    Task SendEmailAsync(string to, string subject, string htmlContent, CancellationToken ct = default);
}
