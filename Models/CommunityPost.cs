namespace BibleReader.Api.Models;

public class CommunityPost
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    public string Content { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public AppUser User { get; set; } = null!;
    public ICollection<CommunityPostLike> Likes { get; set; } = new List<CommunityPostLike>();
    public ICollection<CommunityComment> Comments { get; set; } = new List<CommunityComment>();
}
