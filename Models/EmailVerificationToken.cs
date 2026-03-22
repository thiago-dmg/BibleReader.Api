namespace BibleReader.Api.Models;

public class EmailVerificationToken
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid AppUserId { get; set; }
    public string Token { get; set; } = Guid.NewGuid().ToString();
    public DateTime ExpiresAt { get; set; } = DateTime.UtcNow.AddHours(24);

    public AppUser AppUser { get; set; } = null!;
}
