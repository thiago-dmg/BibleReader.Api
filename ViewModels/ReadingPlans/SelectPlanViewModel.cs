using BibleReader.Api.Models;

namespace BibleReader.Api.ViewModels.ReadingPlans;

public class SelectPlanViewModel
{
    public ReadingPlanType PlanType { get; set; }
    /// <summary>Se omitido, usa a data UTC de hoje.</summary>
    public DateOnly? StartedOn { get; set; }
}
