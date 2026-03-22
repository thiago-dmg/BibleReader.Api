namespace BibleReader.Api.Models;

public class ReadingPlanDayChapter
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ReadingPlanDayId { get; set; }
    public int BibleChapterId { get; set; }
    public bool IsRead { get; set; }
    public DateTime? ReadAt { get; set; }

    public ReadingPlanDay ReadingPlanDay { get; set; } = null!;
    public BibleChapter BibleChapter { get; set; } = null!;
}
