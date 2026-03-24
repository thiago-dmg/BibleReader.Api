using BibleReader.Api.Services.Models;

namespace BibleReader.Api.Services;

public class ApiBibleProviderService : IBibleProviderService
{
    private readonly HttpClient _httpClient;
    private readonly ExternalBibleOptions _options;

    public ApiBibleProviderService(
        HttpClient httpClient,
        Microsoft.Extensions.Options.IOptions<ExternalBibleOptions> options)
    {
        _httpClient = httpClient;
        _options = options.Value;
    }

    public Task<IEnumerable<BibleVersionDto>> GetVersionsAsync(CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public Task<IEnumerable<BibleBookDto>> GetBooksAsync(
        string versionCode,
        CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public Task<BibleChapterDto> GetChapterAsync(
        string versionCode,
        string bookExternalId,
        int chapterNumber,
        CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public Task<BibleVerseDto?> GetVerseAsync(
        string versionCode,
        string bookExternalId,
        int chapterNumber,
        int verseNumber,
        CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }
}