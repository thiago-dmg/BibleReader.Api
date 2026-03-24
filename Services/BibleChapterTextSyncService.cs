using BibleReader.Api.Data;
using BibleReader.Api.External;
using BibleReader.Api.Interfaces;
using BibleReader.Api.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace BibleReader.Api.Services;

/// <summary>
/// Preenche versículos no SQL a partir da <see href="https://www.abibliadigital.com.br/">A Bíblia Digital</see>
/// na primeira leitura (ou quando ainda há placeholders do seed antigo).
/// </summary>
public sealed class BibleChapterTextSyncService
{
    private readonly IAbibliadigitalClient _external;
    private readonly AbibliadigitalClientOptions _options;
    private readonly ILogger<BibleChapterTextSyncService> _logger;

    public BibleChapterTextSyncService(
        IAbibliadigitalClient external,
        IOptions<AbibliadigitalClientOptions> options,
        ILogger<BibleChapterTextSyncService> logger)
    {
        _external = external;
        _options = options.Value;
        _logger = logger;
    }

    public async Task EnsureChapterTextAsync(AppDbContext db, int bookId, int chapterNumber, CancellationToken ct)
    {
        if (!_options.Enabled)
            return;

        var chapter = await db.BibleChapters
            .Include(c => c.BibleBook)
            .ThenInclude(b => b.BibleVersion)
            .FirstOrDefaultAsync(c => c.BibleBookId == bookId && c.ChapterNumber == chapterNumber, ct);

        if (chapter?.BibleBook?.BibleVersion == null)
            return;

        var existing = await db.BibleVerses
            .Where(v => v.BibleChapterId == chapter.Id)
            .OrderBy(v => v.VerseNumber)
            .ToListAsync(ct);

        if (existing.Count > 0 && !existing.Any(IsPlaceholderText))
            return;

        if (!AbibliadigitalBookAbbrevMap.TryGetAbbrev(chapter.BibleBook.Slug, out var abbrev))
        {
            _logger.LogWarning("Slug de livro sem mapeamento para A Bíblia Digital: {Slug}", chapter.BibleBook.Slug);
            return;
        }

        var version = chapter.BibleBook.BibleVersion.Code.Trim().ToLowerInvariant();
        if (string.IsNullOrEmpty(version))
            version = "nvi";

        var payload = await _external.GetChapterAsync(version, abbrev, chapterNumber, ct);
        var items = payload?.Verses?.Where(v => v.Number > 0 && !string.IsNullOrWhiteSpace(v.Text)).ToList();
        if (items == null || items.Count == 0)
        {
            _logger.LogWarning(
                "Sem versículos da API externa para {Version}/{Abbrev}/{Chapter}",
                version,
                abbrev,
                chapterNumber);
            return;
        }

        if (existing.Count > 0)
            db.BibleVerses.RemoveRange(existing);

        foreach (var v in items)
        {
            db.BibleVerses.Add(new BibleVerse
            {
                BibleChapterId = chapter.Id,
                VerseNumber = v.Number,
                Text = v.Text.Trim()
            });
        }

        await db.SaveChangesAsync(ct);
    }

    private static bool IsPlaceholderText(BibleVerse v)
    {
        var t = v.Text;
        return t.Contains("substitua pelo texto", StringComparison.OrdinalIgnoreCase)
               || t.Contains("[Versículo", StringComparison.OrdinalIgnoreCase);
    }
}
