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

        var booksFromProvider = await bibleProvider.GetBooksAsync(versionCode, HttpContext.RequestAborted);

        // Mantém compatibilidade com IDs locais usados no plano de leitura
        var localBooks = await db.BibleBooks
            .AsNoTracking()
            .Include(b => b.BibleVersion)
            .Where(b => b.BibleVersion.Code == versionCode)
            .OrderBy(b => b.Order)
            .ToListAsync(HttpContext.RequestAborted);

        var result = localBooks.Select(local =>
        {
            var providerBook = booksFromProvider.FirstOrDefault(x =>
                x.Abbreviation.ToLower() == local.Abbreviation.ToLower() ||
                x.Name.ToLower() == local.Name.ToLower());

            return new
            {
                local.Id,
                local.Order,
                local.Slug,
                local.Name,
                local.Abbreviation,
                local.ChapterCount,
                externalId = providerBook?.ExternalId
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

    [HttpGet("books/{bookId:int}/chapters/{chapterNumber:int}")]
    public async Task<IActionResult> ChapterContent(
        int bookId,
        int chapterNumber,
        [FromQuery] string versionCode,
        [FromServices] AppDbContext db,
        [FromServices] IBibleProviderService bibleProvider)
    {
        if (string.IsNullOrWhiteSpace(versionCode))
            return BadRequest(new ResultViewModel<string>("versionCode é obrigatório"));

        var chapter = await db.BibleChapters
            .AsNoTracking()
            .Include(c => c.BibleBook)
            .ThenInclude(b => b.BibleVersion)
            .FirstOrDefaultAsync(c => c.BibleBookId == bookId && c.ChapterNumber == chapterNumber, HttpContext.RequestAborted);

        if (chapter == null)
            return NotFound(new ResultViewModel<string>("Capítulo não encontrado"));

        var externalBookId = chapter.BibleBook.Abbreviation;
        var chapterData = await bibleProvider.GetChapterAsync(
            versionCode,
            externalBookId,
            chapterNumber,
            HttpContext.RequestAborted);

        return Ok(new ResultViewModel<object>(new
        {
            chapter.Id,
            book = new
            {
                chapter.BibleBook.Id,
                chapter.BibleBook.Name,
                chapter.BibleBook.Slug,
                chapter.BibleBook.Abbreviation
            },
            chapter.ChapterNumber,
            verses = chapterData.Verses.Select(v => new
            {
                verseNumber = v.Number,
                text = v.Text
            })
        }));
    }

    [HttpGet("books/{bookId:int}/chapters/{chapterNumber:int}/verses/{verseNumber:int}")]
    public async Task<IActionResult> Verse(
        int bookId,
        int chapterNumber,
        int verseNumber,
        [FromQuery] string versionCode,
        [FromServices] AppDbContext db,
        [FromServices] IBibleProviderService bibleProvider)
    {
        if (string.IsNullOrWhiteSpace(versionCode))
            return BadRequest(new ResultViewModel<string>("versionCode é obrigatório"));

        var chapter = await db.BibleChapters
            .AsNoTracking()
            .Include(c => c.BibleBook)
            .FirstOrDefaultAsync(c => c.BibleBookId == bookId && c.ChapterNumber == chapterNumber, HttpContext.RequestAborted);

        if (chapter == null)
            return NotFound(new ResultViewModel<string>("Capítulo não encontrado"));

        var externalBookId = chapter.BibleBook.Abbreviation;

        var verse = await bibleProvider.GetVerseAsync(
            versionCode,
            externalBookId,
            chapterNumber,
            verseNumber,
            HttpContext.RequestAborted);

        if (verse == null)
            return NotFound(new ResultViewModel<string>("Versículo não encontrado"));

        return Ok(new ResultViewModel<object>(new
        {
            book = new
            {
                chapter.BibleBook.Id,
                chapter.BibleBook.Name
            },
            chapter.ChapterNumber,
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