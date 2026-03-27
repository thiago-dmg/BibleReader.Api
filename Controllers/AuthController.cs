using System.Net;
using BibleReader.Api.Data;
using BibleReader.Api.Extensions;
using BibleReader.Api.Interfaces;
using BibleReader.Api.Models;
using BibleReader.Api.Services;
using BibleReader.Api.ViewModels;
using BibleReader.Api.ViewModels.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BibleReader.Api.Controllers;

[ApiController]
[Route("v1/auth")]
public class AuthController : ControllerBase
{
    private readonly IWebHostEnvironment _env;
    private readonly ILogger<AuthController> _logger;

    public AuthController(
        IWebHostEnvironment env,
        ILogger<AuthController> logger)
    {
        _env = env;
        _logger = logger;
    }

    [AllowAnonymous]
    [HttpPost("register")]
    public async Task<IActionResult> Register(
        [FromBody] RegisterViewModel model,
        [FromServices] AppDbContext db,
        [FromServices] IEmailService emailService,
        [FromServices] IConfiguration configuration)
    {
        if (!ModelState.IsValid)
            return BadRequest(new ResultViewModel<string>(ModelState.GetErrors()));

        var email = model.Email.Trim();
        if (await db.Users.AnyAsync(x => x.Email == email))
            return BadRequest(new ResultViewModel<string>("E-mail já cadastrado"));

        var user = new AppUser
        {
            DisplayName = model.DisplayName.Trim(),
            Email = email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(model.Password),
            EmailVerified = false,
            CreatedAt = DateTime.UtcNow
        };

        db.Users.Add(user);
        await db.SaveChangesAsync();

        var verificationToken = new EmailVerificationToken
        {
            AppUserId = user.Id
        };

        db.EmailVerificationTokens.Add(verificationToken);
        await db.SaveChangesAsync();

        var baseUrl = configuration["AppSettings:BaseUrl"]?.TrimEnd('/')
            ?? $"{Request.Scheme}://{Request.Host}";

        var verificationLink =
            $"{baseUrl}/verify-email?token={Uri.EscapeDataString(verificationToken.Token)}";

        var emailHtml = LoadTemplate("verify-email-email.html");

        if (string.IsNullOrWhiteSpace(emailHtml))
        {
            emailHtml = $@"
<html>
<body style='margin:0;padding:20px;background:#F6F2FF;font-family:Arial,sans-serif;'>
    <div style='max-width:420px;margin:auto;background:#fff;padding:28px;border-radius:16px;text-align:center;border:1px solid #EEE7FF;'>
        <h2 style='color:#4B2E83;margin-top:0;'>Bíblia Reader</h2>
        <p style='color:#333;'>Olá, {WebUtility.HtmlEncode(user.DisplayName)}.</p>
        <p style='color:#333;'>Seu cadastro foi realizado com sucesso. Confirme seu e-mail para continuar.</p>

        <a href='{WebUtility.HtmlEncode(verificationLink)}'
           style='display:inline-block;margin-top:16px;padding:12px 24px;background:#6C4DFF;color:#fff;text-decoration:none;border-radius:10px;font-weight:bold;'>
            Confirmar e-mail
        </a>

        <p style='margin-top:20px;font-size:12px;color:#777;'>
            Se o botão não funcionar, copie e cole este link no navegador:
        </p>
        <p style='font-size:12px;color:#777;word-break:break-all;'>
            {WebUtility.HtmlEncode(verificationLink)}
        </p>
    </div>
</body>
</html>";
        }
        else
        {
            emailHtml = emailHtml
                .Replace("{{APP_NAME}}", "Bíblia Reader")
                .Replace("{{USER_NAME}}", WebUtility.HtmlEncode(user.DisplayName))
                .Replace("{{TITLE}}", "Confirme seu e-mail")
                .Replace("{{MESSAGE}}", "Seu cadastro foi realizado com sucesso. Confirme seu e-mail para continuar.")
                .Replace("{{BUTTON_LABEL}}", "Confirmar e-mail")
                .Replace("{{BUTTON_URL}}", WebUtility.HtmlEncode(verificationLink))
                .Replace("{{FOOTER_MESSAGE}}", "Se você não solicitou este cadastro, ignore este e-mail.");
        }

        try
        {
            await emailService.SendEmailAsync(
                user.Email,
                "Confirme seu e-mail — Bíblia Reader",
                emailHtml);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Falha ao enviar e-mail de verificação");
            return StatusCode(StatusCodes.Status503ServiceUnavailable,
                new ResultViewModel<string>(
                    "Não foi possível enviar o e-mail de confirmação agora. Tente novamente em instantes."));
        }

        return Ok(new ResultViewModel<string>("Cadastro realizado. Verifique seu e-mail."));
    }

    [AllowAnonymous]
    [HttpPost("login")]
    public async Task<IActionResult> Login(
        [FromBody] LoginViewModel model,
        [FromServices] AppDbContext db,
        [FromServices] TokenService tokenService)
    {
        if (!ModelState.IsValid)
            return BadRequest(new ResultViewModel<string>(ModelState.GetErrors()));

        var email = model.Email.Trim();
        var user = await db.Users.FirstOrDefaultAsync(x => x.Email == email);

        if (user == null || !BCrypt.Net.BCrypt.Verify(model.Password, user.PasswordHash))
            return Unauthorized(new ResultViewModel<string>("E-mail ou senha inválidos"));

        if (!user.EmailVerified)
        {
            return StatusCode(StatusCodes.Status403Forbidden,
                new ResultViewModel<string>("Confirme seu e-mail antes de entrar"));
        }

        var token = tokenService.GenerateToken(user);

        return Ok(new ResultViewModel<object>(new
        {
            token,
            userId = user.Id,
            displayName = user.DisplayName,
            email = user.Email,
            emailVerified = user.EmailVerified
        }));
    }

    [AllowAnonymous]
    [HttpPost("verify-email")]
    public async Task<IActionResult> VerifyEmailPost(
        [FromBody] VerifyEmailViewModel model,
        [FromServices] AppDbContext db,
        [FromServices] TokenService tokenService)
    {
        if (model == null || string.IsNullOrWhiteSpace(model.Token))
            return BadRequest(new ResultViewModel<string>("Token inválido"));

        var token = model.Token.Trim();
        var email = string.IsNullOrWhiteSpace(model.Email) ? null : model.Email.Trim();
        return await VerifyCoreAsync(token, email, db, tokenService, issueJwt: true);
    }

    [ApiExplorerSettings(IgnoreApi = true)]
    [AllowAnonymous]
    [HttpGet("/verify-email")]
    public async Task<IActionResult> VerifyEmailPage(
        [FromQuery] string token,
        [FromServices] AppDbContext db)
    {
        var html = LoadTemplate("verify-email.html");

        if (string.IsNullOrWhiteSpace(html))
            return Content(GetVerifyEmailFallbackHtml(), "text/html; charset=utf-8");

        const string appDeepLink = "bibliareader://login";

        string title = "Link inválido";
        string message = "Não foi possível validar este link.";
        string iconClass = "error";
        string buttonLabel = "Abrir aplicativo";
        string buttonUrl = appDeepLink;

        if (string.IsNullOrWhiteSpace(token))
        {
            title = "Link inválido";
            message = "O link de confirmação está incompleto.";
        }
        else
        {
            var verification = await db.EmailVerificationTokens
                .Include(x => x.AppUser)
                .FirstOrDefaultAsync(x => x.Token == token);

            if (verification == null)
            {
                title = "Link inválido ou expirado";
                message = "O link não é válido ou já expirou.";
            }
            else if (verification.ExpiresAt < DateTime.UtcNow)
            {
                title = "Link expirado";
                message = "Este link já expirou.";
            }
            else
            {
                verification.AppUser.EmailVerified = true;
                db.EmailVerificationTokens.Remove(verification);
                await db.SaveChangesAsync();

                title = "E-mail confirmado com sucesso!";
                message = "Sua conta foi ativada. Agora é só abrir o app e continuar sua jornada.";
                iconClass = "success";
            }
        }

        html = html
            .Replace("{{TITLE}}", WebUtility.HtmlEncode(title))
            .Replace("{{MESSAGE}}", WebUtility.HtmlEncode(message))
            .Replace("{{ICON_CLASS}}", WebUtility.HtmlEncode(iconClass))
            .Replace("{{BUTTON_LABEL}}", WebUtility.HtmlEncode(buttonLabel))
            .Replace("{{BUTTON_URL}}", WebUtility.HtmlEncode(buttonUrl));

        return Content(html, "text/html; charset=utf-8");
    }

    /// <param name="issueJwt">Quando true (POST do app), retorna o mesmo formato do login para entrar direto.</param>
    private async Task<IActionResult> VerifyCoreAsync(
        string token,
        string? email,
        AppDbContext db,
        TokenService tokenService,
        bool issueJwt)
    {
        var verification = await db.EmailVerificationTokens
            .Include(x => x.AppUser)
            .FirstOrDefaultAsync(x => x.Token == token);

        if (verification == null)
            return BadRequest(new ResultViewModel<string>("Token inválido"));

        if (verification.ExpiresAt < DateTime.UtcNow)
            return BadRequest(new ResultViewModel<string>("Token expirado"));

        if (!string.IsNullOrWhiteSpace(email) &&
            !string.Equals(verification.AppUser.Email, email, StringComparison.OrdinalIgnoreCase))
        {
            return BadRequest(new ResultViewModel<string>("E-mail não confere com o link de verificação"));
        }

        verification.AppUser.EmailVerified = true;
        db.EmailVerificationTokens.Remove(verification);
        await db.SaveChangesAsync();

        if (issueJwt)
        {
            var jwt = tokenService.GenerateToken(verification.AppUser);
            return Ok(new ResultViewModel<object>(new
            {
                token = jwt,
                userId = verification.AppUser.Id,
                displayName = verification.AppUser.DisplayName,
                email = verification.AppUser.Email,
                emailVerified = true
            }));
        }

        return Ok(new ResultViewModel<string>("E-mail confirmado com sucesso"));
    }

    [AllowAnonymous]
    [HttpPost("forgot-password")]
    public async Task<IActionResult> ForgotPassword(
        [FromBody] ForgotPasswordViewModel model,
        [FromServices] AppDbContext db,
        [FromServices] IEmailService emailService,
        [FromServices] IConfiguration configuration)
    {
        var email = model.Email?.Trim();

        if (string.IsNullOrWhiteSpace(email))
            return Ok(new ResultViewModel<string>("Se o e-mail existir, você receberá instruções."));

        var user = await db.Users.FirstOrDefaultAsync(x => x.Email == email);

        if (user == null)
            return Ok(new ResultViewModel<string>("Se o e-mail existir, você receberá instruções."));

        var reset = new PasswordResetToken
        {
            AppUserId = user.Id
        };

        db.PasswordResetTokens.Add(reset);
        await db.SaveChangesAsync();

        var baseUrl = configuration["AppSettings:BaseUrl"]?.TrimEnd('/')
            ?? $"{Request.Scheme}://{Request.Host}";

        var resetLink =
            $"{baseUrl}/reset-password?token={Uri.EscapeDataString(reset.Token)}";

        var emailHtml = LoadTemplate("forgot-password-email.html");

        if (string.IsNullOrWhiteSpace(emailHtml))
        {
            emailHtml = $@"
<html>
<body style='margin:0;padding:20px;background:#F6F2FF;font-family:Arial,sans-serif;'>
    <div style='max-width:420px;margin:auto;background:#fff;padding:28px;border-radius:16px;text-align:center;border:1px solid #EEE7FF;'>
        <h2 style='color:#4B2E83;margin-top:0;'>Bíblia Reader</h2>
        <p style='color:#333;'>Recebemos uma solicitação para redefinir sua senha.</p>

        <a href='{WebUtility.HtmlEncode(resetLink)}'
           style='display:inline-block;margin-top:16px;padding:12px 24px;background:#6C4DFF;color:#fff;text-decoration:none;border-radius:10px;font-weight:bold;'>
            Redefinir senha
        </a>

        <p style='margin-top:20px;font-size:12px;color:#777;'>
            Se você não solicitou isso, ignore este e-mail.
        </p>
    </div>
</body>
</html>";
        }
        else
        {
            emailHtml = emailHtml
                .Replace("{{APP_NAME}}", "Bíblia Reader")
                .Replace("{{TITLE}}", "Redefinição de senha")
                .Replace("{{MESSAGE}}", "Recebemos uma solicitação para redefinir sua senha.")
                .Replace("{{BUTTON_LABEL}}", "Redefinir senha")
                .Replace("{{BUTTON_URL}}", WebUtility.HtmlEncode(resetLink))
                .Replace("{{FOOTER_MESSAGE}}", "Se você não solicitou isso, ignore este e-mail.");
        }

        try
        {
            await emailService.SendEmailAsync(
                user.Email,
                "Redefinição de senha — Bíblia Reader",
                emailHtml);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Falha ao enviar e-mail de reset");
        }

        return Ok(new ResultViewModel<string>("Se o e-mail existir, você receberá instruções."));
    }

    [ApiExplorerSettings(IgnoreApi = true)]
    [AllowAnonymous]
    [HttpGet("/reset-password")]
    public IActionResult ResetPasswordPage([FromQuery] string token)
    {
        var html = LoadTemplate("reset-password.html");

        if (string.IsNullOrWhiteSpace(html))
            return Content(GetResetPasswordFallbackHtml(token ?? string.Empty), "text/html; charset=utf-8");

        html = html.Replace("{{TOKEN}}", WebUtility.HtmlEncode(token ?? string.Empty));
        return Content(html, "text/html; charset=utf-8");
    }

    [AllowAnonymous]
    [HttpPost("reset-password")]
    public async Task<IActionResult> ResetPassword(
        [FromForm] ResetPasswordViewModel model,
        [FromServices] AppDbContext db)
    {
        if (string.IsNullOrWhiteSpace(model.Token) || string.IsNullOrWhiteSpace(model.NewPassword))
            return BadRequest(new ResultViewModel<string>("Dados inválidos"));

        if (model.NewPassword.Length < 6)
            return BadRequest(new ResultViewModel<string>("Senha deve ter ao menos 6 caracteres"));

        var reset = await db.PasswordResetTokens
            .Include(x => x.AppUser)
            .FirstOrDefaultAsync(x => x.Token == model.Token);

        if (reset == null || reset.ExpiresAt < DateTime.UtcNow)
            return BadRequest(new ResultViewModel<string>("Token inválido ou expirado"));

        reset.AppUser.PasswordHash = BCrypt.Net.BCrypt.HashPassword(model.NewPassword);
        db.PasswordResetTokens.Remove(reset);
        await db.SaveChangesAsync();

        return Ok(new ResultViewModel<string>("Senha redefinida com sucesso"));
    }

    private string LoadTemplate(string fileName)
    {
        var webRoot = _env.WebRootPath ?? Path.Combine(_env.ContentRootPath, "wwwroot");

        var paths = new[]
        {
            Path.Combine(_env.ContentRootPath, "Templates", fileName),
            Path.Combine(_env.ContentRootPath, "templates", fileName),
            Path.Combine(_env.ContentRootPath, "wwwroot", "templates", fileName),

            Path.Combine(_env.ContentRootPath, "Templates", "emails", fileName),
            Path.Combine(_env.ContentRootPath, "templates", "emails", fileName),
            Path.Combine(_env.ContentRootPath, "wwwroot", "templates", "emails", fileName),

            Path.Combine(webRoot, "templates", fileName),
            Path.Combine(webRoot, "templates", "emails", fileName),
        };

        foreach (var path in paths)
        {
            if (System.IO.File.Exists(path))
            {
                _logger.LogInformation("Template carregado: {Path}", path);
                return System.IO.File.ReadAllText(path);
            }
        }

        _logger.LogError("Template NÃO encontrado: {FileName}", fileName);
        return string.Empty;
    }

    private static string GetVerifyEmailFallbackHtml()
    {
        return @"
<html>
<body style='margin:0;padding:20px;background:#F6F2FF;font-family:Arial,sans-serif;'>
    <div style='max-width:420px;margin:auto;background:#fff;padding:28px;border-radius:16px;text-align:center;border:1px solid #EEE7FF;'>
        <h2 style='color:#4B2E83;'>Bíblia Reader</h2>
        <p style='color:#333;'>Não foi possível carregar a página de confirmação.</p>
        <a href='bibliareader://login'
           style='display:inline-block;margin-top:16px;padding:12px 24px;background:#6C4DFF;color:#fff;text-decoration:none;border-radius:10px;font-weight:bold;'>
            Abrir aplicativo
        </a>
    </div>
</body>
</html>";
    }

    private static string GetResetPasswordFallbackHtml(string token)
    {
        return $@"
<html>
<body style='margin:0;padding:20px;background:#F6F2FF;font-family:Arial,sans-serif;'>
    <div style='max-width:420px;margin:auto;background:#fff;padding:28px;border-radius:16px;text-align:center;border:1px solid #EEE7FF;'>
        <h2 style='color:#4B2E83;'>Bíblia Reader</h2>
        <p style='color:#333;'>Defina sua nova senha abaixo.</p>

        <form method='post' action='/v1/auth/reset-password'>
            <input type='hidden' name='Token' value='{WebUtility.HtmlEncode(token)}' />
            <input type='password' name='NewPassword' placeholder='Nova senha'
                   style='width:100%;padding:12px;border:1px solid #ddd;border-radius:10px;margin-top:12px;' />
            <button type='submit'
                    style='display:inline-block;margin-top:16px;padding:12px 24px;background:#6C4DFF;color:#fff;border:none;border-radius:10px;font-weight:bold;'>
                Redefinir senha
            </button>
        </form>
    </div>
</body>
</html>";
    }
}