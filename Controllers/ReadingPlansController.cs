using System.Globalization;
using BibleReader.Api;
using BibleReader.Api.Data;
using BibleReader.Api.Extensions;
using BibleReader.Api.Models;
using BibleReader.Api.Services;
using BibleReader.Api.ViewModels;
using BibleReader.Api.ViewModels.ReadingPlans;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BibleReader.Api.Controllers;

[ApiController]
[Authorize]
[Route("v1/reading-plans")]
public class ReadingPlansController : ControllerBase
{
    [HttpGet("current")]
    public async Task<IActionResult> GetCurrent([FromServices] AppDbContext db, [FromServices] ReadingPlanService plans)
    {
        var userId = User.GetUserId();
        if (userId == null)
            return Unauthorized();

        var plan = await plans.GetActivePlanAsync(userId.Value);
        if (plan == null)
            return NotFound(new ResultViewModel<string>("Nenhum plano ativo"));

        return Ok(new ResultViewModel<object>(await MapPlanSummaryAsync(db, plan, HttpContext.RequestAborted)));
    }

    [HttpPost("select")]
    public async Task<IActionResult> Select(
        [FromBody] SelectPlanViewModel model,
        [FromServices] AppDbContext db,
        [FromServices] ReadingPlanService plans)
    {
        var userId = User.GetUserId();
        if (userId == null)
            return Unauthorized();

        var started = model.StartedOn ?? DateOnly.FromDateTime(DateTime.UtcNow);

        var plan = await plans.SelectPlanAsync(userId.Value, model.PlanType, started);
        return Ok(new ResultViewModel<object>(await MapPlanSummaryAsync(db, plan, HttpContext.RequestAborted)));
    }

    [HttpGet("current/calendar")]
    public async Task<IActionResult> Calendar(
        [FromQuery] DateOnly? from,
        [FromQuery] DateOnly? to,
        [FromServices] AppDbContext db,
        [FromServices] ReadingPlanService plans)
    {
        var userId = User.GetUserId();
        if (userId == null)
            return Unauthorized();

        var plan = await plans.GetActivePlanAsync(userId.Value);
        if (plan == null)
            return NotFound(new ResultViewModel<string>("Nenhum plano ativo"));

        var start = from ?? plan.StartedOn;
        var end = to ?? start.AddMonths(6);

        var days = await db.ReadingPlanDays
            .AsNoTracking()
            .Where(d => d.UserReadingPlanId == plan.Id && d.CalendarDate >= start && d.CalendarDate <= end)
            .Select(d => new
            {
                date = d.CalendarDate,
                d.IsCompleted,
                totalChapters = d.Chapters.Count(),
                completedChapters = d.Chapters.Count(c => c.IsRead)
            })
            .OrderBy(x => x.date)
            .ToListAsync();

        return Ok(new ResultViewModel<object>(days));
    }

    [HttpGet("current/today")]
    public async Task<IActionResult> Today([FromServices] AppDbContext db, [FromServices] ReadingPlanService plans)
    {
        var userId = User.GetUserId();
        if (userId == null)
            return Unauthorized();

        var plan = await plans.GetActivePlanAsync(userId.Value);
        if (plan == null)
            return NotFound(new ResultViewModel<string>("Nenhum plano ativo"));

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        return await DayInternal(db, plan.Id, today);
    }

    [HttpGet("current/days/{date}")]
    public async Task<IActionResult> DayByDate(string date, [FromServices] AppDbContext db, [FromServices] ReadingPlanService plans)
    {
        var userId = User.GetUserId();
        if (userId == null)
            return Unauthorized();

        if (!DateOnly.TryParse(date, CultureInfo.InvariantCulture, DateTimeStyles.None, out var d))
            return BadRequest(new ResultViewModel<string>("Use o formato yyyy-MM-dd"));

        var plan = await plans.GetActivePlanAsync(userId.Value);
        if (plan == null)
            return NotFound(new ResultViewModel<string>("Nenhum plano ativo"));

        return await DayInternal(db, plan.Id, d);
    }

    private async Task<IActionResult> DayInternal(AppDbContext db, Guid planId, DateOnly calendarDate)
    {
        var day = await db.ReadingPlanDays
            .AsNoTracking()
            .Include(d => d.Chapters)
            .ThenInclude(c => c.BibleChapter)
            .ThenInclude(ch => ch.BibleBook)
            .FirstOrDefaultAsync(d => d.UserReadingPlanId == planId && d.CalendarDate == calendarDate);

        if (day == null)
            return NotFound(new ResultViewModel<string>("Dia não encontrado neste plano"));

        var chapters = day.Chapters
            .OrderBy(x => x.BibleChapter.GlobalOrder)
            .Select(x => new
            {
                x.BibleChapterId,
                book = x.BibleChapter.BibleBook.Name,
                bookId = x.BibleChapter.BibleBookId,
                bookAbbreviation = x.BibleChapter.BibleBook.Abbreviation,
                chapterNumber = x.BibleChapter.ChapterNumber,
                x.IsRead,
                x.ReadAt
            });

        return Ok(new ResultViewModel<object>(new
        {
            date = day.CalendarDate,
            day.IsCompleted,
            totalChapters = day.Chapters.Count,
            completedChapters = day.Chapters.Count(c => c.IsRead),
            chapters
        }));
    }

    [HttpPatch("current/chapters/{chapterId:int}/read")]
    public async Task<IActionResult> MarkRead(int chapterId, [FromServices] ReadingPlanService plans)
    {
        var userId = User.GetUserId();
        if (userId == null)
            return Unauthorized();

        var ok = await plans.SetChapterReadAsync(userId.Value, chapterId, true);
        if (!ok)
            return NotFound(new ResultViewModel<string>("Capítulo não encontrado no plano atual"));

        return Ok(new ResultViewModel<string>("Marcado como lido"));
    }

    [HttpPatch("current/chapters/{chapterId:int}/unread")]
    public async Task<IActionResult> MarkUnread(int chapterId, [FromServices] ReadingPlanService plans)
    {
        var userId = User.GetUserId();
        if (userId == null)
            return Unauthorized();

        var ok = await plans.SetChapterReadAsync(userId.Value, chapterId, false);
        if (!ok)
            return NotFound(new ResultViewModel<string>("Capítulo não encontrado no plano atual"));

        return Ok(new ResultViewModel<string>("Marcado como não lido"));
    }

    [HttpGet("current/progress")]
    public async Task<IActionResult> Progress([FromServices] AppDbContext db, [FromServices] ReadingPlanService plans)
    {
        var userId = User.GetUserId();
        if (userId == null)
            return Unauthorized();

        var plan = await plans.GetActivePlanAsync(userId.Value);
        if (plan == null)
            return NotFound(new ResultViewModel<string>("Nenhum plano ativo"));

        var totalDays = await db.ReadingPlanDays.CountAsync(d => d.UserReadingPlanId == plan.Id);
        var completedDays = await db.ReadingPlanDays.CountAsync(d => d.UserReadingPlanId == plan.Id && d.IsCompleted);

        var totalChaptersInPlan = await db.ReadingPlanDayChapters
            .CountAsync(c => c.ReadingPlanDay.UserReadingPlanId == plan.Id);

        var readChapters = await db.ReadingPlanDayChapters
            .CountAsync(c => c.ReadingPlanDay.UserReadingPlanId == plan.Id && c.IsRead);

        var pct = totalChaptersInPlan == 0 ? 0 : Math.Round(100.0 * readChapters / totalChaptersInPlan, 2);

        return Ok(new ResultViewModel<object>(new
        {
            planId = plan.Id,
            plan.PlanType,
            planName = ReadingPlanRules.GetDisplayName(plan.PlanType),
            plan.StartedOn,
            totalDays,
            completedDays,
            totalChaptersInPlan,
            readChapters,
            percentComplete = pct
        }));
    }

    private static async Task<object> MapPlanSummaryAsync(AppDbContext db, UserReadingPlan plan, CancellationToken ct)
    {
        var totalDays = await db.ReadingPlanDays.CountAsync(d => d.UserReadingPlanId == plan.Id, ct);
        var chaptersPerDay = ReadingPlanRules.GetChaptersPerDay(plan.PlanType);

        return new
        {
            plan.Id,
            plan.PlanType,
            planName = ReadingPlanRules.GetDisplayName(plan.PlanType),
            chaptersPerDay,
            plan.StartedOn,
            plan.Status,
            totalScheduledDays = totalDays
        };
    }
}