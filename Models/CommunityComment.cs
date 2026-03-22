namespace BibleReader.Api.Models;

public class CommunityComment
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid PostId { get; set; }
    public Guid UserId { get; set; }
    public string Content { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public CommunityPost Post { get; set; } = null!;
    public AppUser User { get; set; } = null!;
}
