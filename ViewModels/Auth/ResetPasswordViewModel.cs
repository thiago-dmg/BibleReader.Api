namespace BibleReader.Api.ViewModels.Auth;

public class ResetPasswordViewModel
{
    public string Token { get; set; } = string.Empty;
    public string NewPassword { get; set; } = string.Empty;
}
