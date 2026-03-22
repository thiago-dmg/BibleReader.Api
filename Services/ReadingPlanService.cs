using BibleReader.Api;
using BibleReader.Api.Data;
using BibleReader.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace BibleReader.Api.Services;

public class ReadingPlanService
{
    private readonly AppDbContext _db;

    public ReadingPlanService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<UserReadingPlan?> GetActivePlanAsync(Guid userId, CancellationToken ct = default)
    {
        return await _db.UserReadingPlans
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.UserId == userId && p.Status == UserReadingPlanStatus.Active, ct);
    }

    public async Task<UserReadingPlan> SelectPlanAsync(
        Guid userId,
        ReadingPlanType planType,
        DateOnly startedOn,
        CancellationToken ct = default)
    {
        var chaptersPerDay = ReadingPlanRules.GetChaptersPerDay(planType);
        var chapterIds = await _db.BibleChapters
            .OrderBy(c => c.GlobalOrder)
            .Select(c => c.Id)
            .ToListAsync(ct);

        if (chapterIds.Count == 0)
            throw new InvalidOperationException("Bíblia ainda não foi carregada no banco (seed).");

        await using var tx = await _db.Database.BeginTransactionAsync(ct);

        var actives = await _db.UserReadingPlans
            .Where(p => p.UserId == userId && p.Status == UserReadingPlanStatus.Active)
            .ToListAsync(ct);

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        foreach (var p in actives)
        {
            p.Status = UserReadingPlanStatus.Superseded;
            p.EndedOn = today;
        }

        var plan = new UserReadingPlan
        {
            UserId = userId,
            PlanType = planType,
            StartedOn = startedOn,
            Status = UserReadingPlanStatus.Active
        };
        _db.UserReadingPlans.Add(plan);
        await _db.SaveChangesAsync(ct);

        var date = startedOn;
        for (var i = 0; i < chapterIds.Count; )
        {
            var slice = chapterIds.Skip(i).Take(chaptersPerDay).ToList();
            if (slice.Count == 0)
                break;

            var day = new ReadingPlanDay
            {
                UserReadingPlanId = plan.Id,
                CalendarDate = date,
                IsCompleted = false
            };
            _db.ReadingPlanDays.Add(day);

            foreach (var chapterId in slice)
            {
                _db.ReadingPlanDayChapters.Add(new ReadingPlanDayChapter
                {
                    ReadingPlanDay = day,
                    BibleChapterId = chapterId,
                    IsRead = false
                });
            }

            i += slice.Count;
            date = date.AddDays(1);
        }

        await _db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);

        return plan;
    }

    public async Task RecalculateDayCompletionAsync(Guid readingPlanDayId, CancellationToken ct = default)
    {
        var day = await _db.ReadingPlanDays
            .Include(d => d.Chapters)
            .FirstAsync(d => d.Id == readingPlanDayId, ct);

        var allRead = day.Chapters.All(c => c.IsRead);
        day.IsCompleted = allRead;
        await _db.SaveChangesAsync(ct);

        await RecalculatePlanCompletionAsync(day.UserReadingPlanId, ct);
    }

    private async Task RecalculatePlanCompletionAsync(Guid planId, CancellationToken ct)
    {
        var plan = await _db.UserReadingPlans
            .Include(p => p.Days)
            .FirstAsync(p => p.Id == planId, ct);

        var total = plan.Days.Count;
        var completed = plan.Days.Count(d => d.IsCompleted);
        if (total > 0 && completed == total)
        {
            plan.Status = UserReadingPlanStatus.Completed;
            plan.EndedOn = DateOnly.FromDateTime(DateTime.UtcNow);
            await _db.SaveChangesAsync(ct);
        }
    }

    public async Task<bool> SetChapterReadAsync(
        Guid userId,
        int bibleChapterId,
        bool isRead,
        CancellationToken ct = default)
    {
        var plan = await _db.UserReadingPlans
            .FirstOrDefaultAsync(p => p.UserId == userId && p.Status == UserReadingPlanStatus.Active, ct);
        if (plan == null)
            return false;

        var link = await _db.ReadingPlanDayChapters
            .Include(x => x.ReadingPlanDay)
            .Where(x => x.BibleChapterId == bibleChapterId && x.ReadingPlanDay.UserReadingPlanId == plan.Id)
            .FirstOrDefaultAsync(ct);

        if (link == null)
            return false;

        link.IsRead = isRead;
        link.ReadAt = isRead ? DateTime.UtcNow : null;
        await _db.SaveChangesAsync(ct);

        await RecalculateDayCompletionAsync(link.ReadingPlanDayId, ct);
        return true;
    }
}
