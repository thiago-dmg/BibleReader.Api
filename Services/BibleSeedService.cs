using BibleReader.Api.Data;
using BibleReader.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace BibleReader.Api.Services;

public class BibleSeedService
{
    private readonly AppDbContext _db;

    public BibleSeedService(AppDbContext db)
    {
        _db = db;
    }

    public async Task EnsureSeedAsync(CancellationToken ct = default)
    {
        if (await _db.BibleVersions.AnyAsync(ct))
            return;

        var version = new BibleVersion
        {
            Code = "NVI",
            Name = "Nova Versão Internacional",
            IsActive = true
        };

        _db.BibleVersions.Add(version);
        await _db.SaveChangesAsync(ct);

        var globalOrder = 1;

        foreach (var def in BibleCanon.Protestant)
        {
            var book = new BibleBook
            {
                BibleVersionId = version.Id,
                Order = def.Order,
                Slug = def.Slug,
                Name = def.Name,
                Abbreviation = def.Abbreviation,
                ChapterCount = def.ChapterCount
            };

            _db.BibleBooks.Add(book);
            await _db.SaveChangesAsync(ct);

            for (var c = 1; c <= def.ChapterCount; c++)
            {
                var chapter = new BibleChapter
                {
                    BibleBookId = book.Id,
                    ChapterNumber = c,
                    GlobalOrder = globalOrder++
                };

                _db.BibleChapters.Add(chapter);
            }
        }

        await _db.SaveChangesAsync(ct);

        // Versículos: carregados na primeira leitura via A Bíblia Digital (BibleChapterTextSyncService).
    }
}