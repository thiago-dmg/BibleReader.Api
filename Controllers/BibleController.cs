using BibleReader.Api.Data;
using BibleReader.Api.ViewModels;
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
    public async Task<IActionResult> Versions([FromServices] AppDbContext db)
    {
        var list = await db.BibleVersions
            .AsNoTracking()
            .Where(v => v.IsActive)
            .Select(v => new { v.Id, v.Code, v.Name })
            .ToListAsync();
        return Ok(new ResultViewModel<object>(list));
    }

    [HttpGet("books")]
    public async Task<IActionResult> Books([FromQuery] int versionId, [FromServices] AppDbContext db)
    {
        var list = await db.BibleBooks
            .AsNoTracking()
            .Where(b => b.BibleVersionId == versionId)
            .OrderBy(b => b.Order)
            .Select(b => new
            {
                b.Id,
                b.Order,
                b.Slug,
                b.Name,
                b.Abbreviation,
                b.ChapterCount
            })
            .ToListAsync();
        return Ok(new ResultViewModel<object>(list));
    }

    /// <summary>Lista números de capítulos do livro (metadados).</summary>
    [HttpGet("books/{bookId:int}/chapters")]
    public async Task<IActionResult> BookChapters(int bookId, [FromServices] AppDbContext db)
    {
        var list = await db.BibleChapters
            .AsNoTracking()
            .Where(c => c.BibleBookId == bookId)
            .OrderBy(c => c.ChapterNumber)
            .Select(c => new { c.Id, c.ChapterNumber, c.GlobalOrder })
            .ToListAsync();
        return Ok(new ResultViewModel<object>(list));
    }

    [HttpGet("books/{bookId:int}/chapters/{chapterNumber:int}")]
    public async Task<IActionResult> ChapterContent(int bookId, int chapterNumber, [FromServices] AppDbContext db)
    {
        var chapter = await db.BibleChapters
            .AsNoTracking()
            .Include(c => c.BibleBook)
            .FirstOrDefaultAsync(c => c.BibleBookId == bookId && c.ChapterNumber == chapterNumber);

        if (chapter == null)
            return NotFound(new ResultViewModel<string>("Capítulo não encontrado"));

        var verses = await db.BibleVerses
            .AsNoTracking()
            .Where(v => v.BibleChapterId == chapter.Id)
            .OrderBy(v => v.VerseNumber)
            .Select(v => new { v.VerseNumber, v.Text })
            .ToListAsync();

        return Ok(new ResultViewModel<object>(new
        {
            chapter.Id,
            book = new { chapter.BibleBook.Id, chapter.BibleBook.Name, chapter.BibleBook.Slug },
            chapter.ChapterNumber,
            verses
        }));
    }

    [HttpGet("books/{bookId:int}/chapters/{chapterNumber:int}/verses/{verseNumber:int}")]
    public async Task<IActionResult> Verse(int bookId, int chapterNumber, int verseNumber, [FromServices] AppDbContext db)
    {
        var chapter = await db.BibleChapters
            .AsNoTracking()
            .Include(c => c.BibleBook)
            .FirstOrDefaultAsync(c => c.BibleBookId == bookId && c.ChapterNumber == chapterNumber);

        if (chapter == null)
            return NotFound(new ResultViewModel<string>("Capítulo não encontrado"));

        var verse = await db.BibleVerses
            .AsNoTracking()
            .FirstOrDefaultAsync(v => v.BibleChapterId == chapter.Id && v.VerseNumber == verseNumber);

        if (verse == null)
            return NotFound(new ResultViewModel<string>("Versículo não encontrado"));

        return Ok(new ResultViewModel<object>(new
        {
            verse.Id,
            book = new { chapter.BibleBook.Id, chapter.BibleBook.Name },
            chapter.ChapterNumber,
            verse.VerseNumber,
            verse.Text
        }));
    }

    /// <summary>Atalho: lista todos os capítulos da versão (1189 linhas) — use com versionId.</summary>
    [HttpGet("chapters")]
    public async Task<IActionResult> AllChapters([FromQuery] int versionId, [FromServices] AppDbContext db)
    {
        var bookIds = await db.BibleBooks
            .AsNoTracking()
            .Where(b => b.BibleVersionId == versionId)
            .Select(b => b.Id)
            .ToListAsync();

        var list = await db.BibleChapters
            .AsNoTracking()
            .Where(c => bookIds.Contains(c.BibleBookId))
            .OrderBy(c => c.GlobalOrder)
            .Select(c => new { c.Id, c.BibleBookId, c.ChapterNumber, c.GlobalOrder })
            .ToListAsync();

        return Ok(new ResultViewModel<object>(list));
    }
}
