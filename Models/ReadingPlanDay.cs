namespace BibleReader.Api.Models;

public class ReadingPlanDay
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserReadingPlanId { get; set; }
    public DateOnly CalendarDate { get; set; }
    public bool IsCompleted { get; set; }

    public UserReadingPlan UserReadingPlan { get; set; } = null!;
    public ICollection<ReadingPlanDayChapter> Chapters { get; set; } = new List<ReadingPlanDayChapter>();
}
