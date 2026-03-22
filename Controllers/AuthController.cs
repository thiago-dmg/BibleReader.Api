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
    private readonly ILogger<AuthController> _logger;

    public AuthController(ILogger<AuthController> logger)
    {
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

        var verificationToken = new EmailVerificationToken { AppUserId = user.Id };
        db.EmailVerificationTokens.Add(verificationToken);
        await db.SaveChangesAsync();

        var baseUrl = configuration["AppSettings:BaseUrl"]?.TrimEnd('/') ?? $"{Request.Scheme}://{Request.Host}";
        var verificationLink = $"{baseUrl}/v1/auth/verify-email?token={Uri.EscapeDataString(verificationToken.Token)}";

        try
        {
            await emailService.SendEmailAsync(
                user.Email,
                "Confirme seu e-mail — Bíblia Reader",
                $@"<html><body style=""font-family:sans-serif"">
<p>Olá, {WebUtility.HtmlEncode(user.DisplayName)},</p>
<p><a href=""{WebUtility.HtmlEncode(verificationLink)}"">Confirmar e-mail</a></p>
<p>Ou copie: {WebUtility.HtmlEncode(verificationLink)}</p>
</body></html>");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Falha ao enviar e-mail de verificação");
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
        if (user == null)
            return Unauthorized(new ResultViewModel<string>("E-mail ou senha inválidos"));

        if (!BCrypt.Net.BCrypt.Verify(model.Password, user.PasswordHash))
            return Unauthorized(new ResultViewModel<string>("E-mail ou senha inválidos"));

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
        [FromServices] AppDbContext db)
    {
        if (string.IsNullOrWhiteSpace(model.Token))
            return BadRequest(new ResultViewModel<string>("Token inválido"));

        return await VerifyCoreAsync(model.Token, db);
    }

    [AllowAnonymous]
    [HttpGet("verify-email")]
    public async Task<IActionResult> VerifyEmailGet([FromQuery] string token, [FromServices] AppDbContext db)
    {
        if (string.IsNullOrWhiteSpace(token))
            return BadRequest(new ResultViewModel<string>("Token inválido"));

        return await VerifyCoreAsync(token, db);
    }

    private async Task<IActionResult> VerifyCoreAsync(string token, AppDbContext db)
    {
        var verification = await db.EmailVerificationTokens
            .Include(x => x.AppUser)
            .FirstOrDefaultAsync(x => x.Token == token);

        if (verification == null)
            return BadRequest(new ResultViewModel<string>("Token inválido"));

        if (verification.ExpiresAt < DateTime.UtcNow)
            return BadRequest(new ResultViewModel<string>("Token expirado"));

        verification.AppUser.EmailVerified = true;
        db.EmailVerificationTokens.Remove(verification);
        await db.SaveChangesAsync();

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
        if (string.IsNullOrEmpty(email))
            return Ok(new ResultViewModel<string>("Se o e-mail existir, você receberá instruções."));

        var user = await db.Users.FirstOrDefaultAsync(x => x.Email == email);
        if (user == null)
            return Ok(new ResultViewModel<string>("Se o e-mail existir, você receberá instruções."));

        var reset = new PasswordResetToken { AppUserId = user.Id };
        db.PasswordResetTokens.Add(reset);
        await db.SaveChangesAsync();

        var baseUrl = configuration["AppSettings:BaseUrl"]?.TrimEnd('/') ?? $"{Request.Scheme}://{Request.Host}";
        var link = $"{baseUrl}/v1/auth/reset-password?token={Uri.EscapeDataString(reset.Token)}";

        try
        {
            await emailService.SendEmailAsync(
                user.Email,
                "Redefinição de senha — Bíblia Reader",
                $@"<html><body style=""font-family:sans-serif"">
<p>Olá,</p>
<p><a href=""{WebUtility.HtmlEncode(link)}"">Redefinir senha</a></p>
<p>Ou copie: {WebUtility.HtmlEncode(link)}</p>
</body></html>");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Falha ao enviar e-mail de reset");
        }

        return Ok(new ResultViewModel<string>("Se o e-mail existir, você receberá instruções."));
    }

    [AllowAnonymous]
    [HttpPost("reset-password")]
    public async Task<IActionResult> ResetPassword(
        [FromBody] ResetPasswordViewModel model,
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
}
