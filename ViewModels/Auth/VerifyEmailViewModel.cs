namespace BibleReader.Api.ViewModels.Auth;

public class VerifyEmailViewModel
{
    /// <summary>Opcional: quando enviado (app), valida se o token pertence a este e-mail.</summary>
    public string? Email { get; set; }

    public string Token { get; set; } = string.Empty;
}
