namespace BibleReader.Api.Models;

public class UserReadingPlan
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    public ReadingPlanType PlanType { get; set; }
    public UserReadingPlanStatus Status { get; set; } = UserReadingPlanStatus.Active;
    public DateOnly StartedOn { get; set; }
    public DateOnly? EndedOn { get; set; }

    public AppUser User { get; set; } = null!;
    public ICollection<ReadingPlanDay> Days { get; set; } = new List<ReadingPlanDay>();
}
