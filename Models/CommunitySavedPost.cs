namespace BibleReader.Api.Models;

public class CommunitySavedPost
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid PostId { get; set; }
    public Guid UserId { get; set; }
    public DateTime SavedAt { get; set; } = DateTime.UtcNow;

    public CommunityPost Post { get; set; } = null!;
    public AppUser User { get; set; } = null!;
}
