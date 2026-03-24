using BibleReader.Api.Services.Models;

namespace BibleReader.Api.Services;

public interface IBibleProviderService
{
    Task<IEnumerable<BibleVersionDto>> GetVersionsAsync(CancellationToken cancellationToken = default);

    Task<IEnumerable<BibleBookDto>> GetBooksAsync(
        string versionCode,
        CancellationToken cancellationToken = default);

    Task<BibleChapterDto> GetChapterAsync(
        string versionCode,
        string bookExternalId,
        int chapterNumber,
        CancellationToken cancellationToken = default);

    Task<BibleVerseDto?> GetVerseAsync(
        string versionCode,
        string bookExternalId,
        int chapterNumber,
        int verseNumber,
        CancellationToken cancellationToken = default);
}