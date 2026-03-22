using BibleReader.Api.Models;

namespace BibleReader.Api;

public static class ReadingPlanRules
{
    public static int GetChaptersPerDay(ReadingPlanType type) => type switch
    {
        ReadingPlanType.BibleInOneYear => 4,
        ReadingPlanType.BibleInSixMonths => 7,
        ReadingPlanType.BibleInNinetyDays => 13,
        _ => throw new ArgumentOutOfRangeException(nameof(type))
    };

    public static string GetDisplayName(ReadingPlanType type) => type switch
    {
        ReadingPlanType.BibleInOneYear => "Bíblia em 1 ano",
        ReadingPlanType.BibleInSixMonths => "Bíblia em 6 meses",
        ReadingPlanType.BibleInNinetyDays => "Bíblia em 90 dias",
        _ => type.ToString()
    };
}
