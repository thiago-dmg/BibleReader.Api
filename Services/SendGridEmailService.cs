using BibleReader.Api.Interfaces;
using SendGrid;
using SendGrid.Helpers.Mail;
using System.Net;

namespace BibleReader.Api.Services;

public class SendGridEmailService : IEmailService
{
    private readonly IConfiguration _configuration;

    public SendGridEmailService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public async Task SendEmailAsync(string to, string subject, string htmlContent, CancellationToken ct = default)
    {
        var apiKey = _configuration["SendGrid:ApiKey"];
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            throw new InvalidOperationException("SendGrid:ApiKey não configurado.");
        }

        var client = new SendGridClient(apiKey);
        var fromEmail = _configuration["SendGrid:FromEmail"] ?? "noreply@localhost";
        var fromName = _configuration["SendGrid:FromName"] ?? "Bíblia Reader";
        var from = new EmailAddress(fromEmail, fromName);
        var toEmail = new EmailAddress(to);
        var msg = MailHelper.CreateSingleEmail(from, toEmail, subject, null, htmlContent);
        var response = await client.SendEmailAsync(msg, ct);
        if ((int)response.StatusCode < 200 || (int)response.StatusCode >= 300)
        {
            var body = await response.Body.ReadAsStringAsync(ct);
            var snippet = string.IsNullOrWhiteSpace(body)
                ? "sem detalhe"
                : (body.Length > 500 ? body[..500] : body);
            throw new InvalidOperationException(
                $"Falha no SendGrid ({(int)response.StatusCode} {response.StatusCode}): {snippet}");
        }
    }
}
