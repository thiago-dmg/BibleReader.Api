using BibleReader.Api.Data;
using BibleReader.Api.ViewModels;
using BibleReader.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BibleReader.Api.Controllers;

[ApiController]
[AllowAnonymous]
[Route("v1/bible")]
public class BibleController : ControllerBase
{
    [HttpGet("versions")]
    public async Task<IActionResult> Versions([FromServices] IBibleProviderService bibleProvider)
    {
        var versions = await bibleProvider.GetVersionsAsync(HttpContext.RequestAborted);
        return Ok(new ResultViewModel<object>(versions));
    }

    [HttpGet("books")]
    public async Task<IActionResult> Books(
        [FromQuery] string versionCode,
        [FromServices] AppDbContext db,
        [FromServices] IBibleProviderService bibleProvider)
    {
        if (string.IsNullOrWhiteSpace(versionCode))
            return BadRequest(new ResultViewModel<string>("versionCode é obrigatório"));

        var booksFromProvider = (await bibleProvider.GetBooksAsync(versionCode, HttpContext.RequestAborted)).ToList();

        var localBooks = await db.BibleBooks
            .AsNoTracking()
            .OrderBy(b => b.Order)
            .ToListAsync(HttpContext.RequestAborted);

        var result = booksFromProvider.Select(providerBook =>
        {
            var local = localBooks.FirstOrDefault(x =>
                x.Abbreviation.ToLower() == providerBook.Abbreviation.ToLower() ||
                x.Name.ToLower() == providerBook.Name.ToLower());

            return new
            {
                id = local?.Id,
                order = local?.Order,
                slug = local?.Slug,
                name = providerBook.Name,
                abbreviation = providerBook.Abbreviation,
                chapterCount = local?.ChapterCount ?? providerBook.ChapterCount,
                externalId = providerBook.ExternalId
            };
        });

        return Ok(new ResultViewModel<object>(result));
    }

    [HttpGet("books/{bookId:int}/chapters")]
    public async Task<IActionResult> BookChapters(int bookId, [FromServices] AppDbContext db)
    {
        var list = await db.BibleChapters
            .AsNoTracking()
            .Where(c => c.BibleBookId == bookId)
            .OrderBy(c => c.ChapterNumber)
            .Select(c => new
            {
                c.Id,
                c.ChapterNumber,
                c.GlobalOrder
            })
            .ToListAsync(HttpContext.RequestAborted);

        return Ok(new ResultViewModel<object>(list));
    }

    [HttpGet("books/{bookExternalId}/chapters/{chapterNumber:int}")]
    public async Task<IActionResult> ChapterContent(
        string bookExternalId,
        int chapterNumber,
        [FromQuery] string versionCode,
        [FromServices] IBibleProviderService bibleProvider)
    {
        if (string.IsNullOrWhiteSpace(versionCode))
            return BadRequest(new ResultViewModel<string>("versionCode é obrigatório"));

        if (string.IsNullOrWhiteSpace(bookExternalId))
            return BadRequest(new ResultViewModel<string>("bookExternalId é obrigatório"));

        var chapterData = await bibleProvider.GetChapterAsync(
            versionCode,
            bookExternalId,
            chapterNumber,
            HttpContext.RequestAborted);

        return Ok(new ResultViewModel<object>(new
        {
            bookExternalId,
            chapterNumber,
            verses = chapterData.Verses.Select(v => new
            {
                verseNumber = v.Number,
                text = v.Text
            })
        }));
    }

    [HttpGet("books/{bookExternalId}/chapters/{chapterNumber:int}/verses/{verseNumber:int}")]
    public async Task<IActionResult> Verse(
        string bookExternalId,
        int chapterNumber,
        int verseNumber,
        [FromQuery] string versionCode,
        [FromServices] IBibleProviderService bibleProvider)
    {
        if (string.IsNullOrWhiteSpace(versionCode))
            return BadRequest(new ResultViewModel<string>("versionCode é obrigatório"));

        if (string.IsNullOrWhiteSpace(bookExternalId))
            return BadRequest(new ResultViewModel<string>("bookExternalId é obrigatório"));

        var verse = await bibleProvider.GetVerseAsync(
            versionCode,
            bookExternalId,
            chapterNumber,
            verseNumber,
            HttpContext.RequestAborted);

        if (verse == null)
            return NotFound(new ResultViewModel<string>("Versículo não encontrado"));

        return Ok(new ResultViewModel<object>(new
        {
            bookExternalId,
            chapterNumber,
            verseNumber = verse.Number,
            text = verse.Text
        }));
    }

    [HttpGet("chapters")]
    public async Task<IActionResult> AllChapters([FromQuery] int versionId, [FromServices] AppDbContext db)
    {
        var bookIds = await db.BibleBooks
            .AsNoTracking()
            .Where(b => b.BibleVersionId == versionId)
            .Select(b => b.Id)
            .ToListAsync(HttpContext.RequestAborted);

        var list = await db.BibleChapters
            .AsNoTracking()
            .Where(c => bookIds.Contains(c.BibleBookId))
            .OrderBy(c => c.GlobalOrder)
            .Select(c => new
            {
                c.Id,
                c.BibleBookId,
                c.ChapterNumber,
                c.GlobalOrder
            })
            .ToListAsync(HttpContext.RequestAborted);

        return Ok(new ResultViewModel<object>(list));
    }
}