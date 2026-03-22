using BibleReader.Api.Data;
using BibleReader.Api.Interfaces;
using BibleReader.Api.Models;
using BibleReader.Api.ViewModels;
using BibleReader.Api.ViewModels.Support;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BibleReader.Api.Controllers;

[ApiController]
[AllowAnonymous]
[Route("v1/support")]
public class SupportController : ControllerBase
{
    [HttpPost("messages")]
    public async Task<IActionResult> PostMessage(
        [FromBody] SupportMessageViewModel model,
        [FromServices] AppDbContext db,
        [FromServices] IEmailService emailService,
        [FromServices] IConfiguration configuration,
        [FromServices] ILogger<SupportController> logger)
    {
        var name = model.Name.Trim();
        var email = model.Email.Trim();
        var message = model.Message.Trim();

        if (name.Length == 0 || email.Length == 0 || message.Length == 0)
            return BadRequest(new ResultViewModel<string>("Preencha nome, e-mail e mensagem"));

        var entity = new SupportMessage
        {
            Name = name,
            Email = email,
            Message = message
        };
        db.SupportMessages.Add(entity);
        await db.SaveChangesAsync();

        var to = configuration["Support:NotifyEmail"] ?? configuration["SendGrid:FromEmail"];
        if (!string.IsNullOrWhiteSpace(to))
        {
            try
            {
                await emailService.SendEmailAsync(
                    to,
                    $"[Bíblia Reader] Suporte — {name}",
                    $@"<p><b>E-mail do utilizador:</b> {System.Net.WebUtility.HtmlEncode(email)}</p>
<p><b>Mensagem:</b></p>
<p>{System.Net.WebUtility.HtmlEncode(message)}</p>");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Falha ao enviar e-mail de suporte");
            }
        }

        return Ok(new ResultViewModel<string>("Recebemos sua mensagem. Obrigado."));
    }
}
